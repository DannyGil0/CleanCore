using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Shared scene wiring for VR menu (runtime + editor).
/// </summary>
public static class VRMenuSceneServices
{
    public static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            go.AddComponent<XRUIInputModule>();
            Debug.Log("[VRMenu] EventSystem creado con XRUIInputModule.");
            return;
        }

        if (es.GetComponent<XRUIInputModule>() == null)
            es.gameObject.AddComponent<XRUIInputModule>();

        if (es.GetComponent<StandaloneInputModule>() is { } standalone)
            standalone.enabled = false;

        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() is { } inputModule)
            inputModule.enabled = false;
    }

    public static void WireSceneReferences(GameObject menuRoot)
    {
        if (menuRoot == null)
            return;

        var menu = menuRoot.GetComponent<InWorldMenuVR>();
        var placement = menuRoot.GetComponent<InWorldMenuPlacement>();
        var stats = menuRoot.GetComponent<CleaningStatsAggregator>();
        var audioMgr = menuRoot.GetComponent<AudioManager>();
        var binder = menuRoot.GetComponent<VRMenuUIBinder>();

        XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
        PaintableSurfaceUI surfaceUI = Object.FindFirstObjectByType<PaintableSurfaceUI>();

        if (origin != null)
        {
            SetField(placement, "_xrOrigin", origin);
            SetField(menu, "_xrOrigin", origin);
        }

        if (surfaceUI != null)
            SetField(stats, "_surfaceUI", surfaceUI);

        if (audioMgr != null)
            SetField(menu, "_audioManager", audioMgr);
        if (stats != null)
            SetField(menu, "_statsAggregator", stats);

        var help = menuRoot.GetComponentInChildren<HelpPanelController>(true);
        if (help != null)
            SetField(menu, "_helpPanel", help);

        HapticImpulsePlayer left = null, right = null;
        foreach (HapticImpulsePlayer h in Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsSortMode.None))
        {
            string n = h.gameObject.name.ToLowerInvariant();
            if (n.Contains("left") && left == null) left = h;
            if (n.Contains("right") && right == null) right = h;
        }

        foreach (VRMenuButtonFeedback fb in menuRoot.GetComponentsInChildren<VRMenuButtonFeedback>(true))
        {
            SetField(fb, "_leftHaptic", left);
            SetField(fb, "_rightHaptic", right);
        }

        WireButtons(menuRoot, menu, binder);
    }

    public static void FinalizeMenu(GameObject menuRoot)
    {
        if (menuRoot == null)
            return;

        EnsureEventSystem();
        WireSceneReferences(menuRoot);

        var menu = menuRoot.GetComponent<InWorldMenuVR>();
        if (menu == null)
            return;

        Transform canvas = menuRoot.transform.Find("MenuCanvas");
        if (canvas != null)
        {
            SetField(menu, "_menuCanvasRoot", canvas.gameObject);
            VRMenuWorldCanvasDriver.RefreshCamera(canvas.gameObject);
        }

        if (menuRoot.GetComponent<VRMenuToggleInput>() == null)
            menuRoot.AddComponent<VRMenuToggleInput>();

        var binder = menuRoot.GetComponent<VRMenuUIBinder>();
        if (binder != null)
            binder.Bind();
    }

    static void WireButtons(GameObject menuRoot, InWorldMenuVR menu, VRMenuUIBinder binder)
    {
        if (binder == null)
            return;

        SetField(binder, "_menu", menu);
        foreach (UnityEngine.UI.Button b in menuRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            switch (b.gameObject.name)
            {
                case "BtnRecenter": SetField(binder, "_btnRecenter", b); break;
                case "BtnReset": SetField(binder, "_btnReset", b); break;
                case "BtnHelp": SetField(binder, "_btnHelp", b); break;
                case "BtnClose": SetField(binder, "_btnClose", b); break;
                case "BtnExit": SetField(binder, "_btnExit", b); break;
            }
        }
    }

    static void SetField(object target, string fieldName, object value)
    {
        if (target == null) return;
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
