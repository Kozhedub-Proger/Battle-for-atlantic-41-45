using UnityEngine;

namespace Atlantic4145 {
public class Projectile : MonoBehaviour {
    public Vector3 velocity;
    public float gravity = 9.81f;
    public float life = 18f;
    public GameObject owner;
    void Update(){
        float dt=Time.deltaTime;
        velocity += Vector3.down*gravity*dt;
        Vector3 next=transform.position+velocity*dt;
        Vector3 d=next-transform.position;
        if(Physics.Raycast(transform.position,d.normalized,out RaycastHit hit,d.magnitude) && hit.collider.gameObject!=owner){
            Debug.Log("Shell impact: "+hit.collider.name+" at "+hit.point);
            Destroy(gameObject); return;
        }
        transform.position=next;
        if(transform.position.y<=0f){ Debug.Log("Shell splash at "+transform.position); Destroy(gameObject); return; }
        life-=dt; if(life<=0)Destroy(gameObject);
    }
}
}
