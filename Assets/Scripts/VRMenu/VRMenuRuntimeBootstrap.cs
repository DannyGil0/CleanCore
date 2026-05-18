using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns the in-world VR menu at play time and after every scene load (including LoadScene reloads).
/// </summary>
public static class VRMenuRuntimeBootstrap
{
    const string PrefabResourcePath = "InWorldMenuVR";

    static bool _sceneCallbackRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RegisterSceneLoadedCallback()
    {
        if (_sceneCallbackRegistered)
            return;

        _sceneCallbackRegistered = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        ScheduleEnsureMenu();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying)
            return;

        ScheduleEnsureMenu();
    }

    /// <summary>
    /// Deferred so XR Origin / cameras exist after scene reload (LoadScene).
    /// </summary>
    public static void ScheduleEnsureMenu()
    {
        if (!Application.isPlaying)
            return;

        var runner = new GameObject("VRMenuBootstrapRunner");
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<VRMenuBootstrapRunner>();
    }

    public static void EnsureMenuInScene()
    {
        InWorldMenuVR existing = Object.FindFirstObjectByType<InWorldMenuVR>();
        if (existing != null && IsMenuHierarchyValid(existing))
        {
            VRMenuSceneServices.FinalizeMenu(existing.gameObject);
            existing.ShowMenuWhenReady();
            Debug.Log("[VRMenu] Menu existente en escena — referencias actualizadas.");
            return;
        }

        if (existing != null)
            Object.Destroy(existing.gameObject);

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

    static bool IsMenuHierarchyValid(InWorldMenuVR menu)
    {
        return menu != null && menu.transform.Find("MenuCanvas") != null;
    }
}

/// <summary>
/// Runs EnsureMenuInScene after the scene and XR rig have initialized.
/// </summary>
sealed class VRMenuBootstrapRunner : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        yield return null;

        VRMenuRuntimeBootstrap.EnsureMenuInScene();
        Destroy(gameObject);
    }
}
