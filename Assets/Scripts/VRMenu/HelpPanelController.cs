using TMPro;
using UnityEngine;

/// <summary>
/// Toggleable help overlay for basic VR / cleaning controls.
/// </summary>
public class HelpPanelController : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_Text _bodyText;

    private const string DefaultHelpText =
        "CONTROLES\n\n" +
        "• Gatillo derecho (Fire1): limpiar superficies\n" +
        "• Gatillo izquierdo (Fire2): ensuciar\n" +
        "• Ray del mando: interactuar con este menú\n" +
        "• Joystick / stick: moverte (según rig XR)\n\n" +
        "MENÚ\n\n" +
        "• Botón Menu del mando izquierdo (Quest): abrir/cerrar menú\n" +
        "• Tecla M (PC): abrir/cerrar menú\n" +
        "• CERRAR: ocultar menú\n" +
        "• Sliders: volumen ambiente y música\n" +
        "• Recenter: recentra la vista VR\n" +
        "• Reiniciar: recarga la escena actual";

    public void Configure(GameObject panelRoot, TMP_Text bodyText)
    {
        _panelRoot = panelRoot;
        _bodyText = bodyText;
        if (_bodyText != null && string.IsNullOrWhiteSpace(_bodyText.text))
            _bodyText.text = DefaultHelpText;
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void Awake()
    {
        if (_panelRoot == null || _bodyText == null)
            return;

        if (_bodyText != null && string.IsNullOrWhiteSpace(_bodyText.text))
            _bodyText.text = DefaultHelpText;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);
        Debug.Log("[VRMenu] Ayuda abierta");
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
        Debug.Log("[VRMenu] Ayuda cerrada");
    }

    public void Toggle()
    {
        if (_panelRoot == null)
            return;

        bool show = !_panelRoot.activeSelf;
        _panelRoot.SetActive(show);
        Debug.Log(show ? "[VRMenu] Ayuda abierta" : "[VRMenu] Ayuda cerrada");
    }

    public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;
}
