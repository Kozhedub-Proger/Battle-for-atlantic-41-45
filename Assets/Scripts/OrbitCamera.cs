using UnityEngine;

namespace BattleForAtlantic
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 85f;
        public float minDistance = 20f;
        public float maxDistance = 220f;
        public float yaw = 35f;
        public float pitch = 22f;
        public float rotateSpeed = 3.5f;
        public float zoomSpeed = 15f;

        private void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetMouseButton(0))
            {
                yaw += Input.GetAxis("Mouse X") * rotateSpeed;
                pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
                pitch = Mathf.Clamp(pitch, 5f, 80f);
            }

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f)
                distance = Mathf.Clamp(distance - wheel * zoomSpeed, minDistance, maxDistance);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.SetPositionAndRotation(target.position + offset, rotation);
        }
    }
}
