using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Keeps a world-space menu canvas assigned to the active XR / main camera.
/// </summary>
[DisallowMultipleComponent]
public class VRMenuWorldCanvasDriver : MonoBehaviour
{
    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    void OnEnable()
    {
        RefreshCamera();
    }

    void LateUpdate()
    {
        RefreshCamera();
    }

    public void RefreshCamera()
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();

        if (_canvas == null || _canvas.renderMode != RenderMode.WorldSpace)
            return;

        Camera cam = ResolveViewCamera();
        if (cam != null)
            _canvas.worldCamera = cam;
    }

    public static void RefreshCamera(GameObject canvasRoot)
    {
        if (canvasRoot == null)
            return;

        var driver = canvasRoot.GetComponent<VRMenuWorldCanvasDriver>();
        if (driver == null)
            driver = canvasRoot.AddComponent<VRMenuWorldCanvasDriver>();
        else
            driver.RefreshCamera();
    }

    static Camera ResolveViewCamera()
    {
        XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin != null && origin.Camera != null)
            return origin.Camera;

        Camera main = Camera.main;
        if (main != null)
            return main;

        return Object.FindFirstObjectByType<Camera>();
    }
}
