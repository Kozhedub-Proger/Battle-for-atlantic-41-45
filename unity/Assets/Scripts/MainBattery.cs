using System.Collections.Generic;
using UnityEngine;

namespace Atlantic4145 {
public class MainBattery : MonoBehaviour {
    [System.Serializable] public class TurretRig { public Transform yaw; public Transform pitch; public List<Transform> muzzles=new(); }
    public List<TurretRig> turrets=new();
    public Transform target;
    public float traverseSpeed=24f, elevationSpeed=12f, muzzleVelocity=95f;
    public float minElevation=-2f,maxElevation=35f;
    public GameObject projectilePrefab;

    void Update(){
        if(!target)return;
        foreach(var t in turrets){
            Vector3 local=t.yaw.parent.InverseTransformPoint(target.position); local.y=0;
            if(local.sqrMagnitude>.01f){ Quaternion q=Quaternion.LookRotation(local); t.yaw.localRotation=Quaternion.RotateTowards(t.yaw.localRotation,q,traverseSpeed*Time.deltaTime); }
            float dist=Vector3.Distance(t.pitch.position,target.position);
            float dy=target.position.y-t.pitch.position.y;
            float g=9.81f,v=muzzleVelocity,v2=v*v;
            float disc=v2*v2-g*(g*dist*dist+2*dy*v2);
            float elev=disc>0 ? Mathf.Atan((v2-Mathf.Sqrt(disc))/(g*dist))*Mathf.Rad2Deg : 12f;
            elev=Mathf.Clamp(elev,minElevation,maxElevation);
            Quaternion pq=Quaternion.Euler(-elev,0,0);
            t.pitch.localRotation=Quaternion.RotateTowards(t.pitch.localRotation,pq,elevationSpeed*Time.deltaTime);
        }
        if(Input.GetKeyDown(KeyCode.Space))FireSalvo();
    }

    public void FireSalvo(){
        foreach(var t in turrets) foreach(var m in t.muzzles){
            GameObject shell=projectilePrefab?Instantiate(projectilePrefab,m.position,m.rotation):GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name="AP Shell"; shell.transform.position=m.position; shell.transform.localScale=Vector3.one*.18f;
            var col=shell.GetComponent<Collider>(); if(col)col.isTrigger=true;
            var p=shell.GetComponent<Projectile>()??shell.AddComponent<Projectile>();
            p.owner=gameObject; p.velocity=m.forward*muzzleVelocity;
        }
    }
}
}
