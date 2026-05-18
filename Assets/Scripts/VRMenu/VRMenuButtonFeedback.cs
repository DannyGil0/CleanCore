using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Hover/click visual feedback, UI sounds, and haptic pulse on VR menu buttons.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class VRMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Graphic _targetGraphic;
    [SerializeField] private Color _normalColor = new Color(0.85f, 0.95f, 1f, 1f);
    [SerializeField] private Color _hoverColor = new Color(0.65f, 0.88f, 0.95f, 1f);
    [SerializeField] private Color _pressedColor = new Color(0.45f, 0.78f, 0.88f, 1f);
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private HapticImpulsePlayer _leftHaptic;
    [SerializeField] private HapticImpulsePlayer _rightHaptic;
    [SerializeField] private float _hapticAmplitude = 0.3f;
    [SerializeField] private float _hapticDuration = 0.05f;

    public void Configure(Graphic targetGraphic, AudioSource audioSource)
    {
        _targetGraphic = targetGraphic;
        _audioSource = audioSource;
        ApplyColor(_normalColor);
    }

    private void Awake()
    {
        if (_targetGraphic == null)
            _targetGraphic = GetComponent<Graphic>();
        ApplyColor(_normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyColor(_hoverColor);
        PlayClip(_hoverClip);
        Debug.Log($"[VRMenu] Hover: {name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyColor(_normalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ApplyColor(_pressedColor);
        PlayClip(_clickClip);
        SendHaptics();
        Debug.Log($"[VRMenu] Click: {name}");
    }

    private void ApplyColor(Color c)
    {
        if (_targetGraphic != null)
            _targetGraphic.color = c;
    }

    private void PlayClip(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
            _audioSource.PlayOneShot(clip);
    }

    private void SendHaptics()
    {
        _leftHaptic?.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
        _rightHaptic?.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
    }
}
