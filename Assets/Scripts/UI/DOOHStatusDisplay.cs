using System.Collections;
using TMPro;
using UnityEngine;

public class DOOHStatusDisplay : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private DOOHServerConfig serverConfig;

    [Header("Text")]
    [SerializeField] private TMP_Text detectedCountText;
    [SerializeField] private TMP_Text currentTimeText;
    [SerializeField] private TMP_FontAsset japaneseFontAsset;
    [SerializeField] private string countLabel = "今日の新宿の人数";
    [SerializeField] private string timeLabel = "現在時刻";

    private Coroutine pollingRoutine;
    private bool hasWarnedMissingDetectedText;
    private bool hasWarnedMissingTimeText;
    private DOOHApiClient apiClient;

    private void Awake()
    {
        ConfigureText(detectedCountText);
        ConfigureText(currentTimeText);
        ShowDefaultStats();
    }

    private void OnEnable()
    {
        if (serverConfig == null || string.IsNullOrWhiteSpace(serverConfig.BaseUrl))
        {
            Debug.LogError(
                "[DOOH API] Server configuration is missing.\n" +
                "Please assign DOOHServerConfig to DOOHStatusDisplay.",
                this);
            return;
        }

        apiClient = new DOOHApiClient(serverConfig);
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

            float waitSeconds = Mathf.Max(1f, serverConfig.PollingIntervalSeconds);
            yield return new WaitForSeconds(waitSeconds);
        }
    }

    private IEnumerator FetchStats()
    {
        yield return apiClient.GetStats(ApplyStats, _ => { });
    }

    private void ApplyStats(DOOHStatsResponse stats)
    {
        if (detectedCountText != null)
        {
            detectedCountText.text = $"{countLabel}：{stats.daily_detected_count}";
        }
        else if (!hasWarnedMissingDetectedText)
        {
            Debug.LogWarning("DetectedCountText is not assigned.");
            hasWarnedMissingDetectedText = true;
        }

        if (currentTimeText != null)
        {
            string timeText = string.IsNullOrWhiteSpace(stats.time_jst) ? "--:--" : stats.time_jst;
            currentTimeText.text = $"{timeLabel}：{timeText}";
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
            detectedCountText.text = $"{countLabel}：0";
        }

        if (currentTimeText != null)
        {
            currentTimeText.text = $"{timeLabel}：--:--";
        }
    }

    private void ConfigureText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (japaneseFontAsset != null)
        {
            text.font = japaneseFontAsset;
        }

        text.enableAutoSizing = true;
        text.fontSizeMax = Mathf.Max(text.fontSizeMax, text.fontSize);
        text.fontSizeMin = Mathf.Min(text.fontSizeMin, 32f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
    }

}
