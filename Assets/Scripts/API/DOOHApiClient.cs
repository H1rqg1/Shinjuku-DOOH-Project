using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class DOOHApiClient
{
    private const float DefaultTimeoutSeconds = 10f;
    private readonly DOOHServerConfig serverConfig;

    public DOOHApiClient(DOOHServerConfig serverConfig)
    {
        this.serverConfig = serverConfig;
    }

    public string EncountersUrl => BuildEndpointUrl(serverConfig.BaseUrl, serverConfig.EncountersPath);

    public IEnumerator GetEncounters(Action<Encounter[]> onSuccess, Action<string> onError)
    {
        string url = EncountersUrl;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            float configuredTimeout = serverConfig.TimeoutSeconds > 0f
                ? serverConfig.TimeoutSeconds
                : DefaultTimeoutSeconds;
            request.timeout = Mathf.CeilToInt(configuredTimeout);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = string.IsNullOrWhiteSpace(request.error)
                    ? "Unknown request error"
                    : request.error;
                string message =
                    $"[DOOH API] Request failed\n" +
                    $"URL: {url}\n" +
                    $"HTTP Status: {request.responseCode}\n" +
                    $"Error: {error}";
                Debug.LogWarning(message);
                onError?.Invoke(message);
                yield break;
            }

            if (!TryParseEncounters(request.downloadHandler.text, out Encounter[] encounters, out string parseError))
            {
                string message =
                    $"[DOOH API] Response parse failed\n" +
                    $"URL: {url}\n" +
                    $"HTTP Status: {request.responseCode}\n" +
                    $"Error: {parseError}";
                Debug.LogWarning(message);
                onError?.Invoke(message);
                yield break;
            }

            onSuccess?.Invoke(encounters);
        }
    }

    public IEnumerator GetStats(Action<DOOHStatsResponse> onSuccess, Action<string> onError)
    {
        string url = BuildEndpointUrl(serverConfig.BaseUrl, "/stats");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            float configuredTimeout = serverConfig.TimeoutSeconds > 0f
                ? serverConfig.TimeoutSeconds
                : DefaultTimeoutSeconds;
            request.timeout = Mathf.CeilToInt(configuredTimeout);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = string.IsNullOrWhiteSpace(request.error)
                    ? "Unknown request error"
                    : request.error;
                string message =
                    $"[DOOH API] Request failed\n" +
                    $"URL: {url}\n" +
                    $"HTTP Status: {request.responseCode}\n" +
                    $"Error: {error}";
                Debug.LogWarning(message);
                onError?.Invoke(message);
                yield break;
            }

            try
            {
                DOOHStatsResponse stats = JsonUtility.FromJson<DOOHStatsResponse>(request.downloadHandler.text);
                if (stats == null)
                {
                    throw new FormatException("The response body is not a stats object.");
                }

                onSuccess?.Invoke(stats);
            }
            catch (Exception exception)
            {
                string message =
                    $"[DOOH API] Response parse failed\n" +
                    $"URL: {url}\n" +
                    $"HTTP Status: {request.responseCode}\n" +
                    $"Error: {exception.Message}";
                Debug.LogWarning(message);
                onError?.Invoke(message);
            }
        }
    }

    public static string BuildEndpointUrl(string baseUrl, string endpointPath)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(endpointPath))
        {
            return string.Empty;
        }

        string normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
        string normalizedPath = endpointPath.Trim();
        normalizedPath = normalizedPath.StartsWith("/", StringComparison.Ordinal)
            ? normalizedPath
            : "/" + normalizedPath;

        if (normalizedBaseUrl.EndsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedBaseUrl;
        }

        return normalizedBaseUrl + normalizedPath;
    }

    private static bool TryParseEncounters(string json, out Encounter[] encounters, out string error)
    {
        encounters = Array.Empty<Encounter>();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The response body is empty.";
            return false;
        }

        try
        {
            string normalizedJson = json.Trim();
            if (normalizedJson.StartsWith("[", StringComparison.Ordinal))
            {
                normalizedJson = $"{{\"encounters\":{normalizedJson}}}";
            }

            EncounterList response = JsonUtility.FromJson<EncounterList>(normalizedJson);
            if (response == null || response.encounters == null)
            {
                error = "The response does not contain an encounters array.";
                return false;
            }

            encounters = response.encounters;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }
}

[Serializable]
public sealed class DOOHStatsResponse
{
    public string date_jst = "";
    public string time_jst = "";
    public int daily_detected_count;
    public int daily_encounter_count;
}
