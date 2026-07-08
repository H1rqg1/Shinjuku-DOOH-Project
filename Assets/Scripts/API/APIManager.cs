using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class APIManager : MonoBehaviour
{
    [Header("Server")]
    [FormerlySerializedAs("url")]
    [SerializeField] private string serverUrl = "http://127.0.0.1:8000";
    [FormerlySerializedAs("fetchInterval")]
    [SerializeField] private float fetchIntervalSeconds = 1f;
    [SerializeField] private bool showExistingDataOnStart = true;

    [Header("Avatar Spawning")]
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private float avatarStaySeconds = 10f;
    [SerializeField] private int maxAvatarsOnScreen = 30;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(12f, 5f, 4f);
    [SerializeField] private float spawnHeight = 0f;

    [Header("CPU Fallback")]
    [SerializeField] private bool showCpuAvatarsWhenEmpty = true;
    [SerializeField] private int cpuAvatarCount = 5;
    [SerializeField] private float cpuAvatarStaySeconds = 30f;
    [SerializeField] private string cpuAvatarIdPrefix = "CPU";

    public event Action<Encounter[]> OnEncountersReceived;

    public bool HandlesAvatarSpawning => avatarPrefab != null && characterRoot != null;

    private readonly HashSet<string> displayedEncounterKeys = new HashSet<string>();
    private readonly Queue<GameObject> activeAvatars = new Queue<GameObject>();
    private readonly List<GameObject> activeUserAvatars = new List<GameObject>();
    private readonly List<GameObject> activeCpuAvatars = new List<GameObject>();
    private bool hasCompletedInitialFetch;
    private Coroutine pollingRoutine;

    private void Awake()
    {
        if (characterRoot == null)
        {
            GameObject root = GameObject.Find("CharacterRoot");
            if (root != null)
            {
                characterRoot = root.transform;
            }
        }
    }

    private void OnEnable()
    {
        pollingRoutine = StartCoroutine(PollEncounters());
    }

    private void OnDisable()
    {
        if (pollingRoutine != null)
        {
            StopCoroutine(pollingRoutine);
            pollingRoutine = null;
        }
    }

    private IEnumerator PollEncounters()
    {
        while (enabled)
        {
            yield return FetchEncounters();

            float waitSeconds = Mathf.Max(0.1f, fetchIntervalSeconds);
            yield return new WaitForSeconds(waitSeconds);
        }
    }

    private IEnumerator FetchEncounters()
    {
        string encountersUrl = BuildEncountersUrl();

        using (UnityWebRequest request = UnityWebRequest.Get(encountersUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning($"Encounter API request failed: {request.error} ({encountersUrl})");
                yield break;
            }

            EncounterList encounterList = JsonUtility.FromJson<EncounterList>(request.downloadHandler.text);
            Encounter[] encounters = encounterList != null && encounterList.encounters != null
                ? encounterList.encounters
                : new Encounter[0];

            OnEncountersReceived?.Invoke(encounters);
            HandleEncounters(encounters);
        }
    }

    private string BuildEncountersUrl()
    {
        string normalizedUrl = string.IsNullOrWhiteSpace(serverUrl)
            ? "http://127.0.0.1:8000"
            : serverUrl.TrimEnd('/');

        if (normalizedUrl.EndsWith("/encounters", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedUrl;
        }

        if (normalizedUrl.EndsWith("/encounter", StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalizedUrl}s";
        }

        return $"{normalizedUrl}/encounters";
    }

    private void HandleEncounters(Encounter[] encounters)
    {
        if (encounters == null)
        {
            return;
        }

        CleanupDestroyedAvatars();

        bool shouldSpawnInitialData = showExistingDataOnStart || hasCompletedInitialFetch;

        foreach (Encounter encounter in encounters)
        {
            if (encounter == null)
            {
                continue;
            }

            string key = encounter.GetDisplayKey();
            if (!displayedEncounterKeys.Add(key))
            {
                continue;
            }

            if (shouldSpawnInitialData)
            {
                SpawnAvatar(encounter, false, avatarStaySeconds);
            }
        }

        if (showCpuAvatarsWhenEmpty && !HasActiveUserAvatars())
        {
            EnsureCpuAvatars();
        }

        hasCompletedInitialFetch = true;
    }

    private void SpawnAvatar(Encounter encounter, bool isCpuAvatar, float displaySeconds)
    {
        if (avatarPrefab == null || characterRoot == null)
        {
            return;
        }

        CleanupDestroyedAvatars();

        if (!isCpuAvatar)
        {
            ClearCpuAvatars();
        }

        GameObject avatarObject = Instantiate(avatarPrefab, characterRoot);
        avatarObject.transform.localPosition = GetRandomSpawnPosition();
        avatarObject.name = $"Avatar_{SafeName(encounter.target_id)}";

        AvatarView avatarView = avatarObject.GetComponent<AvatarView>();
        if (avatarView == null)
        {
            avatarView = avatarObject.AddComponent<AvatarView>();
        }

        avatarView.Initialize(encounter, displaySeconds);

        if (avatarObject.GetComponent<BillboardToCamera>() == null)
        {
            avatarObject.AddComponent<BillboardToCamera>();
        }

        CrowdAvatar crowdAvatar = avatarObject.GetComponent<CrowdAvatar>();
        if (crowdAvatar != null)
        {
            crowdAvatar.SetPlayerName(encounter.target_id);
        }

        activeAvatars.Enqueue(avatarObject);
        if (isCpuAvatar)
        {
            activeCpuAvatars.Add(avatarObject);
        }
        else
        {
            activeUserAvatars.Add(avatarObject);
        }

        TrimAvatarCount();
    }

    private void EnsureCpuAvatars()
    {
        if (avatarPrefab == null || characterRoot == null)
        {
            return;
        }

        CleanupDestroyedAvatars();

        int targetCount = Mathf.Clamp(cpuAvatarCount, 0, Mathf.Max(1, maxAvatarsOnScreen));
        while (activeCpuAvatars.Count < targetCount)
        {
            int cpuIndex = activeCpuAvatars.Count + 1;
            SpawnAvatar(CreateCpuEncounter(cpuIndex), true, cpuAvatarStaySeconds);
        }
    }

    private Encounter CreateCpuEncounter(int index)
    {
        string id = $"{cpuAvatarIdPrefix}_{index:00}";
        return new Encounter
        {
            my_id = "cpu",
            target_id = id,
            timestamp = $"cpu_{Time.frameCount}_{index}"
        };
    }

    private bool HasActiveUserAvatars()
    {
        CleanupDestroyedAvatars();
        return activeUserAvatars.Count > 0;
    }

    private void ClearCpuAvatars()
    {
        for (int i = 0; i < activeCpuAvatars.Count; i++)
        {
            if (activeCpuAvatars[i] != null)
            {
                Destroy(activeCpuAvatars[i]);
            }
        }

        activeCpuAvatars.Clear();
        CleanupDestroyedAvatars();
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 halfSize = spawnAreaSize * 0.5f;

        return spawnCenter + new Vector3(
            UnityEngine.Random.Range(-halfSize.x, halfSize.x),
            spawnHeight,
            UnityEngine.Random.Range(-halfSize.z, halfSize.z)
        );
    }

    private void TrimAvatarCount()
    {
        int limit = Mathf.Max(1, maxAvatarsOnScreen);

        while (activeAvatars.Count > limit)
        {
            GameObject oldestAvatar = activeAvatars.Dequeue();
            if (oldestAvatar != null)
            {
                Destroy(oldestAvatar);
            }
        }
    }

    private void CleanupDestroyedAvatars()
    {
        int count = activeAvatars.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject avatarObject = activeAvatars.Dequeue();
            if (avatarObject != null)
            {
                activeAvatars.Enqueue(avatarObject);
            }
        }

        RemoveDestroyed(activeUserAvatars);
        RemoveDestroyed(activeCpuAvatars);
    }

    private static void RemoveDestroyed(List<GameObject> avatars)
    {
        for (int i = avatars.Count - 1; i >= 0; i--)
        {
            if (avatars[i] == null)
            {
                avatars.RemoveAt(i);
            }
        }
    }

    private static string SafeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
