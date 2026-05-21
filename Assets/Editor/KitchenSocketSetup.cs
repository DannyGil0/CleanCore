using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CleanCore.Kitchen;

public static class KitchenSocketSetup
{
    const string GhostMatPath = "Assets/Materials/KitchenGhost.mat";

    static readonly Vector3 DefaultDisplacement = new Vector3(0.5f, 0.3f, 0.3f);

    struct KitchenPlaceable
    {
        public string objectName;
        public Vector3 localPos;
        public Quaternion localRot;
    }

    static readonly KitchenPlaceable[] Placeables = new[]
    {
        new KitchenPlaceable { objectName = "SM_SaucePan_Black_Pan_01_90",
            localPos = new Vector3(2.22f, -0.09f, -1.38f),
            localRot = new Quaternion(0.0000009f, -0.98262787f, 0f, 0.18558699f) },
        new KitchenPlaceable { objectName = "Kitchen_Props_B_Glass_Cup_A_106",
            localPos = new Vector3(3.7359424f, -0.10843396f, -5.467648f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "Kitchen_Props_B_Glass_Cup_A2_109",
            localPos = new Vector3(3.6068883f, -0.10843396f, -6.18413f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "Kitchen_Props_A_Blender_41",
            localPos = new Vector3(0.70120525f, -0.09655893f, -1.3835092f),
            localRot = new Quaternion(0f, 0.37020296f, 0f, 0.9289509f) },
        new KitchenPlaceable { objectName = "SM_Microwave_104",
            localPos = new Vector3(0.41122723f, -0.088722944f, -4.9434958f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "Kitchen_Props_A_Pot_B_15",
            localPos = new Vector3(0.87145615f, -0.09693694f, -2.461511f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "Kitchen_Props_A_Pot_A_18",
            localPos = new Vector3(1.568613f, -0.08332801f, -1.3698602f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "SM_BreakfastProps_Bread5",
            localPos = new Vector3(3.7959623f, 0.19174898f, -1.3435578f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
        new KitchenPlaceable { objectName = "SM_BreakfastProps_Bread_36",
            localPos = new Vector3(3.038f, -0.052681923f, -1.249001f),
            localRot = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f) },
    };

    [MenuItem("Tools/Kitchen Socket/Setup All Sockets")]
    static void RunSetup()
    {
        Undo.SetCurrentGroupName("Kitchen Socket Setup All");

        var ghostMat = GetOrCreateGhostMaterial();

        // Find or create sockets root
        var socketsRoot = GameObject.Find("KitchenSockets");
        if (socketsRoot == null)
        {
            socketsRoot = new GameObject("KitchenSockets");
            Undo.RegisterCreatedObjectUndo(socketsRoot, "Create KitchenSockets");
        }

        // Unpack XR Origin first
        UnpackXROrigin();

        int configured = 0;
        foreach (var placeable in Placeables)
        {
            var obj = FindInScene(placeable.objectName);
            if (obj == null)
            {
                Debug.LogWarning($"[KitchenSocket] '{placeable.objectName}' not found — skipping.");
                continue;
            }

            // Use hardcoded position as socket world pos (these are local to shared parent)
            // The parent's world position makes these effectively world positions in this scene
            Transform sharedParent = obj.transform.parent;
            Vector3 socketWorldPos;
            Quaternion socketWorldRot;
            if (sharedParent != null)
            {
                socketWorldPos = sharedParent.TransformPoint(placeable.localPos);
                socketWorldRot = sharedParent.rotation * placeable.localRot;
            }
            else
            {
                socketWorldPos = placeable.localPos;
                socketWorldRot = placeable.localRot;
            }

            SetupGrabbable(obj, socketWorldPos);
            SetupSocket(socketsRoot, obj, ghostMat, socketWorldPos, socketWorldRot, placeable.objectName);
            configured++;
        }

        FixNearFarInteractorRaycastMask();
        FixHandInteractionLayers();
        CreatePlacementBoard(socketsRoot);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[KitchenSocket] Setup complete: {configured}/{Placeables.Length} objects configured.");
    }

    static void UnpackXROrigin()
    {
        var xrOrigins = Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None);
        foreach (var origin in xrOrigins)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(origin.gameObject))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(origin.gameObject);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log("[KitchenSocket] Unpacked XR Origin");
            }
        }
    }

    static void FixNearFarInteractorRaycastMask()
    {
        var casters = Object.FindObjectsByType<CurveInteractionCaster>(FindObjectsSortMode.None);
        foreach (var caster in casters)
        {
            int current = (int)(LayerMask)caster.raycastMask;
            int needed = current | (1 << 0) | (1 << 6);
            if (current != needed)
            {
                caster.raycastMask = needed;
                Debug.Log($"[KitchenSocket] Raycast mask: {current} -> {needed} on {caster.gameObject.name}");
            }
        }

        var sphereCasters = Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsSortMode.None);
        foreach (var caster in sphereCasters)
        {
            int current = (int)(LayerMask)caster.physicsLayerMask;
            int needed = current | (1 << 0) | (1 << 6);
            if (current != needed)
            {
                caster.physicsLayerMask = needed;
                Debug.Log($"[KitchenSocket] Sphere mask: {current} -> {needed} on {caster.gameObject.name}");
            }
        }
    }

    static void FixHandInteractionLayers()
    {
        int cocinaBit = InteractionLayerMask.GetMask("Cocina");
        var interactors = Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (var interactor in interactors)
        {
            bool isRight = false;
            bool isLeft = false;
            Transform t = interactor.transform;
            while (t != null)
            {
                if (t.name.Contains("Right")) { isRight = true; break; }
                if (t.name.Contains("Left")) { isLeft = true; break; }
                t = t.parent;
            }

            if (isRight)
            {
                int current = (int)(InteractionLayerMask)interactor.interactionLayers;
                interactor.interactionLayers = current & ~cocinaBit;
                Debug.Log("[KitchenSocket] Right hand: Cocina removed");
            }
            else if (isLeft)
            {
                int current = (int)(InteractionLayerMask)interactor.interactionLayers;
                interactor.interactionLayers = current | cocinaBit;
                interactor.enableFarCasting = true;
                interactor.farAttachMode = InteractorFarAttachMode.Near;
                Debug.Log("[KitchenSocket] Left hand: Cocina + farAttachMode=Near");
            }
        }
    }

    static void SetupGrabbable(GameObject obj, Vector3 socketWorldPos)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (root != null)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }

        SetLayerRecursive(obj, 0);
        GameObjectUtility.SetStaticEditorFlags(obj, 0);
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);

        var meshCols = obj.GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshCols)
            mc.convex = true;

        if (obj.GetComponent<Collider>() == null)
        {
            var box = obj.AddComponent<BoxCollider>();
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                box.center = obj.transform.InverseTransformPoint(renderer.bounds.center);
                box.size = Vector3.Max(
                    new Vector3(
                        Mathf.Abs(obj.transform.InverseTransformVector(renderer.bounds.size).x),
                        Mathf.Abs(obj.transform.InverseTransformVector(renderer.bounds.size).y),
                        Mathf.Abs(obj.transform.InverseTransformVector(renderer.bounds.size).z)),
                    Vector3.one * 0.1f);
            }
        }

        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        rb.isKinematic = false;

        var grab = obj.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = obj.AddComponent<XRGrabInteractable>();
        grab.interactionLayers = InteractionLayerMask.GetMask("Cocina");
        grab.throwOnDetach = false;

        var attachGo = obj.transform.Find("AttachPoint");
        if (attachGo == null)
        {
            var go = new GameObject("AttachPoint");
            go.transform.SetParent(obj.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            attachGo = go.transform;
        }
        grab.attachTransform = attachGo;

        if (obj.GetComponent<KitchenItemInteractable>() == null)
            obj.AddComponent<KitchenItemInteractable>();

        obj.transform.position = socketWorldPos + DefaultDisplacement;
    }

    static void SetupSocket(GameObject socketsRoot, GameObject obj, Material ghostMat,
        Vector3 socketWorldPos, Quaternion socketWorldRot, string objectName)
    {
        string socketName = $"Socket_{objectName}";
        var socketTf = socketsRoot.transform.Find(socketName);
        GameObject socketGo;
        if (socketTf == null)
        {
            socketGo = new GameObject(socketName);
            socketGo.transform.SetParent(socketsRoot.transform, true);
        }
        else
        {
            socketGo = socketTf.gameObject;
        }

        socketGo.transform.position = socketWorldPos;
        socketGo.transform.rotation = socketWorldRot;

        var socket = socketGo.GetComponent<XRSocketInteractor>();
        if (socket == null) socket = socketGo.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = InteractionLayerMask.GetMask("Cocina");

        var col = socketGo.GetComponent<SphereCollider>();
        if (col == null) col = socketGo.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.2f;

        var attachTf = socketGo.transform.Find("AttachTransform");
        if (attachTf == null)
        {
            var go = new GameObject("AttachTransform");
            go.transform.SetParent(socketGo.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            attachTf = go.transform;
        }
        socket.attachTransform = attachTf;

        // GhostGuide
        var ghostTf = socketGo.transform.Find("GhostGuide");
        GameObject ghostGo;
        if (ghostTf == null)
        {
            ghostGo = new GameObject("GhostGuide");
            ghostGo.transform.SetParent(socketGo.transform, false);
            ghostGo.transform.localPosition = Vector3.zero;
            ghostGo.transform.localRotation = Quaternion.identity;
        }
        else
        {
            ghostGo = ghostTf.gameObject;
            ghostGo.transform.localPosition = Vector3.zero;
            ghostGo.transform.localRotation = Quaternion.identity;
        }

        var srcFilter = obj.GetComponentInChildren<MeshFilter>();
        if (srcFilter != null)
        {
            var gf = ghostGo.GetComponent<MeshFilter>();
            if (gf == null) gf = ghostGo.AddComponent<MeshFilter>();
            gf.sharedMesh = srcFilter.sharedMesh;

            var gr = ghostGo.GetComponent<MeshRenderer>();
            if (gr == null) gr = ghostGo.AddComponent<MeshRenderer>();
            gr.sharedMaterial = ghostMat;
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gr.receiveShadows = false;
        }

        // KitchenSocketController
        var controller = socketGo.GetComponent<KitchenSocketController>();
        if (controller == null) controller = socketGo.AddComponent<KitchenSocketController>();

        var so = new SerializedObject(controller);
        so.FindProperty("_trackedItem").objectReferenceValue =
            obj.GetComponent<KitchenItemInteractable>();
        so.FindProperty("_guideVisual").objectReferenceValue = ghostGo;
        so.FindProperty("_objectName").stringValue = objectName;
        so.ApplyModifiedProperties();
    }

    static void CreatePlacementBoard(GameObject socketsRoot)
    {
        // Remove legacy board (wrong canvas scale / parented under KitchenSockets).
        var legacy = socketsRoot.transform.Find("PlacementBoard");
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy.gameObject);

        var existing = GameObject.Find("KitchenPlacementBoard");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var boardGo = new GameObject("KitchenPlacementBoard");
        Undo.RegisterCreatedObjectUndo(boardGo, "Create Kitchen Placement Board");
        boardGo.transform.SetPositionAndRotation(new Vector3(2f, 1.6f, -3f), Quaternion.Euler(0f, 180f, 0f));

        var placement = boardGo.AddComponent<KitchenPlacementBoardPlacement>();
        var placementSo = new SerializedObject(placement);
        placementSo.FindProperty("_worldPosition").vector3Value = new Vector3(2f, 1.6f, -3f);
        placementSo.FindProperty("_yawDegrees").floatValue = 180f;
        placementSo.ApplyModifiedPropertiesWithoutUndo();

        boardGo.AddComponent<KitchenPlacementBoard>();
        boardGo.AddComponent<KitchenVictoryChecker>();

        Debug.Log("[KitchenSocket] KitchenPlacementBoard created at (2, 1.6, -3) — re-run if an old PlacementBoard still exists.");
    }

    static Material GetOrCreateGhostMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GhostMatPath);
        if (mat != null) return mat;

        string dir = System.IO.Path.GetDirectoryName(GhostMatPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetColor("_BaseColor", new Color(0f, 0.5f, 1f, 0.25f));
        mat.SetFloat("_Smoothness", 0.5f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        AssetDatabase.CreateAsset(mat, GhostMatPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[KitchenSocket] Created ghost material at {GhostMatPath}");
        return mat;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    static GameObject FindInScene(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
