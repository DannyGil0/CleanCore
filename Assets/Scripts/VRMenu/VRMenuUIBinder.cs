using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires menu buttons to InWorldMenuVR at runtime (prefab-safe).
/// </summary>
public class VRMenuUIBinder : MonoBehaviour
{
    [SerializeField] private InWorldMenuVR _menu;
    [SerializeField] private Button _btnRecenter;
    [SerializeField] private Button _btnReset;
    [SerializeField] private Button _btnHelp;
    [SerializeField] private Button _btnExit;
    [SerializeField] private Button _btnClose;

    public void Wire(InWorldMenuVR menu, Button btnRecenter, Button btnReset, Button btnHelp, Button btnExit,
        Button btnClose)
    {
        _menu = menu;
        _btnRecenter = btnRecenter;
        _btnReset = btnReset;
        _btnHelp = btnHelp;
        _btnExit = btnExit;
        _btnClose = btnClose;
        Bind();
    }

    private void Awake()
    {
        if (_menu == null)
            _menu = GetComponentInParent<InWorldMenuVR>();
        Bind();
    }

    public void Bind()
    {
        if (_menu == null)
            return;

        if (_btnRecenter != null)
        {
            _btnRecenter.onClick.RemoveListener(_menu.Recalibrate);
            _btnRecenter.onClick.AddListener(_menu.Recalibrate);
        }
        if (_btnReset != null)
        {
            _btnReset.onClick.RemoveListener(_menu.ResetScene);
            _btnReset.onClick.AddListener(_menu.ResetScene);
        }
        if (_btnHelp != null)
        {
            _btnHelp.onClick.RemoveListener(_menu.ToggleHelp);
            _btnHelp.onClick.AddListener(_menu.ToggleHelp);
        }
        if (_btnExit != null)
        {
            _btnExit.onClick.RemoveListener(_menu.ExitGame);
            _btnExit.onClick.AddListener(_menu.ExitGame);
        }
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(_menu.CloseMenu);
            _btnClose.onClick.AddListener(_menu.CloseMenu);
        }
    }
}
