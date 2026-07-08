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
    [SerializeField] private string countLabel = "Today's count in Shinjuku";
    [SerializeField] private string timeLabel = "Time";

    private Coroutine pollingRoutine;
    private bool hasWarnedMissingDetectedText;
    private bool hasWarnedMissingTimeText;

    private void Awake()
    {
        ConfigureText(detectedCountText);
        ConfigureText(currentTimeText);
        ShowDefaultStats();
    }

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
            detectedCountText.text = $"{countLabel}: {stats.daily_detected_count}";
        }
        else if (!hasWarnedMissingDetectedText)
        {
            Debug.LogWarning("DetectedCountText is not assigned.");
            hasWarnedMissingDetectedText = true;
        }

        if (currentTimeText != null)
        {
            string timeText = string.IsNullOrWhiteSpace(stats.time_jst) ? "--:--" : stats.time_jst;
            currentTimeText.text = $"{timeLabel}: {timeText}";
        }
        else if (!hasWarnedMissingTimeText)
        {
            Debug.LogWarning("CurrentTimeText is not assigned.");
            hasWarnedMissingTimeText = true;
        }
    }

    private void ShowDefaultStats()
    {
        if (detectedCountText != null)
        {
            detectedCountText.text = $"{countLabel}: 0";
        }

        if (currentTimeText != null)
        {
            currentTimeText.text = $"{timeLabel}: --:--";
        }
    }

    private static void ConfigureText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.enableAutoSizing = true;
        text.fontSizeMax = Mathf.Max(text.fontSizeMax, text.fontSize);
        text.fontSizeMin = Mathf.Min(text.fontSizeMin, 32f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
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
