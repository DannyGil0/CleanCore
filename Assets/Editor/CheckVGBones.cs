using UnityEditor;
using UnityEngine;

public static class CheckVGBones
{
    [MenuItem("Tools/Check VG Bones")]
    public static void Check()
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/com.gleechi.unity.virtualgrasp/Runtime/Resources/GleechiHands/GleechiLeftHand.fbx");
        if (fbx == null)
        {
            Debug.LogError("No FBX found");
            return;
        }
        var go = Object.Instantiate(fbx);
        var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && smr.bones != null)
        {
            foreach (var b in smr.bones)
            {
                Debug.Log(b.name);
            }
        }
        else
        {
            Debug.Log("No SMR or bones");
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                Debug.Log("Transform: " + t.name);
            }
        }
        Object.DestroyImmediate(go);
    }
}