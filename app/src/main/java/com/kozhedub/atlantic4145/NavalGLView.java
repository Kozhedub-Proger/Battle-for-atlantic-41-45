package com.kozhedub.atlantic4145;

import android.content.Context;
import android.opengl.GLES20;
import android.opengl.GLSurfaceView;
import android.opengl.Matrix;
import android.view.MotionEvent;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.FloatBuffer;
import java.util.Random;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;

public class NavalGLView extends GLSurfaceView {
    public interface GameListener { void onHit(int hp, boolean sunk); void onMiss(); }
    private final SeaRenderer renderer;
    private GameListener listener;
    private float lastX, lastY, oldPinch;

    public NavalGLView(Context c) {
        super(c);
        setEGLContextClientVersion(2);
        setEGLConfigChooser(8,8,8,8,24,0);
        renderer = new SeaRenderer();
        setRenderer(renderer);
        setRenderMode(RENDERMODE_CONTINUOUSLY);
        setPreserveEGLContextOnPause(true);
    }

    public void setListener(GameListener l) { listener = l; renderer.listener = l; }
    public boolean fireMainBattery() { queueEvent(renderer::fire); return true; }
    public void zoom(float d) { queueEvent(() -> renderer.distance = clamp(renderer.distance + d, 18f, 55f)); }

    @Override public boolean onTouchEvent(MotionEvent e) {
        if (e.getPointerCount() >= 2) {
            float dx=e.getX(0)-e.getX(1), dy=e.getY(0)-e.getY(1);
            float pinch=(float)Math.sqrt(dx*dx+dy*dy);
            if (oldPinch>0) {
                float delta=(oldPinch-pinch)*0.035f;
                queueEvent(() -> renderer.distance=clamp(renderer.distance+delta,18f,55f));
            }
            oldPinch=pinch; return true;
        }
        oldPinch=0;
        if (e.getAction()==MotionEvent.ACTION_DOWN) { lastX=e.getX(); lastY=e.getY(); return true; }
        if (e.getAction()==MotionEvent.ACTION_MOVE) {
            float dx=e.getX()-lastX, dy=e.getY()-lastY; lastX=e.getX(); lastY=e.getY();
            queueEvent(() -> { renderer.yaw += dx*.22f; renderer.pitch=clamp(renderer.pitch-dy*.16f,8f,48f); });
            return true;
        }
        return true;
    }

    private static float clamp(float v,float a,float b){return Math.max(a,Math.min(b,v));}

    private static class SeaRenderer implements Renderer {
        private final float[] proj=new float[16], view=new float[16], model=new float[16], mv=new float[16], mvp=new float[16];
        private Mesh cube, wedge, water;
        private int program, aPos, uMvp, uModel, uColor, uTime, uWater, uCam;
        private long start=System.nanoTime(), last=start;
        private float time;
        float yaw=28f, pitch=22f, distance=34f;
        private boolean firing=false, impact=false;
        private float shellT=0, impactT=0;
        private int enemyHp=100;
        private final Random random=new Random();
        GameListener listener;

        private final float px=-8f,pz=5f, ex=8f,ez=-8f;

        @Override public void onSurfaceCreated(GL10 gl, EGLConfig cfg) {
            GLES20.glClearColor(.045f,.12f,.18f,1);
            GLES20.glEnable(GLES20.GL_DEPTH_TEST);
            GLES20.glEnable(GLES20.GL_CULL_FACE);
            GLES20.glCullFace(GLES20.GL_BACK);
            program=createProgram(VS,FS);
            aPos=GLES20.glGetAttribLocation(program,"aPos");
            uMvp=GLES20.glGetUniformLocation(program,"uMVP");
            uModel=GLES20.glGetUniformLocation(program,"uModel");
            uColor=GLES20.glGetUniformLocation(program,"uColor");
            uTime=GLES20.glGetUniformLocation(program,"uTime");
            uWater=GLES20.glGetUniformLocation(program,"uWater");
            uCam=GLES20.glGetUniformLocation(program,"uCam");
            cube=Mesh.cube(); wedge=Mesh.wedge(); water=Mesh.waterGrid(44,3.0f);
        }

        @Override public void onSurfaceChanged(GL10 gl,int w,int h) {
            GLES20.glViewport(0,0,w,h);
            Matrix.perspectiveM(proj,0,46f,(float)w/Math.max(1,h),.1f,180f);
        }

        @Override public void onDrawFrame(GL10 gl) {
            long now=System.nanoTime(); float dt=Math.min(.05f,(now-last)/1_000_000_000f); last=now; time=(now-start)/1_000_000_000f;
            update(dt);
            GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT|GLES20.GL_DEPTH_BUFFER_BIT);
            GLES20.glUseProgram(program);

            float ry=(float)Math.toRadians(yaw), rp=(float)Math.toRadians(pitch);
            float cx=(float)(Math.cos(rp)*Math.sin(ry))*distance;
            float cy=(float)Math.sin(rp)*distance*.72f+4f;
            float cz=(float)(Math.cos(rp)*Math.cos(ry))*distance;
            Matrix.setLookAtM(view,0,cx,cy,cz,0,1.0f,0,0,1,0);
            GLES20.glUniform3f(uCam,cx,cy,cz);
            GLES20.glUniform1f(uTime,time);

            Matrix.setIdentityM(model,0);
            draw(water,model,.055f,.25f,.36f,1f,true);

            drawShip(px,pz,-18f,false,0f);
            float sink=Math.max(0,(15-enemyHp)*.035f);
            drawShip(ex,ez,205f,true,sink);

            if (firing) drawShell();
            if (impact) drawImpact();
        }

        private void update(float dt) {
            if(firing){ shellT += dt*.43f; if(shellT>=1f){ firing=false; impact=true; impactT=0; boolean hit=random.nextFloat()<.82f; if(hit){enemyHp=Math.max(0,enemyHp-(18+random.nextInt(18))); if(listener!=null)listener.onHit(enemyHp,enemyHp==0);}else if(listener!=null)listener.onMiss(); }}
            if(impact){ impactT+=dt; if(impactT>1.8f)impact=false; }
        }

        void fire(){ if(firing||enemyHp<=0)return; firing=true; shellT=0; impact=false; }

        private void drawShell(){
            float t=shellT;
            float x=px+(ex-px)*t, z=pz+(ez-pz)*t;
            float y=2.1f+(float)Math.sin(Math.PI*t)*9.0f;
            Matrix.setIdentityM(model,0); Matrix.translateM(model,0,x,y,z); Matrix.scaleM(model,0,.16f,.16f,.42f);
            draw(cube,model,1.0f,.72f,.2f,1,false);
        }

        private void drawImpact(){
            float k=Math.min(1,impactT*2.2f), fade=Math.max(0,1-impactT/1.8f);
            Matrix.setIdentityM(model,0); Matrix.translateM(model,0,ex,1.2f,ez); Matrix.scaleM(model,0,1.5f*k,3.5f*k,1.5f*k);
            draw(cube,model,1f,.42f,.08f,.72f*fade,false);
            Matrix.setIdentityM(model,0); Matrix.translateM(model,0,ex+.5f,1.8f+k*2f,ez-.3f); Matrix.scaleM(model,0,1.0f*k,2.2f*k,1.0f*k);
            draw(cube,model,.12f,.12f,.12f,.60f*fade,false);
        }

        private void drawShip(float x,float z,float rot,boolean enemy,float sink){
            float[] base=new float[16]; Matrix.setIdentityM(base,0); Matrix.translateM(base,0,x,-.15f-sink,z); Matrix.rotateM(base,0,rot,0,1,0); if(sink>0)Matrix.rotateM(base,0,-sink*9f,1,0,0);
            part(cube,base,-.8f,.65f,0,6.5f,.72f,1.38f, enemy?.25f:.34f,enemy?.28f:.38f,enemy?.31f:.42f);
            part(wedge,base,6.25f,.65f,0,2.5f,.72f,1.38f, enemy?.26f:.36f,enemy?.29f:.40f,enemy?.32f:.44f);
            part(cube,base,-1.0f,1.34f,0,5.5f,.12f,1.15f,.23f,.25f,.26f);
            part(cube,base,-.9f,2.02f,0,2.8f,.65f,.92f,.40f,.42f,.42f);
            part(cube,base,-1.5f,2.70f,0,1.55f,.38f,.72f,.34f,.36f,.36f);
            part(cube,base,-1.8f,3.42f,0,.28f,1.05f,.28f,.25f,.26f,.26f);
            part(cube,base,-1.8f,4.32f,0,1.1f,.08f,.08f,.18f,.18f,.18f);
            turret(base,2.3f,1.62f,0,enemy); turret(base,4.15f,1.54f,0,enemy); turret(base,-4.1f,1.54f,180,enemy);
            part(cube,base,-3.0f,2.25f,0,.65f,1.15f,.58f,.36f,.37f,.37f);
            part(cube,base,.2f,2.45f,0,.55f,1.45f,.55f,.31f,.32f,.32f);
        }

        private void turret(float[] base,float x,float y,float localRot,boolean enemy){
            float angle=(float)Math.toDegrees(Math.atan2(ez-pz,ex-px));
            float[] t=local(base,x,y,0,1.05f,.34f,.95f,0,1,0, localRot+(enemy?angle+160:angle+18));
            draw(cube,t,.19f,.20f,.20f,1,false);
            float[] b=local(base,x+.95f,y+.12f,-.24f,1.55f,.09f,.09f,0,1,0,localRot+(enemy?angle+160:angle+18));
            draw(cube,b,.10f,.10f,.10f,1,false);
            b=local(base,x+.95f,y+.12f,.24f,1.55f,.09f,.09f,0,1,0,localRot+(enemy?angle+160:angle+18));
            draw(cube,b,.10f,.10f,.10f,1,false);
        }

        private void part(Mesh mesh,float[] base,float x,float y,float z,float sx,float sy,float sz,float r,float g,float b){
            float[] m=local(base,x,y,z,sx,sy,sz,0,1,0,0); draw(mesh,m,r,g,b,1,false);
        }
        private float[] local(float[] base,float x,float y,float z,float sx,float sy,float sz,float ax,float ay,float az,float ang){
            float[] l=new float[16],out=new float[16]; Matrix.setIdentityM(l,0); Matrix.translateM(l,0,x,y,z); if(ang!=0)Matrix.rotateM(l,0,ang,ax,ay,az); Matrix.scaleM(l,0,sx,sy,sz); Matrix.multiplyMM(out,0,base,0,l,0); return out;
        }

        private void draw(Mesh mesh,float[] mod,float r,float g,float b,float a,boolean isWater){
            Matrix.multiplyMM(mv,0,view,0,mod,0); Matrix.multiplyMM(mvp,0,proj,0,mv,0);
            GLES20.glUniformMatrix4fv(uMvp,1,false,mvp,0); GLES20.glUniformMatrix4fv(uModel,1,false,mod,0);
            GLES20.glUniform4f(uColor,r,g,b,a); GLES20.glUniform1f(uWater,isWater?1f:0f);
            mesh.buffer.position(0); GLES20.glEnableVertexAttribArray(aPos); GLES20.glVertexAttribPointer(aPos,3,GLES20.GL_FLOAT,false,12,mesh.buffer);
            if(a<.99f){GLES20.glEnable(GLES20.GL_BLEND);GLES20.glBlendFunc(GLES20.GL_SRC_ALPHA,GLES20.GL_ONE_MINUS_SRC_ALPHA);} else GLES20.glDisable(GLES20.GL_BLEND);
            GLES20.glDrawArrays(GLES20.GL_TRIANGLES,0,mesh.count); GLES20.glDisableVertexAttribArray(aPos);
        }

        private static int createProgram(String vs,String fs){int v=shader(GLES20.GL_VERTEX_SHADER,vs),f=shader(GLES20.GL_FRAGMENT_SHADER,fs);int p=GLES20.glCreateProgram();GLES20.glAttachShader(p,v);GLES20.glAttachShader(p,f);GLES20.glLinkProgram(p);return p;}
        private static int shader(int type,String src){int s=GLES20.glCreateShader(type);GLES20.glShaderSource(s,src);GLES20.glCompileShader(s);return s;}

        private static final String VS=
                "attribute vec3 aPos; uniform mat4 uMVP; uniform mat4 uModel; uniform float uTime; uniform float uWater; varying vec3 vWorld; varying float vWave;"+
                "void main(){vec3 p=aPos; float w=0.0; if(uWater>0.5){w=sin(p.x*.22+uTime*1.15)*.28+cos(p.z*.18-uTime*.92)*.20+sin((p.x+p.z)*.12+uTime*.6)*.12;p.y+=w;} vec4 wp=uModel*vec4(p,1.0);vWorld=wp.xyz;vWave=w;gl_Position=uMVP*vec4(p,1.0);}";
        private static final String FS=
                "precision mediump float; uniform vec4 uColor; uniform float uWater; uniform vec3 uCam; varying vec3 vWorld; varying float vWave;"+
                "void main(){float d=distance(vWorld,uCam);float fog=clamp((d-34.0)/75.0,0.0,.72);vec3 c=uColor.rgb;if(uWater>0.5){float shine=.10+max(vWave,0.0)*.35;c+=vec3(shine*.35,shine*.55,shine*.72);}else{float light=.72+clamp(vWorld.y*.055,0.0,.22);c*=light;}c=mix(c,vec3(.42,.61,.68),fog);gl_FragColor=vec4(c,uColor.a);}";
    }

    private static class Mesh {
        final FloatBuffer buffer; final int count;
        Mesh(float[] v){count=v.length/3;buffer=ByteBuffer.allocateDirect(v.length*4).order(ByteOrder.nativeOrder()).asFloatBuffer();buffer.put(v).position(0);}
        static Mesh cube(){float[]v={
            -1,-1,1, 1,-1,1, 1,1,1, -1,-1,1, 1,1,1, -1,1,1,
            1,-1,-1, -1,-1,-1, -1,1,-1, 1,-1,-1, -1,1,-1, 1,1,-1,
            -1,-1,-1, -1,-1,1, -1,1,1, -1,-1,-1, -1,1,1, -1,1,-1,
            1,-1,1, 1,-1,-1, 1,1,-1, 1,-1,1, 1,1,-1, 1,1,1,
            -1,1,1, 1,1,1, 1,1,-1, -1,1,1, 1,1,-1, -1,1,-1,
            -1,-1,-1, 1,-1,-1, 1,-1,1, -1,-1,-1, 1,-1,1, -1,-1,1};return new Mesh(v);}
        static Mesh wedge(){float[]v={
            -1,-1,-1,-1,-1,1,-1,1,1, -1,-1,-1,-1,1,1,-1,1,-1,
            -1,-1,-1,1,0,0,-1,-1,1, -1,-1,1,1,0,0,-1,1,1,
            -1,1,1,1,0,0,-1,1,-1, -1,1,-1,1,0,0,-1,-1,-1,
            -1,-1,1,-1,1,1,1,0,0, -1,1,-1,-1,-1,-1,1,0,0};return new Mesh(v);}
        static Mesh waterGrid(int n,float step){float half=n*step*.5f;float[]v=new float[n*n*18];int k=0;for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++){float x=-half+ix*step,z=-half+iz*step,x2=x+step,z2=z+step;float[]q={x,0,z,x2,0,z,x2,0,z2,x,0,z,x2,0,z2,x,0,z2};for(float f:q)v[k++]=f;}return new Mesh(v);}
    }
}
