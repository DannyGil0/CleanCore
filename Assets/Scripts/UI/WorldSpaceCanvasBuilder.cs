using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Shared helpers for World Space Canvas UI in VR (menus, scoreboards, HUD panels).
/// See Docs/WORLD_SPACE_CANVAS_AGENT_CONTEXT.md for conventions.
/// </summary>
public static class WorldSpaceCanvasBuilder
{
    public const float DefaultScale = 0.002f;
    public const float DefaultPixelsPerUnit = 10f;

    /// <summary>
    /// Creates a World Space canvas with the standard VR scale/size pattern used by InWorldMenuVR.
    /// </summary>
    public static GameObject CreateCanvas(Transform parent, string canvasName, Vector2 sizeDelta,
        int sortingOrder = 10, bool interactive = true)
    {
        if (interactive)
            VRMenuSceneServices.EnsureEventSystem();

        GameObject canvasGo = new GameObject(canvasName, typeof(RectTransform));
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localScale = Vector3.one * DefaultScale;
        canvasGo.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = sizeDelta;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = DefaultPixelsPerUnit;

        if (interactive)
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        canvasGo.AddComponent<VRMenuWorldCanvasDriver>();
        return canvasGo;
    }

    public static void FinalizeCanvas(GameObject canvasGo)
    {
        if (canvasGo != null)
            VRMenuWorldCanvasDriver.RefreshCamera(canvasGo);
    }

    public static GameObject CreatePanel(Transform parent, string name, Color background,
        Vector2 paddingMin, Vector2 paddingMax)
    {
        GameObject panel = CreateUIObject(name, parent);
        panel.AddComponent<Image>().color = background;
        StretchFull(panel.GetComponent<RectTransform>());
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.offsetMin = paddingMin;
        panelRect.offsetMax = paddingMax;
        return panel;
    }

    public static VerticalLayoutGroup AddVerticalLayout(GameObject panel, RectOffset padding, float spacing)
    {
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = padding;
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        return vlg;
    }

    public static TMP_Text CreateTMP(Transform parent, string name, string text, float fontSize,
        FontStyles style, Color color, float preferredHeight, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);
        if (preferredHeight > 0f)
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        return tmp;
    }

    public static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Orients a world-space UI root so its front face (-local Z) points toward the viewer.
    /// </summary>
    public static void FaceCamera(Transform root, Vector3 worldPosition, Vector3 cameraPosition)
    {
        if (root == null)
            return;

        Vector3 flatToCamera = cameraPosition - worldPosition;
        flatToCamera.y = 0f;
        if (flatToCamera.sqrMagnitude < 0.001f)
            flatToCamera = Vector3.forward;

        root.SetPositionAndRotation(worldPosition, Quaternion.LookRotation(-flatToCamera.normalized, Vector3.up));
    }
}
