using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Builds the in-world VR menu hierarchy at runtime.
/// </summary>
public static class VRMenuFactory
{
    static readonly Color PanelBg = new Color(0.95f, 0.98f, 1f, 0.92f);
    static readonly Color BtnNormal = new Color(0.82f, 0.93f, 0.95f, 1f);
    static readonly Color BtnHighlight = new Color(0.55f, 0.82f, 0.88f, 1f);
    static readonly Color BtnPressed = new Color(0.4f, 0.72f, 0.8f, 1f);
    static readonly Color TitleColor = new Color(0.15f, 0.35f, 0.45f, 1f);
    static readonly Color TextColor = new Color(0.2f, 0.3f, 0.35f, 1f);

    public static GameObject CreateMenuInScene()
    {
        VRMenuSceneServices.EnsureEventSystem();

        GameObject root = new GameObject("InWorldMenuVR");

        var menu = root.AddComponent<InWorldMenuVR>();
        root.AddComponent<InWorldMenuPlacement>();
        root.AddComponent<VRMenuUIBinder>();
        root.AddComponent<VRMenuToggleInput>();
        var audioMgr = root.AddComponent<AudioManager>();
        var stats = root.AddComponent<CleaningStatsAggregator>();

        BuildCanvas(root.transform, menu, audioMgr, stats, out VRMenuUIBinder binder);

        VRMenuSceneServices.FinalizeMenu(root);
        menu.ShowMenuWhenReady();
        return root;
    }

    static void BuildCanvas(Transform root, InWorldMenuVR menu, AudioManager audioMgr,
        CleaningStatsAggregator stats, out VRMenuUIBinder binder)
    {
        binder = root.GetComponent<VRMenuUIBinder>();

        GameObject canvasGo = WorldSpaceCanvasBuilder.CreateCanvas(
            root, "MenuCanvas", new Vector2(1000, 650), sortingOrder: 20, interactive: true);

        GameObject panel = CreateUIObject("Panel", canvasGo.transform);
        panel.AddComponent<Image>().color = PanelBg;
        WorldSpaceCanvasBuilder.StretchFull(panel.GetComponent<RectTransform>());
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.offsetMin = new Vector2(20, 20);
        panelRect.offsetMax = new Vector2(-20, -20);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;

        CreateTMP("Title", panel.transform, "CONFIGURACIÓN", 42, FontStyles.Bold, TitleColor, 56);

        GameObject statsRow = CreateUIObject("StatsRow", panel.transform);
        statsRow.AddComponent<LayoutElement>().preferredHeight = 40;
        var hlg = statsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        CreateTMP("StatsLabel", statsRow.transform, "STATS", 28, FontStyles.Bold, TextColor, 36);
        TMP_Text statsLabel = CreateTMP("StatsValue", statsRow.transform, "Limpieza: 0.0%", 28, FontStyles.Normal, TitleColor, 36);

        Slider sliderAmb = CreateSliderBlock(panel.transform, "VOLUMEN AMBIENTE", 0.7f);
        Slider sliderMus = CreateSliderBlock(panel.transform, "VOLUMEN MÚSICA", 0.4f);

        Button btnRecenter = CreateMenuButton(panel.transform, "BtnRecenter", "RECENTRAR VISTA");
        Button btnReset = CreateMenuButton(panel.transform, "BtnReset", "REINICIAR ESCENA");
        Button btnHelp = CreateMenuButton(panel.transform, "BtnHelp", "AYUDA (CONTROLES BÁSICOS)");
        Button btnClose = CreateMenuButton(panel.transform, "BtnClose", "CERRAR");
        Button btnExit = CreateMenuButton(panel.transform, "BtnExit", "SALIR");

        GameObject helpRoot = CreateUIObject("HelpPanel", canvasGo.transform);
        WorldSpaceCanvasBuilder.StretchFull(helpRoot.GetComponent<RectTransform>());
        helpRoot.AddComponent<Image>().color = new Color(0.1f, 0.2f, 0.25f, 0.85f);
        helpRoot.SetActive(false);
        GameObject helpTextGo = CreateUIObject("HelpText", helpRoot.transform);
        var helpRect = helpTextGo.GetComponent<RectTransform>();
        WorldSpaceCanvasBuilder.StretchFull(helpRect);
        helpRect.offsetMin = new Vector2(40, 100);
        helpRect.offsetMax = new Vector2(-40, -40);
        TMP_Text helpText = helpTextGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            helpText.font = TMP_Settings.defaultFontAsset;
        helpText.fontSize = 26;
        helpText.color = Color.white;
        helpText.alignment = TextAlignmentOptions.TopLeft;

        Button btnHelpBack = CreateHelpBackButton(helpRoot.transform);
        var help = helpRoot.AddComponent<HelpPanelController>();
        help.Configure(helpRoot, helpText, btnHelpBack);

        GameObject modalRoot = CreateUIObject("ModalOverlay", canvasGo.transform);
        WorldSpaceCanvasBuilder.StretchFull(modalRoot.GetComponent<RectTransform>());
        modalRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        modalRoot.SetActive(false);

        GameObject modalPanel = CreateUIObject("ModalPanel", modalRoot.transform);
        var mpRect = modalPanel.GetComponent<RectTransform>();
        mpRect.anchorMin = mpRect.anchorMax = new Vector2(0.5f, 0.5f);
        mpRect.sizeDelta = new Vector2(700, 320);
        modalPanel.AddComponent<Image>().color = PanelBg;
        var modalVlg = modalPanel.AddComponent<VerticalLayoutGroup>();
        modalVlg.padding = new RectOffset(24, 24, 24, 24);
        modalVlg.spacing = 20;
        modalVlg.childAlignment = TextAnchor.MiddleCenter;

        TMP_Text modalMsg = CreateTMP("ModalMessage", modalPanel.transform, "¿Confirmar?", 30, FontStyles.Normal, TextColor, 120);
        GameObject modalBtns = CreateUIObject("ModalButtons", modalPanel.transform);
        modalBtns.AddComponent<LayoutElement>().preferredHeight = 56;
        var modalH = modalBtns.AddComponent<HorizontalLayoutGroup>();
        modalH.spacing = 24;
        Button modalYes = CreateMenuButton(modalBtns.transform, "BtnYes", "Sí");
        Button modalNo = CreateMenuButton(modalBtns.transform, "BtnNo", "No");

        WireButtonFeedback(canvasGo.transform);

        menu.WireReferences(sliderAmb, sliderMus, statsLabel, modalRoot, modalMsg, modalYes, modalNo,
            audioMgr, stats, help, btnRecenter, btnReset, btnHelp, btnExit, btnClose, canvasGo, binder);
    }

    static void WireButtonFeedback(Transform canvasRoot)
    {
        AudioSource src = canvasRoot.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        foreach (Button btn in canvasRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<VRMenuButtonFeedback>() == null)
            {
                var fb = btn.gameObject.AddComponent<VRMenuButtonFeedback>();
                fb.Configure(btn.GetComponent<Image>(), src);
            }
        }
    }

    static Slider CreateSliderBlock(Transform parent, string label, float defaultValue)
    {
        GameObject block = CreateUIObject(label.Replace(" ", ""), parent);
        block.AddComponent<LayoutElement>().preferredHeight = 70;
        var v = block.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        CreateTMP("Label", block.transform, label, 24, FontStyles.Bold, TextColor, 28);

        GameObject sliderGo = CreateUIObject("Slider", block.transform);
        sliderGo.AddComponent<LayoutElement>().preferredHeight = 28;
        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        GameObject bg = CreateUIObject("Background", sliderGo.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.75f, 0.85f, 0.88f, 1f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
        StretchFull(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        fill.AddComponent<Image>().color = new Color(0.4f, 0.75f, 0.7f, 1f);

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24, 24);
        handle.AddComponent<Image>().color = TitleColor;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    static Button CreateHelpBackButton(Transform helpRoot)
    {
        GameObject go = CreateUIObject("BtnHelpBack", helpRoot);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 28f);
        rt.sizeDelta = new Vector2(420f, 52f);
        return CreateMenuButtonOnExisting(go, "VOLVER AL MENÚ");
    }

    static Button CreateMenuButtonOnExisting(GameObject go, string label)
    {
        Image img = go.AddComponent<Image>();
        img.color = BtnNormal;
        Button btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = BtnPressed;
        colors.selectedColor = BtnHighlight;
        btn.colors = colors;

        GameObject textGo = CreateUIObject("Text", go.transform);
        StretchFull(textGo.GetComponent<RectTransform>());
        TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TitleColor;
        return btn;
    }

    static Button CreateMenuButton(Transform parent, string name, string label)
    {
        GameObject go = CreateUIObject(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = 52;
        return CreateMenuButtonOnExisting(go, label);
    }

    static TMP_Text CreateTMP(string name, Transform parent, string text, float fontSize, FontStyles style,
        Color color, float preferredHeight)
    {
        GameObject go = CreateUIObject(name, parent);
        if (preferredHeight > 0)
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
