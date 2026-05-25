using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using CleanCore.VRMenu;

/// <summary>
/// Setup de mano VR IZQUIERDA transparente para controles (sin hand tracking).
///
/// La mano derecha NO se configura: ahi vive la pistola de agua.
///
/// Usa el prefab LeftHandInteractionVisual del sample "Hands Interaction Demo" de XRI.
/// Si no existe en Assets/VRHands/Prefabs/, lo copia automaticamente desde
/// Library/PackageCache (asi este script NO depende del paquete Ghostly Hand).
///
/// Sobre la mano instanciada:
///   - Elimina los scripts que dependen del XRHandSubsystem (ocultarian la malla sin
///     tracking real).
///   - Aplica un material URP/Lit transparente generado en runtime (NO depende del
///     shader Ghostly ni del Opaque Texture del pipeline).
///   - Conecta VRHandGripAnimator a las acciones Select Value / Activate Value para
///     animar los dedos al apretar grip / trigger del mando.
///   - Preserva los interactores del controlador (Near-Far / rayo / puntero).
/// </summary>
public static class HandsSetup
{
    private const string PreferredPrefabFolder = "Assets/VRHands/Prefabs";
    private const string PreferredLeftPrefabPath = "Assets/com.gleechi.unity.virtualgrasp/Runtime/Resources/GleechiHands/GleechiLeftHand.fbx";
    private const string FallbackLeftPrefabPath = "Assets/VRHands/Prefabs/LeftHandInteractionVisual.prefab";

    private const string MaterialFolder = "Assets/Materials";
    private const string MaterialPath = "Assets/Materials/VRHand_Transparent.mat";

    private const string XriInputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.1.3/Starter Assets/XRI Default Input Actions.inputactions";
    private const string LeftSelectValueId = "e6005f29-e4c1-4f3b-8bf7-3a28bab5ca9c";
    private const string LeftActivateValueId = "0c3d0ec9-85a1-45b3-839b-1ca43f859ecd";

    private const string LeftHandName = "VRLeftHandVisual";

    private static readonly string[] LegacyHandNames =
    {
        "VRLeftHandVisual", "VRRightHandVisual",
        "GhostlyLeftHandVisual", "GhostlyRightHandVisual"
    };

    [MenuItem("Tools/VR Hands/Setup Left Hand")]
    public static void SetupHands()
    {
        GameObject leftPrefab = LoadOrImportLeftPrefab();
        if (leftPrefab == null)
        {
            Debug.LogError($"[HandsSetup] No se pudo obtener el prefab LeftHandInteractionVisual.\n" +
                           $"Probado: {PreferredLeftPrefabPath}, {FallbackLeftPrefabPath} y Library/PackageCache.\n" +
                           "Instala el sample 'Hands Interaction Demo' del XR Interaction Toolkit desde Package Manager.");
            return;
        }

        Material handMat = EnsureTransparentHandMaterial();
        if (handMat == null)
        {
            Debug.LogError("[HandsSetup] No se pudo crear el material URP transparente para la mano.");
            return;
        }

        Painter painter = Object.FindAnyObjectByType<Painter>();
        if (painter == null)
        {
            Debug.LogError("[HandsSetup] No se encontro el componente Painter en la escena actual.");
            return;
        }

        var painterSo = new SerializedObject(painter);
        Transform leftController = (Transform)painterSo.FindProperty("leftController").objectReferenceValue;
        Transform rightController = (Transform)painterSo.FindProperty("rightController").objectReferenceValue;

        if (leftController == null)
        {
            leftController = FindTransformIncludingInactive(new[] { "Left Controller", "LeftController", "Left Hand Controller", "Left Hand", "LeftHand" });
        }

        Debug.Log($"[HandsSetup] leftController = {HierarchyPath(leftController)}");
        if (leftController == null)
        {
            Debug.LogError("[HandsSetup] No se encontro el transform del controlador izquierdo en la escena.");
            return;
        }

        // Limpiar manos huerfanas creadas por runs anteriores (incluyendo en la mano derecha)
        RemoveAllHandsAnywhere();

        Object[] inputAssets = AssetDatabase.LoadAllAssetsAtPath(XriInputActionsPath);

        SetupLeftHand(leftController, leftPrefab, handMat, inputAssets);

        // Si por alguna razon en el controlador derecho se habia ocultado todo, no lo tocamos
        // aqui (RemoveAllHandsAnywhere ya borro cualquier visual de mano que hubieramos puesto).
        if (rightController != null)
        {
            Debug.Log($"[HandsSetup] rightController = {HierarchyPath(rightController)} (no se configura mano aqui, ahi va la pistola)");
        }

        EditorUtility.SetDirty(painter);
        EditorUtility.SetDirty(leftController.gameObject);
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);

        Debug.Log("<color=green>[HandsSetup] Mano izquierda lista. Guarda la escena (Ctrl+S).</color>");
    }

    [MenuItem("Tools/VR Hands/Remove Hands")]
    public static void RemoveHands()
    {
        int removed = RemoveAllHandsAnywhere();
        Debug.Log($"[HandsSetup] Manos eliminadas: {removed}");
        EditorSceneManager.MarkAllScenesDirty();
    }

    [MenuItem("Tools/VR Hands/Force Recreate Material")]
    public static void ForceRecreateMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(MaterialPath);
            AssetDatabase.SaveAssets();
        }
        var mat = EnsureTransparentHandMaterial();
        if (mat != null)
        {
            Debug.Log($"[HandsSetup] Material recreado en {MaterialPath}. Vuelve a ejecutar 'Setup Left Hand' para reasignarlo.");
        }
    }

    [MenuItem("Tools/VR Hands/Spawn Debug Cube On Left Controller")]
    public static void SpawnDebugCube()
    {
        Painter painter = Object.FindAnyObjectByType<Painter>();
        if (painter == null || painter.leftController == null)
        {
            Debug.LogError("[HandsSetup] No se encontro Painter o leftController.");
            return;
        }

        // Borrar cubo previo si existe
        var prev = painter.leftController.Find("VRDebugCube");
        if (prev != null) Undo.DestroyObjectImmediate(prev.gameObject);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "VRDebugCube";
        Undo.RegisterCreatedObjectUndo(cube, "Spawn Debug Cube");
        cube.transform.SetParent(painter.leftController, false);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one * 0.08f;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            var mat = new Material(urpLit) { name = "VRDebugCube_Mat" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.yellow);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.yellow);
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        // Quitar collider para no interferir con XRI
        var col = cube.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        Debug.Log($"[HandsSetup] Cubo amarillo debug spawneado en {HierarchyPath(cube.transform)}. Si lo ves en play, la posicion del controlador es correcta y el problema es el rig de la mano. Si no lo ves, el problema es de camara/layer.");
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
    }

    [MenuItem("Tools/VR Hands/Apply Solid Pink To Hand (Debug)")]
    public static void ApplySolidPinkToHand()
    {
        var hand = FindTransformIncludingInactive(new[] { LeftHandName });
        if (hand == null)
        {
            Debug.LogError("[HandsSetup] No se encontro VRLeftHandVisual. Ejecuta 'Setup Left Hand' primero.");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        var mat = new Material(urpLit) { name = "VRHand_DebugPink" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0f, 0.6f, 1f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 0f, 0.6f, 1f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);

        int n = 0;
        foreach (var smr in hand.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Undo.RecordObject(smr, "Apply Debug Pink");
            smr.enabled = true;
            smr.gameObject.SetActive(true);
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.updateWhenOffscreen = true;
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            var mats = new Material[Mathf.Max(1, smr.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            smr.sharedMaterials = mats;
            n++;
        }
        foreach (var mr in hand.GetComponentsInChildren<MeshRenderer>(true))
        {
            Undo.RecordObject(mr, "Apply Debug Pink");
            mr.enabled = true;
            mr.gameObject.SetActive(true);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mats = new Material[Mathf.Max(1, mr.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            mr.sharedMaterials = mats;
            n++;
        }
        Debug.Log($"[HandsSetup] Aplicado material ROSA SOLIDO a {n} renderers. Si ahora se ve rosa, el problema era el material transparente.");
    }

    [MenuItem("Tools/VR Hands/Setup Left Hand (Static Mesh Fallback)")]
    public static void SetupLeftHandStaticFallback()
    {
        Painter painter = Object.FindAnyObjectByType<Painter>();
        if (painter == null || painter.leftController == null)
        {
            Debug.LogError("[HandsSetup] No se encontro Painter o leftController.");
            return;
        }

        RemoveAllHandsAnywhere();

        // 1. Crear contenedor vacio
        GameObject handRoot = new GameObject(LeftHandName);
        Undo.RegisterCreatedObjectUndo(handRoot, "Create Static Hand");
        handRoot.transform.SetParent(painter.leftController, false);
        handRoot.transform.localPosition = Vector3.zero;
        handRoot.transform.localRotation = Quaternion.identity;

        Material handMat = EnsureTransparentHandMaterial();

        // 2. Crear geometria basica (Palma + Dedos)
        // Palma
        CreatePart(handRoot, "Palm", new Vector3(-0.01f, -0.01f, -0.04f), new Vector3(0.08f, 0.02f, 0.09f), handMat);
        
        // Pulgar
        CreatePart(handRoot, "Thumb", new Vector3(0.04f, -0.01f, -0.02f), new Vector3(0.02f, 0.02f, 0.06f), handMat, Quaternion.Euler(0, 30, 0));
        
        // Indice
        CreatePart(handRoot, "IndexFinger", new Vector3(0.03f, -0.01f, 0.03f), new Vector3(0.018f, 0.018f, 0.07f), handMat);
        
        // Medio
        CreatePart(handRoot, "MiddleFinger", new Vector3(0.005f, -0.01f, 0.035f), new Vector3(0.018f, 0.018f, 0.08f), handMat);
        
        // Anular
        CreatePart(handRoot, "RingFinger", new Vector3(-0.02f, -0.01f, 0.03f), new Vector3(0.018f, 0.018f, 0.07f), handMat);
        
        // Menique
        CreatePart(handRoot, "PinkyFinger", new Vector3(-0.045f, -0.01f, 0.02f), new Vector3(0.015f, 0.015f, 0.05f), handMat);

        Debug.Log("<color=green>[HandsSetup] Mano estática basica creada. Es visible y no depende de rigs rotos.</color>");
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
    }

    private static void CreatePart(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = pos;
        part.transform.localRotation = rot ?? Quaternion.identity;
        part.transform.localScale = scale;
        
        Object.DestroyImmediate(part.GetComponent<Collider>());
        if (mat != null) part.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    [MenuItem("Tools/VR Hands/Remove Debug Cube")]
    public static void RemoveDebugCube()
    {
        int removed = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (EditorUtility.IsPersistent(t.root.gameObject)) continue;
            if (t.name == "VRDebugCube")
            {
                Undo.DestroyObjectImmediate(t.gameObject);
                removed++;
            }
        }
        Debug.Log($"[HandsSetup] Cubos debug eliminados: {removed}");
    }

    [MenuItem("Tools/VR Hands/Debug Controllers")]
    public static void DebugControllers()
    {
        Painter painter = Object.FindAnyObjectByType<Painter>();
        var sb = new StringBuilder();
        sb.AppendLine("=== DEBUG CONTROLLERS ===");
        if (painter != null)
        {
            sb.AppendLine($"Painter.leftController  = {HierarchyPath(painter.leftController)}");
            sb.AppendLine($"Painter.rightController = {HierarchyPath(painter.rightController)}");
            DumpChildren(painter.leftController, "Left", sb);
            DumpChildren(painter.rightController, "Right", sb);
        }
        else
        {
            sb.AppendLine("No se encontro Painter en la escena.");
        }
        Debug.Log(sb.ToString());
    }

    // ---------- Setup principal ----------

    private static void SetupLeftHand(Transform controller, GameObject handPrefab, Material handMat, Object[] inputAssets)
    {
        Debug.Log($"[HandsSetup] === Procesando {LeftHandName} en {HierarchyPath(controller)} ===");

        // 0. Asegurar que el controlador y todos sus ancestros esten activos.
        for (Transform p = controller; p != null; p = p.parent)
        {
            if (!p.gameObject.activeSelf)
            {
                Undo.RecordObject(p.gameObject, "Activate Controller Ancestor");
                p.gameObject.SetActive(true);
                Debug.Log($"[HandsSetup] Activado nodo ancestro: {p.name}");
            }
        }

        // 1. Procesar hijos del controlador izquierdo: ocultar modelo del mando,
        //    asegurar interactores activos.
        for (int i = 0; i < controller.childCount; i++)
        {
            Transform child = controller.GetChild(i);
            string n = child.name;
            if (n == "(Prb)WaterGun" || n == "WateringCan_FX") continue;

            bool isInteractor = n.Contains("Interactor") || n.Contains("Near-Far") || n.Contains("Ray") || n.Contains("Teleport");
            if (isInteractor)
            {
                Undo.RecordObject(child.gameObject, "Ensure Interactor Visible");
                child.gameObject.SetActive(true);
                EnableLineVisuals(child);
                continue;
            }

            bool isControllerModel = n.Contains("Visual") || n.Contains("Controller Model") || n.Contains("Universal");
            if (isControllerModel)
            {
                Undo.RecordObject(child.gameObject, "Hide Controller Model");
                child.gameObject.SetActive(false);
            }
        }

        // 2. Instanciar la mano como GameObject "plano" (sin link al prefab) para evitar
        //    problemas con instancias anidadas / overrides en el XR Origin unpacked.
        GameObject handInstance = Object.Instantiate(handPrefab, controller);
        if (handInstance == null)
        {
            Debug.LogError($"[HandsSetup] No se pudo instanciar {handPrefab.name} bajo {controller.name}");
            return;
        }
        Undo.RegisterCreatedObjectUndo(handInstance, "Instantiate VR Hand");

        handInstance.name = LeftHandName;
        
        // Mano alineada con el origen del Near-Far Interactor (de donde sale el rayo).
        // Bajamos y adelantamos un poco la mano para que los dedos envuelvan el origen del rayo.
        handInstance.transform.localPosition = new Vector3(0.02f, -0.05f, 0.04f); 
        handInstance.transform.localRotation = Quaternion.Euler(-90f, -90f, 90f); 
        
        handInstance.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

        handInstance.SetActive(true);
        handInstance.hideFlags = HideFlags.None;
        handInstance.layer = 0;

        // 3. Quitar scripts que dependen del XRHandSubsystem y COLLIDERS
        //    (los colliders en la mano pueden bloquear el rayo del apuntador)
        foreach (var b in handInstance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b == null) continue;
            string tn = b.GetType().Name;
            if (tn.Contains("XRHand") || tn.Contains("HandTracking") || tn.Contains("HandVisual") || tn.Contains("HandSkeleton") || tn.Contains("VG_"))
            {
                Undo.DestroyObjectImmediate(b);
            }
        }
        foreach (var col in handInstance.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(col);
        }

        // 3.5 Limpiar referencias rotas de scripts (genera warnings "missing script")
        foreach (var go in handInstance.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
        }

        // 4. Forzar TODO el subarbol activo, layer 0, escala valida
        foreach (var tr in handInstance.GetComponentsInChildren<Transform>(true))
        {
            if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
            tr.gameObject.hideFlags = HideFlags.None;
            tr.gameObject.layer = 0;
            if (tr.localScale == Vector3.zero) tr.localScale = Vector3.one;
        }

        // 5. Aplicar material transparente a todos los renderers + forzar bounds enormes
        //    para que nunca se haga frustum culling con un bounds local degenerado.
        int smrCount = 0;
        int mrCount = 0;
        foreach (var smr in handInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Undo.RecordObject(smr, "Setup Hand Renderer");
            smr.enabled = true;
            smr.gameObject.SetActive(true);
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.updateWhenOffscreen = true;
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            var mats = new Material[Mathf.Max(1, smr.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = handMat;
            smr.sharedMaterials = mats;
            smrCount++;
        }
        foreach (var mr in handInstance.GetComponentsInChildren<MeshRenderer>(true))
        {
            Undo.RecordObject(mr, "Setup Hand Renderer");
            mr.enabled = true;
            mr.gameObject.SetActive(true);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mats = new Material[Mathf.Max(1, mr.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = handMat;
            mr.sharedMaterials = mats;
            mrCount++;
        }

        Debug.Log($"[HandsSetup] {LeftHandName} renderers: {smrCount} SkinnedMeshRenderer + {mrCount} MeshRenderer. " +
                  $"Active: {handInstance.activeInHierarchy}, WorldPos: {handInstance.transform.position}, LossyScale: {handInstance.transform.lossyScale}");

        if (smrCount == 0 && mrCount == 0)
        {
            Debug.LogError($"[HandsSetup] !!! {LeftHandName} no tiene NINGUN renderer. El prefab esta corrupto. Reinstala el sample 'Hands Interaction Demo' del XR Interaction Toolkit.");
        }

        // 6. Animador de dedos por grip / trigger
        var animator = handInstance.GetComponent<VRHandGripAnimator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<VRHandGripAnimator>(handInstance);
        }

        InputActionReference gripRef = FindInputActionReference(inputAssets, LeftSelectValueId);
        InputActionReference triggerRef = FindInputActionReference(inputAssets, LeftActivateValueId);

        var animSo = new SerializedObject(animator);
        animSo.FindProperty("gripValueAction").objectReferenceValue = gripRef;
        animSo.FindProperty("triggerValueAction").objectReferenceValue = triggerRef;
        animSo.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[HandsSetup] {LeftHandName} listo. Grip: {(gripRef != null ? gripRef.name : "<null>")} | Trigger: {(triggerRef != null ? triggerRef.name : "<null>")}");
    }

    private static int RemoveAllHandsAnywhere()
    {
        int removed = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (EditorUtility.IsPersistent(t.root.gameObject)) continue;
            foreach (var name in LegacyHandNames)
            {
                if (t.name == name)
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    removed++;
                    break;
                }
            }
        }
        return removed;
    }

    // ---------- Prefab provisioning (independiente de Ghostly Hand) ----------

    private static GameObject LoadOrImportLeftPrefab()
    {
        // 1. Buscar el FBX de Gleechi (VirtualGrasp)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreferredLeftPrefabPath);
        if (prefab != null) return prefab;

        // 2. Buscar en la carpeta antigua de XRI
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallbackLeftPrefabPath);
        if (prefab != null) return prefab;

        // 3. Copiar desde Library/PackageCache si esta el paquete XRI
        Debug.Log("[HandsSetup] Prefab no encontrado en Assets. Intentando copiar desde Library/PackageCache...");
        return TryCopyPrefabFromPackageCache();
    }

    private static GameObject TryCopyPrefabFromPackageCache()
    {
        const string packageCache = "Library/PackageCache";
        if (!Directory.Exists(packageCache))
        {
            Debug.LogError("[HandsSetup] No existe Library/PackageCache.");
            return null;
        }

        string[] dirs = Directory.GetDirectories(packageCache, "com.unity.xr.interaction.toolkit*");
        if (dirs.Length == 0)
        {
            Debug.LogError("[HandsSetup] No se encontro el paquete com.unity.xr.interaction.toolkit en PackageCache.");
            return null;
        }

        foreach (var dir in dirs)
        {
            string src = Path.Combine(dir, "Samples~/Hands Interaction Demo/Prefabs/LeftHandInteractionVisual.prefab");
            if (!File.Exists(src)) continue;

            if (!Directory.Exists(PreferredPrefabFolder))
            {
                Directory.CreateDirectory(PreferredPrefabFolder);
            }

            string dst = PreferredLeftPrefabPath;
            File.Copy(src, dst, true);

            // Copiar tambien el .meta si existe para preservar GUID
            if (File.Exists(src + ".meta"))
            {
                File.Copy(src + ".meta", dst + ".meta", true);
            }

            AssetDatabase.Refresh();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dst);
            if (prefab != null)
            {
                Debug.Log($"[HandsSetup] Copiado {Path.GetFileName(src)} -> {dst}");
                return prefab;
            }
        }

        Debug.LogError("[HandsSetup] No se encontro el archivo LeftHandInteractionVisual.prefab en Samples~ de XRI. Importa el sample 'Hands Interaction Demo' desde Package Manager.");
        return null;
    }

    // ---------- Material ----------

    private static Material EnsureTransparentHandMaterial()
    {
        if (!Directory.Exists(MaterialFolder))
        {
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
        }

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null) return existing;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[HandsSetup] No se encontro el shader 'Universal Render Pipeline/Lit'. Asegurate de tener URP instalado.");
            return null;
        }

        var mat = new Material(urpLit) { name = "VRHand_Transparent" };
        // Alpha alto (0.85) y color azul saturado para que sea claramente visible.
        ConfigureUrpLitTransparent(mat, new Color(0.30f, 0.75f, 1.0f, 0.85f));
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void ConfigureUrpLitTransparent(Material mat, Color baseColor)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        mat.SetOverrideTag("RenderType", "Transparent");
    }

    // ---------- Helpers ----------

    private static void EnableLineVisuals(Transform root)
    {
        foreach (var b in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b == null) continue;
            string tn = b.GetType().Name;
            if (tn.Contains("LineVisual") || tn.Contains("CurveVisualController") || tn.Contains("InteractorLineVisual"))
            {
                Undo.RecordObject(b, "Enable Ray Visual");
                b.enabled = true;
            }
        }
        foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
        {
            Undo.RecordObject(lr, "Enable Line Renderer");
            lr.enabled = true;
            if (!lr.gameObject.activeSelf) lr.gameObject.SetActive(true);
        }
    }

    private static InputActionReference FindInputActionReference(Object[] assets, string id)
    {
        if (assets == null) return null;
        foreach (var a in assets)
        {
            if (a is InputActionReference iar && iar.action != null && iar.action.id.ToString() == id)
            {
                return iar;
            }
        }
        return null;
    }

    private static Transform FindTransformIncludingInactive(string[] names)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (EditorUtility.IsPersistent(t.root.gameObject)) continue;
            if ((t.gameObject.hideFlags & HideFlags.NotEditable) != 0) continue;
            if ((t.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
            foreach (var n in names)
            {
                if (t.name == n) return t;
            }
        }
        return null;
    }

    private static string HierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null; p = p.parent)
        {
            sb.Insert(0, p.name + "/");
        }
        return sb.ToString();
    }

    private static void DumpChildren(Transform t, string label, StringBuilder sb)
    {
        if (t == null)
        {
            sb.AppendLine($"{label}: <null>");
            return;
        }
        sb.AppendLine($"{label} children:");
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            sb.AppendLine($"  - {c.name} (active: {c.gameObject.activeSelf})");
        }
    }
}
