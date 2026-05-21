using UnityEngine;
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

    const string SaucePanName = "SM_SaucePan_Black_Pan_01_90";
    const string StoveName = "Assets_Proxy_Stove_Proxy_58";

    // Hardcoded socket offset from stove (from original scene positions)
    // Stove world: (1.90, -0.63, -1.12), Pan world: (2.22, -0.09, -1.38)
    // These are local to the parent transform (fileID: 1367946155)
    static readonly Vector3 SocketLocalOffsetFromStove = new Vector3(0.32f, 0.54f, -0.26f);

    // Saucepan displaced start: offset from socket position
    static readonly Vector3 SaucePanDisplacement = new Vector3(0.5f, 0.3f, 0.3f);

    [MenuItem("Tools/Kitchen Socket/Setup Stove Socket")]
    static void RunSetup()
    {
        var saucePan = FindInScene(SaucePanName);
        var stove = FindInScene(StoveName);

        if (saucePan == null)
        {
            Debug.LogError($"[KitchenSocket] '{SaucePanName}' not found in scene.");
            return;
        }
        if (stove == null)
        {
            Debug.LogError($"[KitchenSocket] '{StoveName}' not found in scene.");
            return;
        }

        Undo.SetCurrentGroupName("Kitchen Socket Setup");

        var ghostMat = GetOrCreateGhostMaterial();

        // Socket position: stove world pos + fixed offset (idempotent)
        Vector3 socketWorldPos = stove.transform.position + SocketLocalOffsetFromStove;
        Quaternion socketWorldRot = stove.transform.rotation;

        SetupSaucePan(saucePan, socketWorldPos);
        SetupStoveSocket(stove, saucePan, ghostMat, socketWorldPos, socketWorldRot);
        FixNearFarInteractorRaycastMask();
        FixHandInteractionLayers();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[KitchenSocket] Setup complete. Save scene to persist.");
    }

    static void FixNearFarInteractorRaycastMask()
    {
        // Unpack XR Origin prefab so caster changes persist
        var xrOrigins = Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None);
        foreach (var origin in xrOrigins)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(origin.gameObject))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(origin.gameObject);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log($"[KitchenSocket] Unpacked XR Origin prefab instance");
            }
        }

        var casters = Object.FindObjectsByType<CurveInteractionCaster>(FindObjectsSortMode.None);
        foreach (var caster in casters)
        {
            LayerMask current = caster.raycastMask;
            int needed = current | (1 << 0) | (1 << 6);
            if ((int)current != needed)
            {
                caster.raycastMask = needed;
                Debug.Log($"[KitchenSocket] Raycast mask fixed on {caster.gameObject.name}: {(int)current} -> {needed}");
            }
        }

        var sphereCasters = Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsSortMode.None);
        foreach (var caster in sphereCasters)
        {
            LayerMask current = caster.physicsLayerMask;
            int needed = current | (1 << 0) | (1 << 6);
            if ((int)current != needed)
            {
                caster.physicsLayerMask = needed;
                Debug.Log($"[KitchenSocket] Sphere mask fixed on {caster.gameObject.name}: {(int)current} -> {needed}");
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
                Debug.Log($"[KitchenSocket] Right hand: Cocina removed");
            }
            else if (isLeft)
            {
                int current = (int)(InteractionLayerMask)interactor.interactionLayers;
                interactor.interactionLayers = current | cocinaBit;
                interactor.enableFarCasting = true;
                interactor.farAttachMode = InteractorFarAttachMode.Near;
                Debug.Log($"[KitchenSocket] Left hand: Cocina added, farAttachMode=Near");
            }
        }
    }

    static void SetupSaucePan(GameObject pan, Vector3 socketWorldPos)
    {
        // UNPACK prefab instance so all changes persist directly in scene
        if (PrefabUtility.IsPartOfPrefabInstance(pan))
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(pan);
            if (root != null)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log($"[KitchenSocket] Unpacked prefab instance: {root.name}");
            }
        }

        // Layer 0 (Default)
        SetLayerRecursive(pan, 0);

        // Remove static flags
        GameObjectUtility.SetStaticEditorFlags(pan, 0);
        foreach (Transform child in pan.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);

        // Make MeshColliders convex
        var meshCols = pan.GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshCols)
            mc.convex = true;

        // BoxCollider on root
        if (pan.GetComponent<Collider>() == null)
        {
            var box = pan.AddComponent<BoxCollider>();
            var renderer = pan.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                box.center = pan.transform.InverseTransformPoint(renderer.bounds.center);
                box.size = Vector3.Max(
                    new Vector3(
                        Mathf.Abs(pan.transform.InverseTransformVector(renderer.bounds.size).x),
                        Mathf.Abs(pan.transform.InverseTransformVector(renderer.bounds.size).y),
                        Mathf.Abs(pan.transform.InverseTransformVector(renderer.bounds.size).z)),
                    Vector3.one * 0.1f);
            }
        }

        // Rigidbody
        var rb = pan.GetComponent<Rigidbody>();
        if (rb == null) rb = pan.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        rb.isKinematic = false;

        // XRGrabInteractable — Cocina ONLY so only left hand can grab
        var grab = pan.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = pan.AddComponent<XRGrabInteractable>();
        grab.interactionLayers = InteractionLayerMask.GetMask("Cocina");
        grab.throwOnDetach = false;

        // AttachPoint
        var attachGo = pan.transform.Find("AttachPoint");
        if (attachGo == null)
        {
            var go = new GameObject("AttachPoint");
            go.transform.SetParent(pan.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            attachGo = go.transform;
        }
        grab.attachTransform = attachGo;

        // KitchenItemInteractable
        if (pan.GetComponent<KitchenItemInteractable>() == null)
            pan.AddComponent<KitchenItemInteractable>();

        // Position saucepan at displaced location (away from socket)
        pan.transform.position = socketWorldPos + SaucePanDisplacement;

        Debug.Log($"[KitchenSocket] SaucePan at {pan.transform.position} (socket at {socketWorldPos}). Layer={pan.layer}");
    }

    static void SetupStoveSocket(GameObject stove, GameObject saucePan, Material ghostMat,
        Vector3 socketWorldPos, Quaternion socketWorldRot)
    {
        var socketTf = stove.transform.Find("StoveSocket");
        GameObject socketGo;
        if (socketTf == null)
        {
            socketGo = new GameObject("StoveSocket");
            Undo.RegisterCreatedObjectUndo(socketGo, "Create StoveSocket");
            socketGo.transform.SetParent(stove.transform, true);
        }
        else
        {
            socketGo = socketTf.gameObject;
        }

        // Socket always at the fixed stove-top position
        socketGo.transform.position = socketWorldPos;
        socketGo.transform.rotation = socketWorldRot;

        // XRSocketInteractor — Cocina layer
        var socket = socketGo.GetComponent<XRSocketInteractor>();
        if (socket == null) socket = Undo.AddComponent<XRSocketInteractor>(socketGo);
        socket.interactionLayers = InteractionLayerMask.GetMask("Cocina");

        var col = socketGo.GetComponent<SphereCollider>();
        if (col == null) col = Undo.AddComponent<SphereCollider>(socketGo);
        col.isTrigger = true;
        col.radius = 0.2f;

        // AttachTransform
        var attachTf = socketGo.transform.Find("AttachTransform");
        if (attachTf == null)
        {
            var go = new GameObject("AttachTransform");
            Undo.RegisterCreatedObjectUndo(go, "Create AttachTransform");
            go.transform.SetParent(socketGo.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            attachTf = go.transform;
        }
        socket.attachTransform = attachTf;

        // GhostGuide — visual hint at socket position
        var ghostTf = socketGo.transform.Find("GhostGuide");
        GameObject ghostGo;
        if (ghostTf == null)
        {
            ghostGo = new GameObject("GhostGuide");
            Undo.RegisterCreatedObjectUndo(ghostGo, "Create GhostGuide");
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

        var srcFilter = saucePan.GetComponentInChildren<MeshFilter>();
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
        if (controller == null) controller = Undo.AddComponent<KitchenSocketController>(socketGo);

        var so = new SerializedObject(controller);
        so.FindProperty("_trackedItem").objectReferenceValue =
            saucePan.GetComponent<KitchenItemInteractable>();
        so.FindProperty("_guideVisual").objectReferenceValue = ghostGo;
        so.ApplyModifiedProperties();

        Debug.Log($"[KitchenSocket] Socket+Ghost at {socketWorldPos}");
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
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
