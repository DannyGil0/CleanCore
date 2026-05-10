using UnityEngine;
using UnityEngine.InputSystem;

public class Painter : MonoBehaviour
{
    private static readonly Collider[] COLLIDERS = new Collider[100];

    public float SpreadRadius = 0.1f;
    public float Range = 10;
    public LayerMask LayerMask;

    [Header("VR Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("VR Input")]
    public InputActionReference fire1Action;
    public InputActionReference fire2Action;

    private Transform _transform;

    private void Awake()
    {
        _transform = this.transform;
    }

    private void OnEnable()
    {
        fire1Action?.action.Enable();
        fire2Action?.action.Enable();
    }

    private void OnDisable()
    {
        fire1Action?.action.Disable();
        fire2Action?.action.Disable();
    }

    private void Update()
    {
        bool fire1 = fire1Action != null && fire1Action.action.IsPressed();
        bool fire2 = fire2Action != null && fire2Action.action.IsPressed();

        if (!fire1) fire1 = Input.GetButton("Fire1");
        if (!fire2) fire2 = Input.GetButton("Fire2");

        if (!fire1 && !fire2)
            return;

        // Usa el controlador correspondiente o la cámara como fallback
        Transform origin = fire1
            ? (rightController != null ? rightController : _transform)
            : (leftController != null ? leftController : _transform);

        origin.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        if (!Physics.Raycast(position, rotation * Vector3.forward, out RaycastHit hit, this.Range, this.LayerMask))
            return;

        int hits = Physics.OverlapSphereNonAlloc(hit.point, this.SpreadRadius, COLLIDERS, this.LayerMask);
        for (int i = 0; i < hits; i++)
        {
            Collider col = COLLIDERS[i];
            if (!col.TryGetComponent(out PaintableSurface surface))
                continue;
            Vector3 normal = rotation * Vector3.back;
            Color color = fire2 ? Color.white : Color.black;
            surface.Paint(hit.point, normal, this.SpreadRadius, color);
        }
    }
}
