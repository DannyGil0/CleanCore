using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns the in-world VR menu at play time if it is missing from the scene.
/// This was the original working path before hide-on-start was added.
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

        EnsureMenuInScene();
    }

    public static void EnsureMenuInScene()
    {
        InWorldMenuVR existing = Object.FindFirstObjectByType<InWorldMenuVR>();
        if (existing != null)
        {
            VRMenuSceneServices.FinalizeMenu(existing.gameObject);
            existing.ShowMenuWhenReady();
            Debug.Log("[VRMenu] Menu existente en escena — referencias actualizadas.");
            return;
        }

        VRMenuSceneServices.EnsureEventSystem();

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab != null)
        {
            Object.Instantiate(prefab);
            Debug.Log("[VRMenu] Menu instanciado desde Resources/InWorldMenuVR");
        }
        else
        {
            VRMenuFactory.CreateMenuInScene();
            Debug.Log("[VRMenu] Menu generado en runtime (no habia prefab en escena ni en Resources).");
        }

        InWorldMenuVR menu = Object.FindFirstObjectByType<InWorldMenuVR>();
        if (menu != null)
        {
            VRMenuSceneServices.FinalizeMenu(menu.gameObject);
            menu.ShowMenuWhenReady();
        }
        else
        {
            Debug.LogError("[VRMenu] Fallo al crear el menu. Revisa errores de compilacion en la consola.");
        }
    }
}
