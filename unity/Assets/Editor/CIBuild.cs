using System;
using System.IO;
using System.Linq;
using Atlantic4145.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atlantic4145.Editor
{
    public static class CIBuild
    {
        public static void PerformBuild()
        {
            string version = Environment.GetEnvironmentVariable("ATLANTIC_VERSION");
            if (string.IsNullOrWhiteSpace(version)) version = "0.0.0.000";
            int versionCode = 1;
            int.TryParse(Environment.GetEnvironmentVariable("ATLANTIC_BUILD_CODE"), out versionCode);
            if (versionCode < 1) versionCode = 1;

            PlayerSettings.productName = "Сражения в Атлантике 41–45";
            PlayerSettings.applicationIdentifier = "com.kozhedub.atlantic4145";
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            Directory.CreateDirectory("Assets/Generated");
            Directory.CreateDirectory("../build/Android");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            BuildWater();
            var shellPrefab = BuildShellPrefab();
            var enemy = BuildShip("EnemyShip", new Vector3(22f, 0f, 35f), Quaternion.Euler(0f, 205f, 0f), shellPrefab, false);
            var player = BuildShip("PlayerShip", new Vector3(-18f, 0f, -8f), Quaternion.Euler(0f, 18f, 0f), shellPrefab, true);
            BuildCamera(player.transform, enemy.transform);

            var battery = player.GetComponent<ShipMainBattery>();
            battery.target = enemy.transform;
            battery.manualElevation = 8f;

            string scenePath = "Assets/Generated/Combat.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            string output = GetArg("-customBuildPath");
            if (string.IsNullOrWhiteSpace(output)) output = "../build/Android/Battle-for-Atlantic.apk";
            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                output = Path.Combine(output, "Battle-for-Atlantic.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "../build/Android");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Unity Android build failed: " + report.summary.result);

            Debug.Log($"[CI] Built Unity APK {output} version {version}, size {report.summary.totalSize} bytes");
        }

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.94f, 0.82f);
            lightGo.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            RenderSettings.ambientLight = new Color(0.34f, 0.39f, 0.44f);
        }

        private static void BuildWater()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea_Waterline_Y0";
            water.transform.position = Vector3.zero;
            water.transform.localScale = new Vector3(35f, 1f, 35f);
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.035f, 0.22f, 0.31f, 1f);
            mat.SetFloat("_Glossiness", 0.72f);
            mat.SetFloat("_Metallic", 0.1f);
            water.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static ShellProjectile BuildShellPrefab()
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "ShellProjectile";
            shell.transform.localScale = Vector3.one * 0.22f;
            var rb = shell.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var projectile = shell.AddComponent<ShellProjectile>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.11f, 0.09f);
            shell.GetComponent<Renderer>().sharedMaterial = mat;
            string prefabPath = "Assets/Generated/ShellProjectile.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(shell, prefabPath);
            UnityEngine.Object.DestroyImmediate(shell);
            return prefab.GetComponent<ShellProjectile>();
        }

        private static GameObject BuildShip(string name, Vector3 position, Quaternion rotation, ShellProjectile shellPrefab, bool player)
        {
            var ship = new GameObject(name);
            ship.transform.position = position;
            ship.transform.rotation = rotation;

            var hullMat = MakeMaterial(player ? new Color(0.31f, 0.35f, 0.38f) : new Color(0.27f, 0.29f, 0.31f));
            AddBox(ship.transform, "Hull", new Vector3(0f, -0.55f, 0f), new Vector3(6.5f, 1.25f, 18f), hullMat);
            AddBox(ship.transform, "Deck", new Vector3(0f, 0.18f, 0f), new Vector3(5.8f, 0.25f, 15.5f), MakeMaterial(new Color(0.18f, 0.2f, 0.21f)));
            AddBox(ship.transform, "Superstructure", new Vector3(0f, 1.35f, 1.2f), new Vector3(3.8f, 2.1f, 5.4f), MakeMaterial(new Color(0.46f, 0.48f, 0.49f)));
            AddBox(ship.transform, "Bridge", new Vector3(0f, 2.75f, 1.6f), new Vector3(2.3f, 1.1f, 2.4f), MakeMaterial(new Color(0.5f, 0.52f, 0.52f)));

            var battery = ship.AddComponent<ShipMainBattery>();
            battery.mainBattery.Clear();
            battery.mainBattery.Add(BuildTurret(ship.transform, "Turret_A", new Vector3(0f, 0.85f, 5.2f), shellPrefab));
            battery.mainBattery.Add(BuildTurret(ship.transform, "Turret_B", new Vector3(0f, 0.85f, -5.0f), shellPrefab));
            return ship;
        }

        private static TurretController BuildTurret(Transform ship, string name, Vector3 localPos, ShellProjectile shellPrefab)
        {
            var yaw = new GameObject(name + "_Yaw");
            yaw.transform.SetParent(ship, false);
            yaw.transform.localPosition = localPos;

            AddBox(yaw.transform, "TurretBody", new Vector3(0f, 0.35f, 0f), new Vector3(2.4f, 0.7f, 2.8f), MakeMaterial(new Color(0.2f, 0.21f, 0.22f)));

            var pitch = new GameObject(name + "_Pitch");
            pitch.transform.SetParent(yaw.transform, false);
            pitch.transform.localPosition = new Vector3(0f, 0.42f, 0.25f);

            var controller = yaw.AddComponent<TurretController>();
            controller.yawPivot = yaw.transform;
            controller.pitchPivot = pitch.transform;
            controller.projectilePrefab = shellPrefab;
            controller.muzzleVelocity = 95f;
            controller.shellSpreadDegrees = 0.08f;

            float[] xOffsets = { -0.42f, 0.42f };
            foreach (float x in xOffsets)
            {
                var barrel = AddBox(pitch.transform, "Barrel", new Vector3(x, 0f, 2.0f), new Vector3(0.22f, 0.22f, 4.3f), MakeMaterial(new Color(0.08f, 0.08f, 0.08f)));
                var muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(pitch.transform, false);
                muzzle.transform.localPosition = new Vector3(x, 0f, 4.2f);
                muzzle.transform.localRotation = Quaternion.identity;
                controller.muzzles.Add(muzzle.transform);
            }
            return controller;
        }

        private static void BuildCamera(Transform player, Transform enemy)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.16f, 0.22f);
            Vector3 mid = Vector3.Lerp(player.position, enemy.position, 0.5f);
            camGo.transform.position = mid + new Vector3(-34f, 22f, -38f);
            camGo.transform.LookAt(mid + Vector3.up * 1.5f);
            cam.fieldOfView = 48f;
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.22f);
            return mat;
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
