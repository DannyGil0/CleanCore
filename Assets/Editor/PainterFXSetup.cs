using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PainterFXSetup
{
    [MenuItem("Tools/Water Gun/Setup Farming FX")]
    public static void SetupPainterFX()
    {
        Painter painter = Object.FindAnyObjectByType<Painter>();
        if (painter == null)
        {
            Debug.LogError("[PainterFXSetup] No se encontró el componente Painter en la escena actual.");
            return;
        }

        Undo.RecordObject(painter, "Setup Painter FX");
        SerializedObject serializedObject = new SerializedObject(painter);

        // 1. Configurar el chorro de agua (Spray)
        GameObject sprayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Farming_game_FX/Prefabs/Farming/WateringCan.prefab");
        if (sprayPrefab != null)
        {
            // Intentamos anclarlo a la mano derecha; si no, al transform base
            Transform rightController = (Transform)serializedObject.FindProperty("rightController").objectReferenceValue;
            Transform parent = rightController != null ? rightController : painter.transform;

            Transform existingSpray = parent.Find("WateringCan_FX");
            if (existingSpray != null)
            {
                Undo.DestroyObjectImmediate(existingSpray.gameObject);
            }

            GameObject sprayInstance = (GameObject)PrefabUtility.InstantiatePrefab(sprayPrefab, parent);
            sprayInstance.name = "WateringCan_FX";
            sprayInstance.transform.localPosition = Vector3.zero;
            sprayInstance.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(sprayInstance, "Create Spray FX");

            ParticleSystem ps = sprayInstance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                serializedObject.FindProperty("sprayEffect").objectReferenceValue = ps;
            }
        }
        else
        {
            Debug.LogError("[PainterFXSetup] No se encontró el prefab WateringCan en Assets/Farming_game_FX/Prefabs/Farming/");
        }

        // 2. Configurar el impacto/salpicadura
        GameObject impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Farming_game_FX/Prefabs/Fishing/FX_WaterSplash1.prefab");
        if (impactPrefab != null)
        {
            Transform existingImpact = GameObject.Find("WaterSplash_FX")?.transform;
            if (existingImpact != null)
            {
                Undo.DestroyObjectImmediate(existingImpact.gameObject);
            }

            GameObject impactInstance = (GameObject)PrefabUtility.InstantiatePrefab(impactPrefab);
            impactInstance.name = "WaterSplash_FX";
            impactInstance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(impactInstance, "Create Impact FX");

            ParticleSystem ps = impactInstance.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                serializedObject.FindProperty("impactEffect").objectReferenceValue = ps;
            }
        }
        else
        {
            Debug.LogError("[PainterFXSetup] No se encontró el prefab FX_WaterSplash1 en Assets/Farming_game_FX/Prefabs/Fishing/");
        }

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(painter);
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
        Debug.Log("<color=green>[PainterFXSetup] Efectos de agua aplicados exitosamente al script Painter.</color> Guarda tu escena.");
    }
}
