using System.Collections.Generic;
using UnityEngine;

namespace Atlantic4145.Combat
{
    public sealed class TurretController : MonoBehaviour
    {
        public Transform yawPivot;
        public Transform pitchPivot;
        public List<Transform> muzzles = new();
        public float traverseSpeed = 28f;
        public float elevationSpeed = 18f;
        public float minElevation = -3f;
        public float maxElevation = 35f;
        public ShellProjectile projectilePrefab;
        public float muzzleVelocity = 120f;
        public float shellSpreadDegrees = 0.18f;

        private float wantedYaw;
        private float wantedElevation;

        public void Aim(float yawDeg, float elevationDeg)
        {
            wantedYaw = yawDeg;
            wantedElevation = Mathf.Clamp(elevationDeg, minElevation, maxElevation);
        }

        private void Update()
        {
            if (yawPivot != null)
                yawPivot.localRotation = Quaternion.RotateTowards(yawPivot.localRotation, Quaternion.Euler(0f, wantedYaw, 0f), traverseSpeed * Time.deltaTime);
            if (pitchPivot != null)
                pitchPivot.localRotation = Quaternion.RotateTowards(pitchPivot.localRotation, Quaternion.Euler(-wantedElevation, 0f, 0f), elevationSpeed * Time.deltaTime);
        }

        public int FireSalvo()
        {
            if (projectilePrefab == null) return 0;
            int fired = 0;
            foreach (var muzzle in muzzles)
            {
                if (muzzle == null) continue;
                var shell = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
                float yaw = Random.Range(-shellSpreadDegrees, shellSpreadDegrees);
                float pitch = Random.Range(-shellSpreadDegrees, shellSpreadDegrees);
                Vector3 direction = Quaternion.Euler(pitch, yaw, 0f) * muzzle.forward;
                shell.Launch(direction * muzzleVelocity);
                fired++;
            }
            return fired;
        }
    }
}
