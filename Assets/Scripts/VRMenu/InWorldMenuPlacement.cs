using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Places the menu once at Start relative to XROrigin (not camera-locked).
/// </summary>
public class InWorldMenuPlacement : MonoBehaviour
{
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 1.4f, 2f);
    [SerializeField] private XROrigin _xrOrigin;

    private void Start()
    {
        StartCoroutine(PlaceWhenReady());
    }

    IEnumerator PlaceWhenReady()
    {
        // Wait for XR Origin / camera to initialize (especially in VR play mode).
        for (int i = 0; i < 10; i++)
        {
            XROrigin origin = _xrOrigin != null ? _xrOrigin : FindFirstObjectByType<XROrigin>();
            Camera cam = origin != null ? origin.Camera : Camera.main;

            if (origin != null && cam != null)
            {
                PlaceMenu(origin, cam);
                yield break;
            }

            yield return null;
        }

        Camera fallbackCam = Camera.main;
        if (fallbackCam != null)
        {
            Transform camT = fallbackCam.transform;
            Vector3 worldPos = camT.position + camT.forward * 2f + Vector3.up * 0.2f;
            Vector3 toPlayer = camT.position - worldPos;
            toPlayer.y = 0f;
            transform.SetPositionAndRotation(worldPos, Quaternion.LookRotation(toPlayer.normalized, Vector3.up));
            Debug.Log($"[VRMenu] Menu colocado frente a Camera.main en {worldPos}");
        }
        else
        {
            Debug.LogWarning("[VRMenu] XROrigin/camara no listos; menu en posicion por defecto.", this);
        }
    }

    void PlaceMenu(XROrigin origin, Camera cam)
    {
        Transform originTransform = origin.transform;
        Vector3 worldPos = originTransform.TransformPoint(_localOffset);

        Vector3 toPlayer = cam.transform.position - worldPos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f)
            toPlayer = originTransform.forward;

        transform.SetPositionAndRotation(worldPos, Quaternion.LookRotation(toPlayer.normalized, Vector3.up));
        Debug.Log($"[VRMenu] Menu colocado en {worldPos} mirando al jugador.");
    }
}
