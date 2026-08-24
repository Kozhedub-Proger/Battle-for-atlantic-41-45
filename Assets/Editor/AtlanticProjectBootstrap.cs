#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BattleForAtlantic;

namespace BattleForAtlantic.EditorTools
{
    [InitializeOnLoad]
    public static class AtlanticProjectBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string MarkerPath = "Assets/.atlantic_bootstrapped";
        private const string UrpFolder = "Assets/Settings";
        private const string UrpAssetPath = "Assets/Settings/Atlantic_URP.asset";
        private const string WaterMaterialPath = "Assets/Materials/Water_Placeholder.mat";

        static AtlanticProjectBootstrap()
        {
            EditorApplication.delayCall += EnsureProject;
        }

        [MenuItem("Battle for Atlantic/Fix URP and rebuild starter scene")]
        public static void FixUrpAndRebuild()
        {
            CreateFolders();
            EnsureUrp();
            CreateStarterScene(true);
            Debug.Log("[BattleForAtlantic] URP fixed and starter scene rebuilt.");
        }

        [MenuItem("Battle for Atlantic/Rebuild starter scene")]
        public static void RebuildStarterScene()
        {
            CreateFolders();
            EnsureUrp();
            CreateStarterScene(true);
        }

        private static void EnsureProject()
        {
            CreateFolders();
            EnsureUrp();

            PlayerSettings.productName = "Сражения в Атлантике 41–45";
            PlayerSettings.companyName = "Kozhedub-Proger";
            PlayerSettings.bundleVersion = "0.0.0.002";

            // Existing bootstrap scene from v0.0.0.001 may contain a magenta material,
            // so repair it automatically after URP becomes active.
            if (!File.Exists(ScenePath))
                CreateStarterScene(false);
            else
                RepairExistingWaterMaterial();

            if (!File.Exists(MarkerPath))
            {
                File.WriteAllText(MarkerPath, "Unity 6.3 LTS project initialized.\n");
                AssetDatabase.Refresh();
            }
        }

        private static void CreateFolders()
        {
            string[] dirs =
            {
                "Assets/Scenes", "Assets/Ships", "Assets/Water", "Assets/Scripts",
                "Assets/Materials", "Assets/Prefabs", "Assets/Textures", UrpFolder
            };
            foreach (string dir in dirs)
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void EnsureUrp()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);

            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                pipeline.name = "Atlantic_URP";
                pipeline.LoadBuiltinRendererData(RendererType.UniversalRenderer);
                pipeline.supportsCameraDepthTexture = true;
                pipeline.supportsCameraOpaqueTexture = true;
                pipeline.supportsHDR = true;
                pipeline.useSRPBatcher = true;
                AssetDatabase.CreateAsset(pipeline, UrpAssetPath);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }

        private static Material CreateOrRepairWaterMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogError("[BattleForAtlantic] URP/Lit shader was not found. Check that com.unity.render-pipelines.universal is installed.");
                return mat;
            }

            if (mat == null)
            {
                mat = new Material(shader) { name = "Water_Placeholder" };
                AssetDatabase.CreateAsset(mat, WaterMaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            Color water = new Color(0.018f, 0.19f, 0.27f, 1f);
            mat.SetColor("_BaseColor", water);
            mat.SetFloat("_Smoothness", 0.82f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void RepairExistingWaterMaterial()
        {
            Material waterMat = CreateOrRepairWaterMaterial();
            if (waterMat == null) return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject ocean = GameObject.Find("Ocean_Placeholder");
            if (ocean != null && ocean.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.sharedMaterial = waterMat;
                EditorUtility.SetDirty(renderer);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void CreateStarterScene(bool force)
        {
            if (!force && File.Exists(ScenePath)) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            GameObject world = new GameObject("WORLD");

            GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean_Placeholder";
            ocean.transform.SetParent(world.transform);
            ocean.transform.position = Vector3.zero;
            ocean.transform.localScale = new Vector3(100f, 1f, 100f);
            ocean.GetComponent<Renderer>().sharedMaterial = CreateOrRepairWaterMaterial();

            GameObject shipA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shipA.name = "SHIP_A_MODEL_GOES_HERE";
            shipA.transform.SetParent(world.transform);
            shipA.transform.position = new Vector3(-25f, 2f, 0f);
            shipA.transform.localScale = new Vector3(8f, 4f, 42f);

            GameObject shipB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shipB.name = "SHIP_B_MODEL_GOES_HERE";
            shipB.transform.SetParent(world.transform);
            shipB.transform.position = new Vector3(25f, 2f, 45f);
            shipB.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            shipB.transform.localScale = new Vector3(8f, 4f, 42f);

            GameObject focus = new GameObject("CameraFocus");
            focus.transform.SetParent(world.transform);
            focus.transform.position = new Vector3(0f, 3f, 20f);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 55f;
            OrbitCamera orbit = cameraGo.AddComponent<OrbitCamera>();
            orbit.target = focus.transform;

            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sunGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.62f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.28f, 0.33f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.07f, 0.075f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleForAtlantic] Starter scene created: " + ScenePath);
        }
    }
}
#endif
