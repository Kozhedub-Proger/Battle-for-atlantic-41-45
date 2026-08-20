using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Atlantic4145 {
public class BattleBootstrap : MonoBehaviour {
    Material sea,gray,dark;
    void Start(){
        Application.targetFrameRate=60;
        sea=Mat(new Color(.04f,.24f,.34f)); gray=Mat(new Color(.35f,.38f,.40f)); dark=Mat(new Color(.12f,.14f,.15f));
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.48f,.64f,.70f); RenderSettings.fogDensity=.006f;
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.15f; sun.transform.rotation=Quaternion.Euler(42,-35,0);
        var ocean=GameObject.CreatePrimitive(PrimitiveType.Plane); ocean.name="Atlantic Ocean / waterline Y=0"; ocean.transform.localScale=new Vector3(35,1,35); ocean.GetComponent<Renderer>().material=sea;
        var a=Ship("Ship A",new Vector3(-18,-1.45f,0),Quaternion.Euler(0,90,0));
        var b=Ship("Ship B",new Vector3(18,-1.45f,8),Quaternion.Euler(0,-90,0));
        a.GetComponent<MainBattery>().target=b.transform; b.GetComponent<MainBattery>().target=a.transform;
        Camera.main.transform.position=new Vector3(-25,14,-24); Camera.main.transform.LookAt(Vector3.zero); Camera.main.fieldOfView=48;
        MakeUI(a.GetComponent<MainBattery>());
    }
    GameObject Ship(string name,Vector3 pos,Quaternion rot){
        var root=new GameObject(name); root.transform.SetPositionAndRotation(pos,rot);
        Box(root,"Hull",new Vector3(0,1.2f,0),new Vector3(15,2.6f,3.4f),gray);
        Box(root,"Deck",new Vector3(-.5f,2.55f,0),new Vector3(11,.45f,2.9f),dark);
        Box(root,"Superstructure",new Vector3(-1,3.7f,0),new Vector3(4.4f,2.0f,2.2f),gray);
        Box(root,"Bridge",new Vector3(-1.8f,5.0f,0),new Vector3(2.2f,.8f,1.7f),gray);
        Box(root,"Mast",new Vector3(-1.6f,7.0f,0),new Vector3(.18f,3.5f,.18f),dark);
        var mb=root.AddComponent<MainBattery>();
        AddTurret(root,mb,new Vector3(4.6f,3.25f,0),2); AddTurret(root,mb,new Vector3(2.2f,3.25f,0),2);
        return root;
    }
    void AddTurret(GameObject ship,MainBattery mb,Vector3 p,int guns){
        var yaw=new GameObject("Turret_Yaw").transform; yaw.SetParent(ship.transform,false); yaw.localPosition=p;
        Box(yaw.gameObject,"TurretBody",Vector3.zero,new Vector3(1.8f,.7f,1.7f),dark);
        var pitch=new GameObject("Gun_Pitch").transform; pitch.SetParent(yaw,false); pitch.localPosition=new Vector3(.7f,.25f,0);
        var rig=new MainBattery.TurretRig{yaw=yaw,pitch=pitch};
        for(int i=0;i<guns;i++){
            float z=(i-(guns-1)*.5f)*.55f;
            var barrel=GameObject.CreatePrimitive(PrimitiveType.Cylinder); barrel.name="Barrel_"+(i+1); barrel.transform.SetParent(pitch,false);
            barrel.transform.localPosition=new Vector3(1.5f,0,z); barrel.transform.localRotation=Quaternion.Euler(0,0,-90); barrel.transform.localScale=new Vector3(.11f,1.5f,.11f); barrel.GetComponent<Renderer>().material=dark;
            var muzzle=new GameObject("Muzzle_"+(i+1)).transform; muzzle.SetParent(pitch,false); muzzle.localPosition=new Vector3(3.05f,0,z); muzzle.localRotation=Quaternion.Euler(0,90,0); rig.muzzles.Add(muzzle);
        }
        mb.turrets.Add(rig);
    }
    void MakeUI(MainBattery mb){
        var canvas=new GameObject("UI").AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.gameObject.AddComponent<CanvasScaler>(); canvas.gameObject.AddComponent<GraphicRaycaster>();
        var go=new GameObject("FIRE MAIN BATTERY"); go.transform.SetParent(canvas.transform,false); var b=go.AddComponent<Button>(); var img=go.AddComponent<Image>(); img.color=new Color(.45f,.08f,.06f,.9f);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(.78f,.04f);rt.anchorMax=new Vector2(.97f,.14f);rt.offsetMin=rt.offsetMax=Vector2.zero; b.onClick.AddListener(mb.FireSalvo);
        var txt=new GameObject("Text").AddComponent<Text>(); txt.transform.SetParent(go.transform,false);txt.text="ОГОНЬ ГК  (4 СНАРЯДА)";txt.alignment=TextAnchor.MiddleCenter;txt.font=Resources.GetBuiltinResource<Font>("Arial.ttf");txt.fontSize=26;txt.color=Color.white; var tr=txt.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
    }
    static Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;return m;}
    static void Box(GameObject parent,string name,Vector3 lp,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent.transform,false);g.transform.localPosition=lp;g.transform.localScale=scale;g.GetComponent<Renderer>().material=mat;}
}
}
