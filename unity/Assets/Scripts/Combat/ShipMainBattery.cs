using System.Collections.Generic;
using UnityEngine;

namespace Atlantic4145.Combat
{
    public sealed class ShipMainBattery : MonoBehaviour
    {
        public List<TurretController> mainBattery = new();
        public Transform target;
        public KeyCode fireKey = KeyCode.Space;
        public float manualElevation = 8f;

        private void Update()
        {
            AimTurrets();
            if (Input.GetKeyDown(fireKey)) FireAll();
        }

        public void AimTurrets()
        {
            if (target == null) return;
            foreach (var turret in mainBattery)
            {
                if (turret == null || turret.yawPivot == null) continue;
                Vector3 local = transform.InverseTransformDirection((target.position - turret.yawPivot.position).normalized);
                float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                turret.Aim(yaw, manualElevation);
            }
        }

        public int FireAll()
        {
            int shells = 0;
            foreach (var turret in mainBattery)
                if (turret != null) shells += turret.FireSalvo();
            Debug.Log($"[MainBattery] Salvo fired: {shells} shells");
            return shells;
        }
    }
}
