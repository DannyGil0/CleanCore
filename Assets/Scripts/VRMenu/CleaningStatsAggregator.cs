using System;
using UnityEngine;

/// <summary>
/// Computes global cleanliness % from PaintableSurfaceUI surface list.
/// </summary>
public class CleaningStatsAggregator : MonoBehaviour
{
    public event Action<float> OnStatsUpdated;

    [SerializeField] private PaintableSurfaceUI _surfaceUI;
    [SerializeField] private float _refreshInterval = 0.5f;

    public float GlobalCleanlinessPercent { get; private set; }

    private float _nextRefreshTime;

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + _refreshInterval;
        RefreshStats();
    }

    public void RefreshStats()
    {
        PaintableSurface[] surfaces = _surfaceUI != null ? _surfaceUI.Surfaces : null;
        if (surfaces == null || surfaces.Length == 0)
        {
            GlobalCleanlinessPercent = 0f;
            OnStatsUpdated?.Invoke(GlobalCleanlinessPercent);
            return;
        }

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] == null)
                continue;
            sum += surfaces[i].Cleanliness;
            count++;
        }

        GlobalCleanlinessPercent = count > 0 ? (sum / count) * 100f : 0f;
        OnStatsUpdated?.Invoke(GlobalCleanlinessPercent);
    }
}
