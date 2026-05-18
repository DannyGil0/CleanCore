using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// In-world VR settings menu: volume, stats, recenter, scene reset, help, exit.
/// </summary>
public class InWorldMenuVR : MonoBehaviour
{
    private enum PendingAction
    {
        None,
        ResetScene,
        ExitGame,
    }

    [Header("References")]
    [SerializeField] private XROrigin _xrOrigin;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private CleaningStatsAggregator _statsAggregator;
    [SerializeField] private HelpPanelController _helpPanel;

    [Header("UI")]
    [SerializeField] private Slider _sliderAmbiente;
    [SerializeField] private Slider _sliderMusica;
    [SerializeField] private TMP_Text _statsLabel;
    [SerializeField] private GameObject _modalRoot;
    [SerializeField] private TMP_Text _modalMessage;

    [Header("Modal Buttons")]
    [SerializeField] private Button _modalYesButton;
    [SerializeField] private Button _modalNoButton;

    private PendingAction _pendingAction = PendingAction.None;
    private bool _wired;
    private bool _startInitialized;

    /// <summary>
    /// Called by VRMenuFactory after building UI at runtime.
    /// </summary>
    public void WireReferences(
        Slider sliderAmbiente,
        Slider sliderMusica,
        TMP_Text statsLabel,
        GameObject modalRoot,
        TMP_Text modalMessage,
        Button modalYes,
        Button modalNo,
        AudioManager audioManager,
        CleaningStatsAggregator statsAggregator,
        HelpPanelController helpPanel,
        Button btnRecenter,
        Button btnReset,
        Button btnHelp,
        Button btnExit,
        VRMenuUIBinder binder)
    {
        _sliderAmbiente = sliderAmbiente;
        _sliderMusica = sliderMusica;
        _statsLabel = statsLabel;
        _modalRoot = modalRoot;
        _modalMessage = modalMessage;
        _modalYesButton = modalYes;
        _modalNoButton = modalNo;
        _audioManager = audioManager;
        _statsAggregator = statsAggregator;
        _helpPanel = helpPanel;

        if (binder != null)
        {
            binder.Wire(menu: this, btnRecenter, btnReset, btnHelp, btnExit);
        }

        _wired = true;
        InitializeStartLogic();
    }

    private void Awake()
    {
        if (_xrOrigin == null)
            _xrOrigin = FindFirstObjectByType<XROrigin>();

        if (_audioManager == null)
            _audioManager = FindFirstObjectByType<AudioManager>();

        if (_statsAggregator == null)
            _statsAggregator = GetComponentInChildren<CleaningStatsAggregator>();

        if (_modalRoot != null)
            _modalRoot.SetActive(false);
    }

    private void Start()
    {
        InitializeStartLogic();
    }

    void InitializeStartLogic()
    {
        if (_startInitialized)
            return;
        _startInitialized = true;

        if (!_wired && _sliderAmbiente == null)
            Debug.LogWarning("[VRMenu] Referencias UI vacias. Ejecuta Tools > VR Menu > Add To House Interior Scene.", this);

        float amb = PlayerPrefs.GetFloat(AudioManager.PrefVolAmbiente, 0.7f);
        float mus = PlayerPrefs.GetFloat(AudioManager.PrefVolMusica, 0.4f);

        if (_sliderAmbiente != null)
        {
            _sliderAmbiente.SetValueWithoutNotify(amb);
            _sliderAmbiente.onValueChanged.RemoveListener(SetVolumeAmbiente);
            _sliderAmbiente.onValueChanged.AddListener(SetVolumeAmbiente);
        }

        if (_sliderMusica != null)
        {
            _sliderMusica.SetValueWithoutNotify(mus);
            _sliderMusica.onValueChanged.RemoveListener(SetVolumeMusica);
            _sliderMusica.onValueChanged.AddListener(SetVolumeMusica);
        }

        if (_statsAggregator != null)
        {
            _statsAggregator.OnStatsUpdated -= OnStatsUpdated;
            _statsAggregator.OnStatsUpdated += OnStatsUpdated;
            _statsAggregator.RefreshStats();
        }

        if (_modalYesButton != null)
        {
            _modalYesButton.onClick.RemoveListener(OnModalYes);
            _modalYesButton.onClick.AddListener(OnModalYes);
        }
        if (_modalNoButton != null)
        {
            _modalNoButton.onClick.RemoveListener(OnModalNo);
            _modalNoButton.onClick.AddListener(OnModalNo);
        }
    }

    private void OnDestroy()
    {
        if (_statsAggregator != null)
            _statsAggregator.OnStatsUpdated -= OnStatsUpdated;
    }

    private void OnStatsUpdated(float percent)
    {
        if (_statsLabel != null)
            _statsLabel.text = $"Limpieza: {percent:F1}%";
    }

    public void SetVolumeAmbiente(float value)
    {
        if (_audioManager != null)
            _audioManager.SetAmbienteVolume(value);
    }

    public void SetVolumeMusica(float value)
    {
        if (_audioManager != null)
            _audioManager.SetMusicaVolume(value);
    }

    public void ShowStats()
    {
        _statsAggregator?.RefreshStats();
        Debug.Log("[VRMenu] Stats actualizados manualmente");
    }

    public void Recalibrate()
    {
        bool ok = false;
        var subsystems = new System.Collections.Generic.List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (XRInputSubsystem subsystem in subsystems)
        {
            if (subsystem != null && subsystem.TryRecenter())
                ok = true;
        }

        Debug.Log(ok ? "[VRMenu] Recenter OK" : "[VRMenu] Recenter fallo o no soportado en este dispositivo");
    }

    public void ResetScene()
    {
        OpenModal(PendingAction.ResetScene, "¿Reiniciar la escena? Se perderá el progreso de limpieza.");
    }

    public void ExitGame()
    {
        OpenModal(PendingAction.ExitGame, "¿Salir del juego?");
    }

    public void ToggleHelp()
    {
        if (_helpPanel == null)
            return;
        _helpPanel.Toggle();
    }

    private void OpenModal(PendingAction action, string message)
    {
        _pendingAction = action;
        if (_modalMessage != null)
            _modalMessage.text = message;
        if (_modalRoot != null)
            _modalRoot.SetActive(true);
        Debug.Log($"[VRMenu] Modal abierto: {action}");
    }

    private void OnModalYes()
    {
        PendingAction action = _pendingAction;
        CloseModal();

        switch (action)
        {
            case PendingAction.ResetScene:
                string sceneName = SceneManager.GetActiveScene().name;
                Debug.Log($"[VRMenu] Reiniciando escena: {sceneName}");
                SceneManager.LoadScene(sceneName);
                break;
            case PendingAction.ExitGame:
                Debug.Log("[VRMenu] Saliendo del juego");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private void OnModalNo()
    {
        Debug.Log("[VRMenu] Modal cancelado");
        CloseModal();
    }

    private void CloseModal()
    {
        _pendingAction = PendingAction.None;
        if (_modalRoot != null)
            _modalRoot.SetActive(false);
    }
}
