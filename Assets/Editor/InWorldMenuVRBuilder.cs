using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Builds InWorldMenuVR prefab, MainMixer asset, and scene integration.
/// </summary>
public static class InWorldMenuVRBuilder
{
    const string PrefabPath = "Assets/Prefabs/UI/InWorldMenuVR.prefab";
    const string MixerPath = "Assets/Audio/MainMixer.mixer";

    static readonly Color PanelBg = new Color(0.95f, 0.98f, 1f, 0.92f);
    static readonly Color BtnNormal = new Color(0.82f, 0.93f, 0.95f, 1f);
    static readonly Color BtnHighlight = new Color(0.55f, 0.82f, 0.88f, 1f);
    static readonly Color BtnPressed = new Color(0.4f, 0.72f, 0.8f, 1f);
    static readonly Color TitleColor = new Color(0.15f, 0.35f, 0.45f, 1f);
    static readonly Color TextColor = new Color(0.2f, 0.3f, 0.35f, 1f);

    [MenuItem("Tools/VR Menu/Create Audio Mixer Asset")]
    public static void CreateAudioMixerAsset()
    {
        EnsureMainMixer();
        Debug.Log($"[VRMenu] AudioMixer listo en {MixerPath}");
    }

    [MenuItem("Tools/VR Menu/Create InWorld Menu Prefab")]
    public static void CreateInWorldMenuPrefab()
    {
        EnsureFolders();
        AudioMixer mixer = EnsureMainMixer();

        GameObject root = new GameObject("InWorldMenuVR");
        try
        {
            var placement = root.AddComponent<InWorldMenuPlacement>();
            var audioMgr = root.AddComponent<AudioManager>();
            var stats = root.AddComponent<CleaningStatsAggregator>();
            var menu = root.AddComponent<InWorldMenuVR>();
            var binder = root.AddComponent<VRMenuUIBinder>();
            root.AddComponent<VRMenuToggleInput>();

            SetPrivateField(audioMgr, "_mixer", mixer);

            GameObject menuCanvas = CreateCanvasHierarchy(root.transform, menu, out HelpPanelController help,
                out Slider sliderAmb, out Slider sliderMus, out TMP_Text statsLabel, out GameObject modalRoot,
                out TMP_Text modalMsg, out Button modalYes, out Button modalNo,
                out Button btnRecenter, out Button btnReset, out Button btnHelp, out Button btnClose, out Button btnExit);

            SetPrivateField(stats, "_surfaceUI", (PaintableSurfaceUI)null);
            SetPrivateField(menu, "_sliderAmbiente", sliderAmb);
            SetPrivateField(menu, "_sliderMusica", sliderMus);
            SetPrivateField(menu, "_statsLabel", statsLabel);
            SetPrivateField(menu, "_modalRoot", modalRoot);
            SetPrivateField(menu, "_modalMessage", modalMsg);
            SetPrivateField(menu, "_modalYesButton", modalYes);
            SetPrivateField(menu, "_modalNoButton", modalNo);
            SetPrivateField(menu, "_audioManager", audioMgr);
            SetPrivateField(menu, "_statsAggregator", stats);
            SetPrivateField(menu, "_helpPanel", help);
            SetPrivateField(binder, "_menu", menu);
            SetPrivateField(binder, "_btnRecenter", btnRecenter);
            SetPrivateField(binder, "_btnReset", btnReset);
            SetPrivateField(binder, "_btnHelp", btnHelp);
            SetPrivateField(binder, "_btnExit", btnExit);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[VRMenu] Prefab guardado: {PrefabPath}", prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/VR Menu/Add To House Interior Scene")]
    public static void AddToHouseInteriorScene()
    {
        const string scenePath = "Assets/StylArts/StylizedHouseInterior/Scene/URP_Stylized_House_Interior.unity";
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        AddToActiveScene();
    }

    [MenuItem("Tools/VR Menu/Add To Active Scene")]
    public static void AddToActiveScene()
    {
        CreateInWorldMenuPrefab();
        EnsureEventSystem();

        GameObject existing = GameObject.Find("InWorldMenuVR");
        if (existing != null)
        {
            Debug.LogWarning("[VRMenu] Ya existe InWorldMenuVR en escena; omitiendo instancia nueva.");
            WireSceneReferences(existing);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[VRMenu] Prefab no encontrado. Ejecuta Create InWorld Menu Prefab primero.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "InWorldMenuVR";
        WireSceneReferences(instance);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[VRMenu] Menu anadido a escena activa y referencias conectadas.");
    }

    [MenuItem("Tools/VR Menu/Setup EventSystem Only")]
    public static void SetupEventSystemOnly()
    {
        EnsureEventSystem();
        Debug.Log("[VRMenu] EventSystem configurado con XRUIInputModule.");
    }

    static void WireSceneReferences(GameObject menuRoot)
    {
        var menu = menuRoot.GetComponent<InWorldMenuVR>();
        var placement = menuRoot.GetComponent<InWorldMenuPlacement>();
        var stats = menuRoot.GetComponent<CleaningStatsAggregator>();
        var audioMgr = menuRoot.GetComponent<AudioManager>();

        XROrigin origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
        PaintableSurfaceUI surfaceUI = UnityEngine.Object.FindFirstObjectByType<PaintableSurfaceUI>();

        if (origin != null)
        {
            SetPrivateField(placement, "_xrOrigin", origin);
            SetPrivateField(menu, "_xrOrigin", origin);
        }

        if (surfaceUI != null)
            SetPrivateField(stats, "_surfaceUI", surfaceUI);

        if (audioMgr != null)
            SetPrivateField(menu, "_audioManager", audioMgr);
        if (stats != null)
            SetPrivateField(menu, "_statsAggregator", stats);

        HapticImpulsePlayer[] haptics = UnityEngine.Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsSortMode.None);
        HapticImpulsePlayer left = null, right = null;
        foreach (var h in haptics)
        {
            string n = h.gameObject.name.ToLowerInvariant();
            if (n.Contains("left") && left == null) left = h;
            if (n.Contains("right") && right == null) right = h;
        }
        if (left == null && haptics.Length > 0) left = haptics[0];
        if (right == null && haptics.Length > 1) right = haptics[1];

        foreach (var feedback in menuRoot.GetComponentsInChildren<VRMenuButtonFeedback>(true))
        {
            SetPrivateField(feedback, "_leftHaptic", left);
            SetPrivateField(feedback, "_rightHaptic", right);
        }

        var help = menuRoot.GetComponentInChildren<HelpPanelController>(true);
        if (help != null)
            SetPrivateField(menu, "_helpPanel", help);

        var binder = menuRoot.GetComponent<VRMenuUIBinder>();
        if (binder != null)
        {
            SetPrivateField(binder, "_menu", menu);
            foreach (Button b in menuRoot.GetComponentsInChildren<Button>(true))
            {
                switch (b.gameObject.name)
                {
                    case "BtnRecenter": SetPrivateField(binder, "_btnRecenter", b); break;
                    case "BtnReset": SetPrivateField(binder, "_btnReset", b); break;
                    case "BtnHelp": SetPrivateField(binder, "_btnHelp", b); break;
                    case "BtnClose": SetPrivateField(binder, "_btnClose", b); break;
                    case "BtnExit": SetPrivateField(binder, "_btnExit", b); break;
                }
            }
        }

        VRMenuSceneServices.FinalizeMenu(menuRoot);

        EditorUtility.SetDirty(menuRoot);
    }

    static void EnsureEventSystem()
    {
        EventSystem es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            go.AddComponent<XRUIInputModule>();
            Debug.Log("[VRMenu] EventSystem creado con XRUIInputModule.");
        }
        else
        {
            if (es.GetComponent<XRUIInputModule>() == null)
                es.gameObject.AddComponent<XRUIInputModule>();

            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null)
                standalone.enabled = false;

            var inputSystemModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemModule != null)
                inputSystemModule.enabled = false;
        }
    }

    static GameObject CreateCanvasHierarchy(Transform root, InWorldMenuVR menu, out HelpPanelController help,
        out Slider sliderAmb, out Slider sliderMus, out TMP_Text statsLabel, out GameObject modalRoot,
        out TMP_Text modalMsg, out Button modalYes, out Button modalNo,
        out Button btnRecenter, out Button btnReset, out Button btnHelp, out Button btnClose, out Button btnExit)
    {
        GameObject canvasGo = new GameObject("MenuCanvas");
        canvasGo.transform.SetParent(root, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 650);

        canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
        canvasGo.AddComponent<VRMenuWorldCanvasDriver>();

        // Panel
        GameObject panel = CreateUIObject("Panel", canvasGo.transform);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PanelBg;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        StretchFull(panelRect);
        panelRect.offsetMin = new Vector2(20, 20);
        panelRect.offsetMax = new Vector2(-20, -20);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateTMP("Title", panel.transform, "CONFIGURACIÓN", 42, FontStyles.Bold, TitleColor, 56);

        // Stats row
        GameObject statsRow = CreateUIObject("StatsRow", panel.transform);
        statsRow.AddComponent<LayoutElement>().preferredHeight = 40;
        HorizontalLayoutGroup hlg = statsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        CreateTMP("StatsLabel", statsRow.transform, "STATS", 28, FontStyles.Bold, TextColor, 36);
        statsLabel = CreateTMP("StatsValue", statsRow.transform, "Limpieza: 0.0%", 28, FontStyles.Normal, TitleColor, 36);

        sliderAmb = CreateSliderBlock(panel.transform, "VOLUMEN AMBIENTE", 0.7f, out _);
        sliderMus = CreateSliderBlock(panel.transform, "VOLUMEN MÚSICA", 0.4f, out _);

        btnRecenter = CreateMenuButton(panel.transform, "BtnRecenter", "RECENTRAR VISTA");
        btnReset = CreateMenuButton(panel.transform, "BtnReset", "REINICIAR ESCENA");
        btnHelp = CreateMenuButton(panel.transform, "BtnHelp", "AYUDA (CONTROLES BÁSICOS)");
        btnClose = CreateMenuButton(panel.transform, "BtnClose", "CERRAR");
        btnExit = CreateMenuButton(panel.transform, "BtnExit", "SALIR");

        // Help panel (sibling under canvas)
        GameObject helpRoot = CreateUIObject("HelpPanel", canvasGo.transform);
        StretchFull(helpRoot.GetComponent<RectTransform>());
        Image helpBg = helpRoot.AddComponent<Image>();
        helpBg.color = new Color(0.1f, 0.2f, 0.25f, 0.85f);
        helpRoot.SetActive(false);
        help = helpRoot.AddComponent<HelpPanelController>();
        GameObject helpTextGo = CreateUIObject("HelpText", helpRoot.transform);
        StretchFull(helpTextGo.GetComponent<RectTransform>());
        helpTextGo.GetComponent<RectTransform>().offsetMin = new Vector2(40, 100);
        helpTextGo.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -40);
        TMP_Text helpText = helpTextGo.AddComponent<TextMeshProUGUI>();
        helpText.fontSize = 26;
        helpText.color = Color.white;
        helpText.alignment = TextAlignmentOptions.TopLeft;

        Button btnHelpBack = CreateHelpBackButton(helpRoot.transform);
        SetPrivateField(help, "_panelRoot", helpRoot);
        SetPrivateField(help, "_bodyText", helpText);
        SetPrivateField(help, "_backButton", btnHelpBack);

        // Modal
        modalRoot = CreateUIObject("ModalOverlay", canvasGo.transform);
        StretchFull(modalRoot.GetComponent<RectTransform>());
        Image modalBg = modalRoot.AddComponent<Image>();
        modalBg.color = new Color(0, 0, 0, 0.55f);
        modalRoot.SetActive(false);

        GameObject modalPanel = CreateUIObject("ModalPanel", modalRoot.transform);
        RectTransform mpRect = modalPanel.GetComponent<RectTransform>();
        mpRect.anchorMin = mpRect.anchorMax = new Vector2(0.5f, 0.5f);
        mpRect.sizeDelta = new Vector2(700, 320);
        modalPanel.AddComponent<Image>().color = PanelBg;

        VerticalLayoutGroup modalVlg = modalPanel.AddComponent<VerticalLayoutGroup>();
        modalVlg.padding = new RectOffset(24, 24, 24, 24);
        modalVlg.spacing = 20;
        modalVlg.childAlignment = TextAnchor.MiddleCenter;

        modalMsg = CreateTMP("ModalMessage", modalPanel.transform, "¿Confirmar?", 30, FontStyles.Normal, TextColor, 120);

        GameObject modalBtns = CreateUIObject("ModalButtons", modalPanel.transform);
        modalBtns.AddComponent<LayoutElement>().preferredHeight = 56;
        HorizontalLayoutGroup modalH = modalBtns.AddComponent<HorizontalLayoutGroup>();
        modalH.spacing = 24;
        modalH.childAlignment = TextAnchor.MiddleCenter;
        modalYes = CreateMenuButton(modalBtns.transform, "BtnYes", "Sí");
        modalNo = CreateMenuButton(modalBtns.transform, "BtnNo", "No");

        WireButtonFeedback(canvasGo.transform);
        return canvasGo;
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
                SetPrivateField(fb, "_targetGraphic", btn.GetComponent<Image>());
                SetPrivateField(fb, "_audioSource", src);
            }
        }
    }

    static Slider CreateSliderBlock(Transform parent, string label, float defaultValue, out TMP_Text labelTmp)
    {
        GameObject block = CreateUIObject(label.Replace(" ", ""), parent);
        block.AddComponent<LayoutElement>().preferredHeight = 70;
        VerticalLayoutGroup v = block.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        labelTmp = CreateTMP("Label", block.transform, label, 24, FontStyles.Bold, TextColor, 28);

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
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.75f, 0.7f, 1f);

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

        Image img = go.AddComponent<Image>();
        img.color = BtnNormal;
        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = BtnPressed;
        colors.selectedColor = BtnHighlight;
        btn.colors = colors;

        GameObject textGo = CreateUIObject("Text", go.transform);
        StretchFull(textGo.GetComponent<RectTransform>());
        TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "VOLVER AL MENÚ";
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TitleColor;
        return btn;
    }

    static Button CreateMenuButton(Transform parent, string name, string label)
    {
        GameObject go = CreateUIObject(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = 52;
        Image img = go.AddComponent<Image>();
        img.color = BtnNormal;
        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = BtnPressed;
        colors.selectedColor = BtnHighlight;
        btn.colors = colors;

        GameObject textGo = CreateUIObject("Text", go.transform);
        StretchFull(textGo.GetComponent<RectTransform>());
        TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TitleColor;

        return btn;
    }

    static TMP_Text CreateTMP(string name, Transform parent, string text, float fontSize, FontStyles style,
        Color color, float preferredHeight)
    {
        GameObject go = CreateUIObject(name, parent);
        if (preferredHeight > 0)
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
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

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");
    }

    static AudioMixer EnsureMainMixer()
    {
        EnsureFolders();
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer != null)
            return mixer;

        Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor.CoreModule")
                           ?? Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
        if (controllerType == null)
        {
            Debug.LogError("[VRMenu] No se pudo crear AudioMixerController. Crea MainMixer manualmente (ver VR_MENU_SETUP.md).");
            return null;
        }

        ScriptableObject controller = ScriptableObject.CreateInstance(controllerType);
        AssetDatabase.CreateAsset(controller, MixerPath);
        AssetDatabase.SaveAssets();
        mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        TryAddMixerGroups(mixer);
        AssetDatabase.SaveAssets();
        return mixer;
    }

    static void TryAddMixerGroups(AudioMixer mixer)
    {
        if (mixer == null) return;

        try
        {
            AudioMixerGroup[] master = mixer.FindMatchingGroups("Master");
            if (master == null || master.Length == 0)
                return;

            AudioMixerGroup masterGroup = master[0];
            CreateChildGroupIfMissing(mixer, masterGroup, "Ambiente", AudioManager.ParamVolAmbiente);
            CreateChildGroupIfMissing(mixer, masterGroup, "Musica", AudioManager.ParamVolMusica);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[VRMenu] Grupos mixer: configura manualmente Ambiente/Musica. {ex.Message}");
        }
    }

    static void CreateChildGroupIfMissing(AudioMixer mixer, AudioMixerGroup parent, string groupName, string exposedParam)
    {
        AudioMixerGroup[] existing = mixer.FindMatchingGroups(groupName);
        if (existing != null && existing.Length > 0)
            return;

        Type type = mixer.GetType();
        MethodInfo createGroup = type.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (createGroup == null)
            return;

        object result = createGroup.Invoke(mixer, new object[] { groupName, parent, false });
        if (result is AudioMixerGroup group)
            ExposeVolumeParameter(mixer, group, exposedParam);
    }

    static void ExposeVolumeParameter(AudioMixer mixer, AudioMixerGroup group, string paramName)
    {
        Type type = mixer.GetType();
        MethodInfo expose = type.GetMethod("AddExposedParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (expose == null)
            return;

        // Expose attenuation on the group's Attenuation effect
        expose.Invoke(mixer, new object[] { $"Attenuation.{group.name}.Volume", paramName });
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
