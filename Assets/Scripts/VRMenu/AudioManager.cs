using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Controls Ambiente/Musica mixer groups. Sliders use linear 0-1; mixer uses dB.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public const string PrefVolAmbiente = "VolAmbiente";
    public const string PrefVolMusica = "VolMusica";
    public const string ParamVolAmbiente = "VolAmbiente";
    public const string ParamVolMusica = "VolMusica";

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private bool _persistAcrossScenes;

    public float AmbienteVolume { get; private set; } = 0.7f;
    public float MusicaVolume { get; private set; } = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        AmbienteVolume = PlayerPrefs.GetFloat(PrefVolAmbiente, 0.7f);
        MusicaVolume = PlayerPrefs.GetFloat(PrefVolMusica, 0.4f);
        ApplyAmbienteVolume(AmbienteVolume);
        ApplyMusicaVolume(MusicaVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetAmbienteVolume(float linear01)
    {
        AmbienteVolume = Mathf.Clamp01(linear01);
        ApplyAmbienteVolume(AmbienteVolume);
        PlayerPrefs.SetFloat(PrefVolAmbiente, AmbienteVolume);
        PlayerPrefs.Save();
        Debug.Log($"[VRMenu] VolAmbiente={AmbienteVolume:F2}");
    }

    public void SetMusicaVolume(float linear01)
    {
        MusicaVolume = Mathf.Clamp01(linear01);
        ApplyMusicaVolume(MusicaVolume);
        PlayerPrefs.SetFloat(PrefVolMusica, MusicaVolume);
        PlayerPrefs.Save();
        Debug.Log($"[VRMenu] VolMusica={MusicaVolume:F2}");
    }

    private void ApplyAmbienteVolume(float linear01)
    {
        SetMixerVolume(ParamVolAmbiente, linear01);
    }

    private void ApplyMusicaVolume(float linear01)
    {
        SetMixerVolume(ParamVolMusica, linear01);
    }

    private void SetMixerVolume(string param, float linear01)
    {
        if (_mixer == null)
            return;

        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        _mixer.SetFloat(param, db);
    }
}
