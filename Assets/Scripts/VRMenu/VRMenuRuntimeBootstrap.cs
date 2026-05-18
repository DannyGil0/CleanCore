using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns the in-world VR menu at play time if it is missing from the scene.
/// </summary>
public static class VRMenuRuntimeBootstrap
{
    const string PrefabResourcePath = "InWorldMenuVR";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || !scene.IsValid())
            return;

        if (Object.FindFirstObjectByType<InWorldMenuVR>() != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab != null)
        {
            Object.Instantiate(prefab);
            Debug.Log("[VRMenu] Menu instanciado desde Resources/InWorldMenuVR");
            VRMenuSceneServices.EnsureEventSystem();
            VRMenuSceneServices.WireSceneReferences(Object.FindFirstObjectByType<InWorldMenuVR>().gameObject);
            return;
        }

        VRMenuFactory.CreateMenuInScene();
        Debug.Log("[VRMenu] Menu generado en runtime (no habia prefab en escena ni en Resources).");
    }
}
