using UnityEngine;

namespace Atlantic4145 {
public static class RuntimeEntry {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){
        if(!Camera.main){var c=new GameObject("Main Camera");c.tag="MainCamera";c.AddComponent<Camera>();c.AddComponent<AudioListener>();}
        if(!Object.FindObjectOfType<BattleBootstrap>()) new GameObject("Atlantic 41-45 Bootstrap").AddComponent<BattleBootstrap>();
    }
}
}
