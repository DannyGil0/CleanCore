using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class HouseCleaningSetup
{
    const string MeshPath  = "Assets/StylArts/StylizedHouseInterior/Art/Meshes";
    const string ScenePath = "Assets/StylArts/StylizedHouseInterior/Scene/URP_Stylized_House_Interior.unity";
    const string DirtTexPath     = "Assets/Textures/GeneratedDirtTex.png";
    const string PaintShaderPath = "Assets/Shaders/Paint.shader";
    const string CoverageShaderPath = "Assets/Shaders/ParallelReduce.compute";
    const string ClonedMatFolder = "Assets/HouseUberMaterials";
    const string OrigMatFolder   = "Assets/StylArts/StylizedHouseInterior/Art/Materials";
    const int    EnvironmentLayer = 6;
    const float  MinBoundsSize    = 0.25f;

    static readonly HashSet<string> SkipShaderNames = new HashSet<string>
    {
        "Shader Graphs/S_Glass",
        "Shader Graphs/S_Sky",
        "Shader Graphs/S_PBR_TRANSPARENT_ORM_LeartesMasterMaterial",
        "Shader Graphs/S_VertexPaintGround",
        "Universal Render Pipeline/Unlit",
        "Skybox/Procedural",
        "Skybox/6 Sided",
        "Skybox/Cubemap",
        "TextMeshPro/Mobile/Distance Field",
        "UI/Default",
    };

    // ─────────────────────── 0  DIAGNOSTICO ───────────────────────
    [MenuItem("Tools/House Cleaning Setup/0 - Diagnostico")]
    static void Diagnostico()
    {
        Shader uberShader  = Shader.Find("Custom/Uber Shader");
        Shader paintShader = Shader.Find("Custom/Paint");
        ComputeShader covS = AssetDatabase.LoadAssetAtPath<ComputeShader>(CoverageShaderPath);
        Texture2D dirtTex  = AssetDatabase.LoadAssetAtPath<Texture2D>(DirtTexPath);

        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { MeshPath });
        int uv1Count = 0;
        foreach (string g in modelGuids)
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as ModelImporter;
            if (imp != null && imp.generateSecondaryUV) uv1Count++;
        }

        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var surfaces  = Object.FindObjectsByType<PaintableSurface>(FindObjectsSortMode.None);
        var ui        = Object.FindFirstObjectByType<PaintableSurfaceUI>();

        var shaderCounts = new Dictionary<string, int>();
        int uberCount = 0, nullBaseMap = 0;
        var uniqueMats = new HashSet<Material>();

        foreach (var rend in renderers)
            foreach (var m in rend.sharedMaterials)
            {
                if (m == null) continue;
                shaderCounts.TryGetValue(m.shader.name, out int c);
                shaderCounts[m.shader.name] = c + 1;
                if (uberShader != null && m.shader == uberShader)
                {
                    uberCount++;
                    if (uniqueMats.Add(m) && m.GetTexture("_BaseMap") == null)
                        nullBaseMap++;
                }
            }

        string sl = "";
        foreach (var kv in shaderCounts) sl += $"  {kv.Key}: {kv.Value}\n";

        string msg = "=== DIAGNOSTICO ===\n\n"
            + $"Uber Shader: {(uberShader != null ? "SI" : "NO")}\n"
            + $"Paint Shader: {(paintShader != null ? "SI" : "NO")}\n"
            + $"Coverage Shader: {(covS != null ? "SI" : "NO")}\n"
            + $"DirtTex: {(dirtTex != null ? "SI" : "NO")}\n\n"
            + $"FBX con UV1: {uv1Count}/{modelGuids.Length}\n\n"
            + $"MeshRenderers: {renderers.Length}\n"
            + $"  Con Uber Shader: {uberCount}\n"
            + $"  Uber con _BaseMap NULL: {nullBaseMap} materiales unicos\n"
            + $"PaintableSurface: {surfaces.Length}\n"
            + $"PaintableSurfaceUI: {(ui != null ? "SI" : "NO")}\n\n"
            + $"Shaders en uso:\n{sl}\n"
            + $"Escena: {EditorSceneManager.GetActiveScene().path}";

        Debug.Log(msg);
        EditorUtility.DisplayDialog("Diagnostico", msg, "OK");
    }

    // ─────────────────────── RESET ─────────────────────────────────
    [MenuItem("Tools/House Cleaning Setup/RESET - Limpiar Todo")]
    static void ResetAll()
    {
        if (!EditorUtility.DisplayDialog("RESET",
            "Esto eliminara todos los PaintableSurface, MeshCollider agregados, "
            + "restaurara materiales originales y borrara HouseUberMaterials.\n\n"
            + "Continuar?", "Si", "Cancelar"))
            return;

        // Build lookup: clone name -> original material in Art/Materials/
        var origMats = new Dictionary<string, Material>();
        string[] origGuids = AssetDatabase.FindAssets("t:Material", new[] { OrigMatFolder });
        foreach (string g in origGuids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null) origMats[m.name] = m;
        }

        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int restored = 0;

        foreach (var rend in renderers)
        {
            Material[] mats = rend.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string path = AssetDatabase.GetAssetPath(mats[i]);
                if (!path.StartsWith(ClonedMatFolder)) continue;

                string origName = mats[i].name;
                if (origName.EndsWith("_Uber"))
                    origName = origName.Substring(0, origName.Length - 5);

                if (origMats.TryGetValue(origName, out Material orig))
                {
                    mats[i] = orig;
                    changed = true;
                    restored++;
                }
            }

            if (changed)
            {
                rend.sharedMaterials = mats;
                EditorUtility.SetDirty(rend);
            }
        }

        // Remove PaintableSurface components
        var surfaces = Object.FindObjectsByType<PaintableSurface>(FindObjectsSortMode.None);
        foreach (var ps in surfaces)
            Undo.DestroyObjectImmediate(ps);

        // Delete HouseUberMaterials folder
        if (AssetDatabase.IsValidFolder(ClonedMatFolder))
            AssetDatabase.DeleteAsset(ClonedMatFolder);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[RESET] Materiales restaurados: {restored}, PaintableSurface eliminados: {surfaces.Length}");
        EditorUtility.DisplayDialog("RESET",
            $"Materiales restaurados: {restored}\nPaintableSurface eliminados: {surfaces.Length}\n\n"
            + "Ahora ejecuta Fase 2-3 de nuevo.", "OK");
    }

    // ─────────────────────── 1  ENABLE UV1 ────────────────────────
    [MenuItem("Tools/House Cleaning Setup/1 - Enable UV1 on House Meshes")]
    static void Fase1_EnableSecondaryUV()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MeshPath });
        int changed = 0;
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Fase 1 - UV1",
                    $"{System.IO.Path.GetFileName(path)} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length)) break;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && !importer.generateSecondaryUV)
                {
                    importer.generateSecondaryUV = true;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        Debug.Log($"[Fase 1] UV1: {changed}/{guids.Length}");
        EditorUtility.DisplayDialog("Fase 1", $"UV1 habilitado en {changed}/{guids.Length} meshes.", "OK");
    }

    // ────────────────── 2-3  SWITCH + SETUP ───────────────────────
    [MenuItem("Tools/House Cleaning Setup/2-3 - Switch Shaders + Setup Surfaces")]
    static void Fase23_SwitchAndSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.path.Contains("URP_Stylized_House_Interior"))
        {
            if (!EditorUtility.DisplayDialog("Escena incorrecta",
                $"Activa: {scene.path}\n\nAbrir URP_Stylized_House_Interior?", "Abrir", "Cancelar"))
                return;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Shader uberShader  = Shader.Find("Custom/Uber Shader");
        Shader paintShader = AssetDatabase.LoadAssetAtPath<Shader>(PaintShaderPath);
        ComputeShader covS = AssetDatabase.LoadAssetAtPath<ComputeShader>(CoverageShaderPath);
        if (uberShader == null || paintShader == null || covS == null)
        {
            Debug.LogError("[Fase 2-3] Faltan shaders"); return;
        }

        Texture2D dirtTex = EnsureDirtTexture();

        if (!AssetDatabase.IsValidFolder(ClonedMatFolder))
            AssetDatabase.CreateFolder("Assets", "HouseUberMaterials");

        // Build lookup of original materials in Art/Materials/ for fallback
        var origMatLookup = new Dictionary<string, Material>();
        string[] origGuids = AssetDatabase.FindAssets("t:Material", new[] { OrigMatFolder });
        foreach (string g in origGuids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null) origMatLookup[m.name] = m;
        }

        var matMap = new Dictionary<Material, Material>();
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int matCreated = 0, surfaceCount = 0, skipped = 0;

        try
        {
            for (int ri = 0; ri < renderers.Length; ri++)
            {
                var rend = renderers[ri];
                if (ri % 100 == 0)
                    EditorUtility.DisplayProgressBar("Fase 2-3",
                        $"Renderer {ri + 1}/{renderers.Length}", (float)ri / renderers.Length);

                if (!ShouldProcess(rend, uberShader)) { skipped++; continue; }

                Material[] mats = rend.sharedMaterials;
                bool anyValid = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material orig = mats[i];
                    if (orig == null) continue;
                    if (SkipShaderNames.Contains(orig.shader.name)) continue;

                    // Already a good Uber material
                    if (orig.shader == uberShader && orig.GetTexture("_BaseMap") != null)
                    {
                        anyValid = true;
                        continue;
                    }

                    // Already cloned this material
                    if (matMap.TryGetValue(orig, out Material cached))
                    {
                        mats[i] = cached;
                        anyValid = true;
                        continue;
                    }

                    // Create new Uber clone
                    Material clone = new Material(uberShader);
                    string baseName = orig.name.Replace("_Uber", "");
                    clone.name = baseName + "_Uber";

                    // Use orig as texture source, but if it's a broken Uber material,
                    // find the matching original in Art/Materials/
                    Material textureSource = orig;
                    if (orig.shader == uberShader && orig.GetTexture("_BaseMap") == null)
                    {
                        // Try name variants: MI_X -> M_X, MM_X -> M_X, X -> M_X
                        string lookupName = baseName;
                        if (lookupName.StartsWith("MI_"))
                            lookupName = "M_" + lookupName.Substring(3);
                        else if (lookupName.StartsWith("MM_"))
                            lookupName = "M_" + lookupName.Substring(3);

                        if (origMatLookup.TryGetValue(lookupName, out Material origM))
                            textureSource = origM;
                        else if (origMatLookup.TryGetValue(baseName, out Material origM2))
                            textureSource = origM2;
                        else if (origMatLookup.TryGetValue("M_" + baseName, out Material origM3))
                            textureSource = origM3;
                        else
                            Debug.LogWarning($"[Fase 2-3] No se encontro original para '{baseName}' en Art/Materials/");
                    }

                    CopyTexturesFromSource(textureSource, clone, dirtTex);

                    string safeName = SanitizeFileName(clone.name);
                    string savePath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{ClonedMatFolder}/{safeName}.mat");
                    AssetDatabase.CreateAsset(clone, savePath);

                    matMap[orig] = clone;
                    mats[i] = clone;
                    anyValid = true;
                    matCreated++;
                }

                if (!anyValid) continue;

                rend.sharedMaterials = mats;
                EditorUtility.SetDirty(rend);

                GameObject go = rend.gameObject;
                go.layer = EnvironmentLayer;

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags &= ~StaticEditorFlags.BatchingStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);

                if (go.GetComponent<Collider>() == null)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        Undo.AddComponent<MeshCollider>(go);
                }

                if (go.GetComponent<PaintableSurface>() == null)
                {
                    PaintableSurface ps = Undo.AddComponent<PaintableSurface>(go);
                    ps.TextureSize    = PaintableSurface.TextureSizes.M;
                    ps.PaintShader    = paintShader;
                    ps.CoverageShader = covS;

                    var so = new SerializedObject(ps);
                    var tP = so.FindProperty("_transform");
                    var rP = so.FindProperty("_mainRenderer");
                    if (tP != null) tP.objectReferenceValue = go.transform;
                    if (rP != null) rP.objectReferenceValue = rend;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ps);
                    surfaceCount++;
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        string msg = $"Materiales creados: {matCreated}\n"
            + $"PaintableSurface: {surfaceCount}\n"
            + $"Omitidos: {skipped}\n\nAhora ejecuta Fase 4.";
        Debug.Log($"[Fase 2-3] {msg}");
        EditorUtility.DisplayDialog("Fase 2-3", msg, "OK");
    }

    // ───────── Copy textures using shader property enumeration ─────
    static void CopyTexturesFromSource(Material src, Material dst, Texture2D dirtTex)
    {
        Shader srcShader = src.shader;
        int propCount = srcShader.GetPropertyCount();

        Texture albedo = null, normal = null, metallic = null, emission = null;
        Color baseColor = Color.white;
        bool foundColor = false;

        for (int i = 0; i < propCount; i++)
        {
            string pName = srcShader.GetPropertyName(i);
            string desc  = srcShader.GetPropertyDescription(i).ToLowerInvariant();
            var pType    = srcShader.GetPropertyType(i);

            if (pType == ShaderPropertyType.Texture)
            {
                Texture tex = src.GetTexture(pName);
                if (tex == null) continue;

                if (albedo == null && (desc.Contains("base") && desc.Contains("colo")
                    || desc.Contains("albedo") || desc.Contains("diffuse")
                    || desc.Contains("basecolor") || desc.Contains("base colour")))
                    albedo = tex;
                else if (normal == null && desc.Contains("normal"))
                    normal = tex;
                else if (metallic == null && (desc.Contains("orm") || desc.Contains("metallic")
                    || desc.Contains("roughmask") || desc.Contains("metallicgloss")))
                    metallic = tex;
                else if (emission == null && desc.Contains("emission"))
                    emission = tex;
            }
            else if (pType == ShaderPropertyType.Color)
            {
                if (!foundColor && (desc.Contains("base") || desc.Contains("tint")
                    || desc.Contains("color") || desc.Contains("colour")))
                {
                    baseColor = src.GetColor(pName);
                    foundColor = true;
                }
            }
        }

        // Fallback: try standard property names directly on the material
        if (albedo == null)
        {
            albedo = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
            if (albedo == null)
                albedo = src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
        }
        if (normal == null && src.HasProperty("_BumpMap"))
            normal = src.GetTexture("_BumpMap");
        if (metallic == null && src.HasProperty("_MetallicGlossMap"))
            metallic = src.GetTexture("_MetallicGlossMap");

        // Last resort: try known ShaderGraph property names from serialized data
        if (albedo == null)
        {
            // S_PBR_OPAQUE_ORM: "Material 01 Base Colour"
            albedo = src.GetTexture("Texture2D_2A1A8040");
        }
        if (albedo == null)
        {
            // S_Kitchen_Master: "BaseColor"
            albedo = src.GetTexture("Texture2D_296162aee2a745d3b57152085d1d03fd");
        }
        if (normal == null)
        {
            normal = src.GetTexture("Texture2D_7EA392D1")     // PBR ORM Normal
                  ?? src.GetTexture("Texture2D_eb55fdf554d54ac0bf51809f94be4d07"); // Kitchen Normal
        }

        // Assign to Uber Shader
        if (albedo != null)
        {
            dst.SetTexture("_BaseMap", albedo);
            dst.SetTexture("_MainTex", albedo);
        }
        if (normal != null)
        {
            dst.SetTexture("_BumpMap", normal);
            dst.EnableKeyword("_NORMALMAP");
        }
        if (metallic != null)
        {
            dst.SetTexture("_MetallicGlossMap", metallic);
            dst.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (emission != null)
        {
            dst.SetTexture("_EmissionMap", emission);
            dst.EnableKeyword("_EMISSION");
        }

        // Fallback color: HDRP materials store color as _Color, not _BaseColor
        if (!foundColor)
        {
            Color c = src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            if (c != Color.white || !src.HasProperty("_BaseColor"))
            {
                baseColor = c;
                foundColor = true;
            }
            else if (src.HasProperty("_BaseColor"))
            {
                baseColor = src.GetColor("_BaseColor");
                foundColor = true;
            }
        }

        if (foundColor)
            dst.SetColor("_BaseColor", baseColor);

        if (dirtTex != null)
            dst.SetTexture("_DirtTex", dirtTex);

        if (albedo == null)
            Debug.LogWarning($"[Fase 2-3] No se encontro albedo para: {src.name} (shader: {srcShader.name})");
    }

    static bool ShouldProcess(MeshRenderer rend, Shader uberShader)
    {
        MeshFilter mf = rend.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        if (rend.sharedMaterials.Length == 0) return false;

        bool allSkip = true;
        foreach (Material m in rend.sharedMaterials)
        {
            if (m == null) continue;
            if (SkipShaderNames.Contains(m.shader.name)) continue;
            // Good Uber material also counts as processable
            if (m.shader == uberShader && m.GetTexture("_BaseMap") != null)
            { allSkip = false; break; }
            // Non-skip shader that needs conversion
            if (m.shader != uberShader)
            { allSkip = false; break; }
            // Uber with null _BaseMap needs reconversion
            if (m.shader == uberShader && m.GetTexture("_BaseMap") == null)
            { allSkip = false; break; }
        }
        if (allSkip) return false;

        float maxDim = Mathf.Max(rend.bounds.size.x, rend.bounds.size.y, rend.bounds.size.z);
        return maxDim >= MinBoundsSize;
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    // ────────────────────── DIRT TEXTURE ───────────────────────────
    static Texture2D EnsureDirtTexture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(DirtTexPath);
        if (existing != null) return existing;

        int sz = 256;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f)
                        + Mathf.PerlinNoise(x * 0.12f + 100f, y * 0.12f + 100f) * 0.3f;
                tex.SetPixel(x, y, new Color(
                    Mathf.Lerp(0.22f, 0.45f, n),
                    Mathf.Lerp(0.16f, 0.32f, n),
                    Mathf.Lerp(0.08f, 0.18f, n), 1f));
            }
        tex.Apply();

        string dir = System.IO.Path.Combine(Application.dataPath, "Textures");
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(dir, "GeneratedDirtTex.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(DirtTexPath);
    }

    // ──────────────────────── 4  CONNECT UI ───────────────────────
    [MenuItem("Tools/House Cleaning Setup/4 - Connect UI to Surfaces")]
    static void Fase4_ConnectUI()
    {
        var ui = Object.FindFirstObjectByType<PaintableSurfaceUI>();
        if (ui == null)
        {
            EditorUtility.DisplayDialog("ERROR", "PaintableSurfaceUI no encontrado.", "OK");
            return;
        }

        var surfaces = Object.FindObjectsByType<PaintableSurface>(FindObjectsSortMode.None);
        if (surfaces.Length == 0)
        {
            EditorUtility.DisplayDialog("ERROR", "0 PaintableSurface. Ejecuta Fase 2-3.", "OK");
            return;
        }

        var so   = new SerializedObject(ui);
        var prop = so.FindProperty("_surfaces");
        prop.arraySize = surfaces.Length;
        for (int i = 0; i < surfaces.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = surfaces[i];
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[Fase 4] UI -> {surfaces.Length} superficies");
        EditorUtility.DisplayDialog("Fase 4", $"UI conectado con {surfaces.Length} superficies.", "OK");
    }

    // ─────────── 5  FIX SELECTED (bypass all filters) ─────────────
    [MenuItem("Tools/House Cleaning Setup/5 - Fix Selected Objects")]
    static void FixSelected()
    {
        Shader uberShader  = Shader.Find("Custom/Uber Shader");
        Shader paintShader = AssetDatabase.LoadAssetAtPath<Shader>(PaintShaderPath);
        ComputeShader covS = AssetDatabase.LoadAssetAtPath<ComputeShader>(CoverageShaderPath);
        if (uberShader == null || paintShader == null || covS == null)
        {
            Debug.LogError("[Fix] Faltan shaders"); return;
        }

        Texture2D dirtTex = EnsureDirtTexture();

        if (!AssetDatabase.IsValidFolder(ClonedMatFolder))
            AssetDatabase.CreateFolder("Assets", "HouseUberMaterials");

        // Build original material lookup for fallback
        var origMatLookup = new Dictionary<string, Material>();
        string[] origGuids = AssetDatabase.FindAssets("t:Material", new[] { OrigMatFolder });
        foreach (string g in origGuids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m != null) origMatLookup[m.name] = m;
        }

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Fix Selected",
                "Selecciona uno o mas objetos en la jerarquia primero.", "OK");
            return;
        }

        int matCreated = 0, surfaceCount = 0, rendCount = 0;

        foreach (GameObject root in selected)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer rend in renderers)
            {
                rendCount++;
                Material[] mats = rend.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material orig = mats[i];
                    if (orig == null) continue;
                    if (orig.shader == uberShader && orig.GetTexture("_BaseMap") != null)
                        continue;

                    Material clone = new Material(uberShader);
                    string baseName = orig.name.Replace("_Uber", "");
                    clone.name = baseName + "_Uber";

                    // Find best texture source
                    Material textureSource = orig;
                    if (orig.shader == uberShader && orig.GetTexture("_BaseMap") == null)
                    {
                        string lookupName = baseName;
                        if (lookupName.StartsWith("MI_"))
                            lookupName = "M_" + lookupName.Substring(3);
                        else if (lookupName.StartsWith("MM_"))
                            lookupName = "M_" + lookupName.Substring(3);

                        if (origMatLookup.TryGetValue(lookupName, out Material o1))
                            textureSource = o1;
                        else if (origMatLookup.TryGetValue(baseName, out Material o2))
                            textureSource = o2;
                        else if (origMatLookup.TryGetValue("M_" + baseName, out Material o3))
                            textureSource = o3;
                    }

                    CopyTexturesFromSource(textureSource, clone, dirtTex);

                    string safeName = SanitizeFileName(clone.name);
                    string savePath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{ClonedMatFolder}/{safeName}.mat");
                    AssetDatabase.CreateAsset(clone, savePath);

                    mats[i] = clone;
                    changed = true;
                    matCreated++;
                }

                if (changed)
                {
                    rend.sharedMaterials = mats;
                    EditorUtility.SetDirty(rend);
                }

                GameObject go = rend.gameObject;
                go.layer = EnvironmentLayer;

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags &= ~StaticEditorFlags.BatchingStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);

                if (go.GetComponent<Collider>() == null)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        Undo.AddComponent<MeshCollider>(go);
                }

                if (go.GetComponent<PaintableSurface>() == null)
                {
                    PaintableSurface ps = Undo.AddComponent<PaintableSurface>(go);
                    ps.TextureSize    = PaintableSurface.TextureSizes.M;
                    ps.PaintShader    = paintShader;
                    ps.CoverageShader = covS;

                    var so = new SerializedObject(ps);
                    var tP = so.FindProperty("_transform");
                    var rP = so.FindProperty("_mainRenderer");
                    if (tP != null) tP.objectReferenceValue = go.transform;
                    if (rP != null) rP.objectReferenceValue = rend;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ps);
                    surfaceCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        string msg = $"Renderers: {rendCount}\nMateriales creados: {matCreated}\n"
            + $"PaintableSurface agregados: {surfaceCount}\n\n"
            + "Ejecuta Fase 4 para reconectar el UI.";
        Debug.Log($"[Fix Selected] {msg}");
        EditorUtility.DisplayDialog("Fix Selected", msg, "OK");
    }
}
