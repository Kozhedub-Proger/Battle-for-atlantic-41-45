using UnityEngine;

namespace Atlantic4145.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShellProjectile : MonoBehaviour
    {
        public float lifeTime = 20f;
        public float damage = 25f;
        public GameObject impactPrefab;
        private float age;

        public void Launch(Vector3 velocity)
        {
            var rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.velocity = velocity;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifeTime) Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (impactPrefab != null && collision.contactCount > 0)
                Instantiate(impactPrefab, collision.GetContact(0).point, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
