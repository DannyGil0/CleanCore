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

    /// <summary>Repositions the menu in front of the player (e.g. when opening the menu).</summary>
    public void TryPlaceNow()
    {
        XROrigin origin = _xrOrigin != null ? _xrOrigin : FindFirstObjectByType<XROrigin>();
        Camera cam = origin != null ? origin.Camera : Camera.main;

        if (origin != null && cam != null)
            PlaceMenu(origin, cam);
        else if (cam != null)
            PlaceFacingCamera(transform, cam.transform.position + cam.transform.forward * 2f + Vector3.up * 0.2f,
                cam.transform.position);
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
            PlaceFacingCamera(transform,
                fallbackCam.transform.position + fallbackCam.transform.forward * 2f + Vector3.up * 0.2f,
                fallbackCam.transform.position);
            Debug.Log("[VRMenu] Menu colocado frente a Camera.main");
        }
        else
        {
            Debug.LogWarning("[VRMenu] XROrigin/camara no listos; menu en posicion por defecto.", this);
        }
    }

    void PlaceMenu(XROrigin origin, Camera cam)
    {
        Vector3 worldPos = origin.transform.TransformPoint(_localOffset);
        PlaceFacingCamera(transform, worldPos, cam.transform.position);
        Debug.Log($"[VRMenu] Menu colocado en {worldPos} mirando al jugador.");
    }

    static void PlaceFacingCamera(Transform root, Vector3 worldPos, Vector3 cameraPosition)
    {
        if (root == null)
            return;

        Vector3 flatToCamera = cameraPosition - worldPos;
        flatToCamera.y = 0f;
        if (flatToCamera.sqrMagnitude < 0.001f)
            flatToCamera = Vector3.forward;

        // World-space UI is drawn on -local Z; root forward should point away from the camera.
        root.SetPositionAndRotation(worldPos, Quaternion.LookRotation(-flatToCamera.normalized, Vector3.up));

        Transform canvas = root.Find("MenuCanvas");
        if (canvas != null)
            canvas.localRotation = Quaternion.identity;
    }
}
