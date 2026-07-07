using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class DOOHStatusDisplay : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverUrl = "http://127.0.0.1:8000";
    [SerializeField] private float updateIntervalSeconds = 5f;

    [Header("Text")]
    [SerializeField] private TMP_Text detectedCountText;
    [SerializeField] private TMP_Text currentTimeText;
    [SerializeField] private string countLabel = "本日の検出人数";
    [SerializeField] private string timeLabel = "現在時刻";

    private Coroutine pollingRoutine;
    private bool hasWarnedMissingDetectedText;
    private bool hasWarnedMissingTimeText;

    private void OnEnable()
    {
        pollingRoutine = StartCoroutine(PollStats());
    }

    private void OnDisable()
    {
        if (pollingRoutine != null)
        {
            StopCoroutine(pollingRoutine);
            pollingRoutine = null;
        }
    }

    private IEnumerator PollStats()
    {
        while (enabled)
        {
            yield return FetchStats();

            float waitSeconds = Mathf.Max(1f, updateIntervalSeconds);
            yield return new WaitForSeconds(waitSeconds);
        }
    }

    private IEnumerator FetchStats()
    {
        string statsUrl = BuildStatsUrl();

        using (UnityWebRequest request = UnityWebRequest.Get(statsUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning($"DOOH stats request failed: {request.error} ({statsUrl})");
                yield break;
            }

            StatsResponse stats = JsonUtility.FromJson<StatsResponse>(request.downloadHandler.text);
            if (stats == null)
            {
                Debug.LogWarning($"DOOH stats parse failed: {request.downloadHandler.text}");
                yield break;
            }

            ApplyStats(stats);
        }
    }

    private string BuildStatsUrl()
    {
        string normalizedUrl = string.IsNullOrWhiteSpace(serverUrl)
            ? "http://127.0.0.1:8000"
            : serverUrl.TrimEnd('/');

        if (normalizedUrl.EndsWith("/stats", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedUrl;
        }

        return $"{normalizedUrl}/stats";
    }

    private void ApplyStats(StatsResponse stats)
    {
        if (detectedCountText != null)
        {
            detectedCountText.text = $"{countLabel}：{stats.daily_detected_count}人";
        }
        else if (!hasWarnedMissingDetectedText)
        {
            Debug.LogWarning("DetectedCountText is not assigned.");
            hasWarnedMissingDetectedText = true;
        }

        if (currentTimeText != null)
        {
            currentTimeText.text = $"{timeLabel}：{stats.time_jst}";
        }
        else if (!hasWarnedMissingTimeText)
        {
            Debug.LogWarning("CurrentTimeText is not assigned.");
            hasWarnedMissingTimeText = true;
        }
    }

    [Serializable]
    private class StatsResponse
    {
        public string date_jst = "";
        public string time_jst = "";
        public int daily_detected_count = 0;
        public int daily_encounter_count = 0;
    }
}
