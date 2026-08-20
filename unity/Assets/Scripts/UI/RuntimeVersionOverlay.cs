using UnityEngine;
using Atlantic4145.Combat;

namespace Atlantic4145.UI
{
    public sealed class RuntimeVersionOverlay : MonoBehaviour
    {
        private ShipMainBattery battery;
        private GUIStyle versionStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("RuntimeVersionOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<RuntimeVersionOverlay>();
        }

        private void Awake()
        {
            battery = FindObjectOfType<ShipMainBattery>();
            versionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.dpi / 220f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            GUI.Label(new Rect(sw - 390, sh - 52, 370, 34), "Версия " + Application.version, versionStyle);

            if (battery == null) battery = FindObjectOfType<ShipMainBattery>();
            if (battery != null && GUI.Button(new Rect(sw - 240, sh - 125, 210, 62), "ОГОНЬ ГК", buttonStyle))
                battery.FireAll();
        }
    }
}
