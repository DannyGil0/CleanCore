using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles the VR settings menu with a controller button (default: left Menu button on Quest).
/// </summary>
public class VRMenuToggleInput : MonoBehaviour
{
    [SerializeField] private InWorldMenuVR _menu;
    [SerializeField] private InputActionReference _toggleActionReference;
    [SerializeField] private bool _allowKeyboardFallback = true;
    [SerializeField] private Key _keyboardFallbackKey = Key.M;

    InputAction _toggleAction;
    bool _ownsAction;

    void Awake()
    {
        if (_menu == null)
            _menu = GetComponent<InWorldMenuVR>();

        if (_toggleActionReference != null && _toggleActionReference.action != null)
        {
            _toggleAction = _toggleActionReference.action;
        }
        else
        {
            _toggleAction = new InputAction(
                name: "ToggleVRMenu",
                type: InputActionType.Button);
            _toggleAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            _toggleAction.AddBinding("<XRController>{RightHand}/{MenuButton}");
            _toggleAction.AddBinding("<XRController>{LeftHand}/menuButton");
            _toggleAction.AddBinding("<XRController>{RightHand}/menuButton");
            _toggleAction.AddBinding("<OculusTouchController>/start");
            if (_allowKeyboardFallback)
                _toggleAction.AddBinding($"<Keyboard>/{_keyboardFallbackKey}");
            _ownsAction = true;
        }
    }

    void OnEnable()
    {
        _toggleAction?.Enable();
    }

    void OnDisable()
    {
        _toggleAction?.Disable();
    }

    void OnDestroy()
    {
        if (_ownsAction && _toggleAction != null)
            _toggleAction.Dispose();
    }

    void Update()
    {
        if (_menu == null)
            return;

        if (!WasTogglePressed())
            return;

        _menu.ToggleMenuVisibility();
    }

    bool WasTogglePressed()
    {
        if (_toggleAction != null && _toggleAction.WasPerformedThisFrame())
            return true;

        if (!_allowKeyboardFallback)
            return false;

        if (Keyboard.current != null && Keyboard.current[_keyboardFallbackKey].wasPressedThisFrame)
            return true;

        // Legacy input works in Editor even when Keyboard.current is null (no Game view focus).
        return Input.GetKeyDown(KeyCode.M);
    }
}
