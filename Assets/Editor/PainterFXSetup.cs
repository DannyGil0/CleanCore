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

        Transform rightController = (Transform)serializedObject.FindProperty("rightController").objectReferenceValue;
        Transform parent = rightController != null ? rightController : painter.transform;

        // 0. Configurar el modelo 3D de la Pistola de Agua
        GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)WaterGun.prefab");
        Transform gunInstanceTf = parent.Find("(Prb)WaterGun");
        
        if (gunInstanceTf != null)
        {
            Undo.DestroyObjectImmediate(gunInstanceTf.gameObject);
        }

        if (gunPrefab != null)
        {
            GameObject gunInstance = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab, parent);
            // Escala del 12% (0.12) para que tenga un tamaño realista en VR
            gunInstance.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            // Alineamos la empuñadura de la pistola con el agarre del control físico (atrás y abajo)
            gunInstance.transform.localPosition = new Vector3(0f, -0.06f, -0.05f);
            gunInstance.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(gunInstance, "Create Water Gun Model");
            gunInstanceTf = gunInstance.transform;

            // Desactivamos los modelos visuales del mando para que no se superpongan con la pistola
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != gunInstanceTf && child.name != "WateringCan_FX")
                {
                    Undo.RecordObject(child.gameObject, "Hide Controller Model");
                    child.gameObject.SetActive(false);
                    Debug.Log($"[PainterFXSetup] Se ocultó el modelo del mando: {child.name}");
                }
            }
        }
        else
        {
            Debug.LogError("[PainterFXSetup] No se encontró el modelo de la pistola en Assets/nappin/WeaponStylizedPack/Prefabs/(Prb)WaterGun.prefab");
            gunInstanceTf = parent; // Fallback
        }

        // 1. Configurar el chorro de agua (Spray) ANCLADO A LA PISTOLA
        GameObject sprayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Farming_game_FX/Prefabs/Farming/WateringCan.prefab");
        if (sprayPrefab != null)
        {
            Transform existingSpray = gunInstanceTf.Find("WateringCan_FX");
            if (existingSpray != null)
            {
                Undo.DestroyObjectImmediate(existingSpray.gameObject);
            }

            GameObject sprayInstance = (GameObject)PrefabUtility.InstantiatePrefab(sprayPrefab, gunInstanceTf);
            sprayInstance.name = "WateringCan_FX";
            // Lo movemos ligeramente hacia adelante en Z asumiendo que es la punta del arma
            sprayInstance.transform.localPosition = new Vector3(0, 0, 0.3f);
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
        Debug.Log("<color=green>[PainterFXSetup] Pistola y Efectos de agua aplicados exitosamente.</color> Guarda tu escena.");
    }
}
