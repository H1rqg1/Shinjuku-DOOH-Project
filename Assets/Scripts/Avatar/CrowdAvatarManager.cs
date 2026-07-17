using System.Collections.Generic;
using UnityEngine;

public class CrowdAvatarManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private APIManager apiManager;
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarCatalog avatarCatalog;

    [Header("Display")]
    [SerializeField] private int maxAvatarCount = 30;
    [SerializeField, Min(1f)] private float userAvatarStaySeconds = 600f;

    [Header("CPU Fallback")]
    [SerializeField] private bool showCpuAvatarsWhenEmpty = true;
    [SerializeField] private int cpuAvatarCount = 5;
    [SerializeField] private float cpuAvatarStaySeconds = 30f;
    [SerializeField] private string cpuAvatarIdPrefix = "CPU";

    private sealed class ActiveUserAvatar
    {
        public GameObject Object;
        public string SavedAt;
    }

    private static readonly System.TimeSpan JstOffset = System.TimeSpan.FromHours(9);
    private readonly Dictionary<string, ActiveUserAvatar> activeUserAvatarsById = new Dictionary<string, ActiveUserAvatar>();
    private readonly Queue<GameObject> activeAvatarsQueue = new Queue<GameObject>();
    private readonly List<GameObject> activeUserAvatars = new List<GameObject>();
    private readonly List<GameObject> activeCpuAvatars = new List<GameObject>();
    private bool hasWarnedMissingCatalog;

    public int CurrentAvatarCount => activeAvatarsQueue.Count;
    public int MaxAvatarCount => maxAvatarCount;

    private void OnEnable()
    {
        if (apiManager != null && !apiManager.HandlesAvatarSpawning)
        {
            apiManager.OnEncountersReceived += HandleEncountersReceived;
        }
    }

    private void OnDisable()
    {
        if (apiManager != null)
        {
            apiManager.OnEncountersReceived -= HandleEncountersReceived;
        }
    }

    private void HandleEncountersReceived(Encounter[] encounters)
    {
        if (encounters == null)
        {
            return;
        }

        CleanupDestroyedAvatars();

        HashSet<string> receivedUserIds = new HashSet<string>();

        foreach (Encounter encounter in encounters)
        {
            if (encounter == null || !IsCurrentJstDate(encounter.timestamp))
            {
                continue;
            }

            string userId = NormalizeUserId(encounter.my_id);
            if (string.IsNullOrEmpty(userId) || !receivedUserIds.Add(userId))
            {
                continue;
            }

            if (activeUserAvatarsById.TryGetValue(userId, out ActiveUserAvatar activeAvatar) &&
                activeAvatar.Object != null)
            {
                if (!string.Equals(activeAvatar.SavedAt, encounter.timestamp, System.StringComparison.Ordinal))
                {
                    ConfigureUserAvatar(activeAvatar.Object, encounter, userAvatarStaySeconds);
                    activeAvatar.SavedAt = encounter.timestamp;
                }

                continue;
            }

            GameObject avatarObject = SpawnAvatar(encounter, false, userAvatarStaySeconds);
            if (avatarObject != null)
            {
                activeUserAvatarsById[userId] = new ActiveUserAvatar
                {
                    Object = avatarObject,
                    SavedAt = encounter.timestamp
                };
            }
        }

        RemoveUserAvatarsMissingFrom(receivedUserIds);

        if (showCpuAvatarsWhenEmpty && !HasActiveUserAvatars())
        {
            EnsureCpuAvatars();
        }
    }

    private GameObject SpawnAvatar(Encounter data, bool isCpuAvatar, float displaySeconds)
    {
        if (avatarPrefab == null || characterRoot == null)
        {
            Debug.LogWarning("avatarPrefab or characterRoot is not assigned.");
            return null;
        }

        CleanupDestroyedAvatars();

        if (!isCpuAvatar)
        {
            ClearCpuAvatars();
        }

        GameObject obj = Instantiate(avatarPrefab, characterRoot);
        bool usePrefabDefaultCostume = data.UsesPrefabDefaultAvatar();
        string displayTargetId = data.EnsureDisplayTargetId();
        obj.transform.localPosition = new Vector3(
            Random.Range(-6f, 6f),
            0f,
            Random.Range(2f, 10f)
        );

        if (obj.GetComponent<BillboardToCamera>() == null)
        {
            obj.AddComponent<BillboardToCamera>();
        }

        if (isCpuAvatar)
        {
            AvatarView avatarView = ResolveAvatarView(obj);
            avatarView.Initialize(data, displaySeconds);

            CostumeEntry costume = ResolveCostume(data, true, usePrefabDefaultCostume);
            CrowdAvatar avatarScript = obj.GetComponent<CrowdAvatar>();
            if (avatarScript != null)
            {
                avatarScript.ApplyCostume(costume);
                avatarScript.SetPlayerInfo(displayTargetId, ResolveMessageTexts(data));
            }
        }
        else
        {
            ConfigureUserAvatar(obj, data, displaySeconds);
        }

        activeAvatarsQueue.Enqueue(obj);
        if (isCpuAvatar)
        {
            activeCpuAvatars.Add(obj);
        }
        else
        {
            activeUserAvatars.Add(obj);
        }

        while (activeAvatarsQueue.Count > maxAvatarCount)
        {
            GameObject oldestAvatar = activeAvatarsQueue.Dequeue();
            if (oldestAvatar != null)
            {
                Destroy(oldestAvatar);
            }
        }

        return obj;
    }

    private void ConfigureUserAvatar(GameObject avatarObject, Encounter data, float displaySeconds)
    {
        if (avatarObject == null || data == null)
        {
            return;
        }

        string displayTargetId = data.EnsureDisplayTargetId();
        ResolveAvatarView(avatarObject).Initialize(data, displaySeconds);

        CrowdAvatar avatarScript = avatarObject.GetComponent<CrowdAvatar>();
        if (avatarScript == null)
        {
            return;
        }

        CostumeEntry costume = ResolveCostume(data, false, data.UsesPrefabDefaultAvatar());
        avatarScript.ApplyCostume(costume);
        avatarScript.SetPlayerInfo(displayTargetId, ResolveMessageTexts(data));
    }

    private static AvatarView ResolveAvatarView(GameObject avatarObject)
    {
        AvatarView avatarView = avatarObject.GetComponent<AvatarView>();
        return avatarView != null ? avatarView : avatarObject.AddComponent<AvatarView>();
    }

    private void RemoveUserAvatarsMissingFrom(HashSet<string> receivedUserIds)
    {
        List<string> removedUserIds = new List<string>();
        foreach (KeyValuePair<string, ActiveUserAvatar> pair in activeUserAvatarsById)
        {
            if (pair.Value.Object != null && receivedUserIds.Contains(pair.Key))
            {
                continue;
            }

            if (pair.Value.Object != null)
            {
                activeUserAvatars.Remove(pair.Value.Object);
                Destroy(pair.Value.Object);
            }

            removedUserIds.Add(pair.Key);
        }

        foreach (string userId in removedUserIds)
        {
            activeUserAvatarsById.Remove(userId);
        }
    }

    private static string NormalizeUserId(string userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
    }

    private static bool IsCurrentJstDate(string timestamp)
    {
        if (!System.DateTimeOffset.TryParse(timestamp, out System.DateTimeOffset savedAt))
        {
            return false;
        }

        System.DateTime currentJstDate = System.DateTimeOffset.UtcNow.ToOffset(JstOffset).Date;
        return savedAt.ToOffset(JstOffset).Date == currentJstDate;
    }

    private CostumeEntry ResolveCostume(Encounter data, bool isCpuAvatar, bool usePrefabDefaultCostume)
    {
        if (isCpuAvatar || usePrefabDefaultCostume || data == null || string.IsNullOrWhiteSpace(data.costume_id))
        {
            return null;
        }

        if (avatarCatalog == null)
        {
            if (!hasWarnedMissingCatalog)
            {
                Debug.LogWarning("AvatarCatalog is not assigned. Avatar prefab sprite will be used.");
                hasWarnedMissingCatalog = true;
            }

            return null;
        }

        if (!avatarCatalog.TryGetCostume(data.costume_id, out CostumeEntry costume))
        {
            Debug.LogWarning($"Costume id is not registered in AvatarCatalog: {data.costume_id}");
            return null;
        }

        if (costume == null || !costume.HasAnySprite())
        {
            Debug.LogWarning($"Costume sprites are not assigned: {data.costume_id}");
            return null;
        }

        return costume;
    }

    private List<string> ResolveMessageTexts(Encounter data)
    {
        List<string> messageTexts = new List<string>();
        if (data == null || data.message_ids == null || data.message_ids.Length == 0)
        {
            return messageTexts;
        }

        if (avatarCatalog == null)
        {
            if (!hasWarnedMissingCatalog)
            {
                Debug.LogWarning("AvatarCatalog is not assigned. Message ids cannot be resolved.");
                hasWarnedMissingCatalog = true;
            }

            return messageTexts;
        }

        foreach (string messageId in data.message_ids)
        {
            if (avatarCatalog.TryGetMessage(messageId, out MessageEntry message) &&
                message != null &&
                !string.IsNullOrWhiteSpace(message.text))
            {
                messageTexts.Add(message.text);
                continue;
            }

            Debug.LogWarning($"Message id is not registered in AvatarCatalog: {messageId}");
        }

        return messageTexts;
    }

    private void EnsureCpuAvatars()
    {
        if (avatarPrefab == null || characterRoot == null)
        {
            return;
        }

        CleanupDestroyedAvatars();

        int targetCount = Mathf.Clamp(cpuAvatarCount, 0, Mathf.Max(1, maxAvatarCount));
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

    private void CleanupDestroyedAvatars()
    {
        int count = activeAvatarsQueue.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject avatarObject = activeAvatarsQueue.Dequeue();
            if (avatarObject != null)
            {
                activeAvatarsQueue.Enqueue(avatarObject);
            }
        }

        RemoveDestroyed(activeUserAvatars);
        RemoveDestroyed(activeCpuAvatars);

        List<string> destroyedUserIds = new List<string>();
        foreach (KeyValuePair<string, ActiveUserAvatar> pair in activeUserAvatarsById)
        {
            if (pair.Value.Object == null)
            {
                destroyedUserIds.Add(pair.Key);
            }
        }

        foreach (string userId in destroyedUserIds)
        {
            activeUserAvatarsById.Remove(userId);
        }
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
}
