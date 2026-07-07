using System.Collections.Generic;
using UnityEngine;

public class CrowdAvatarManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private APIManager apiManager;
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private Transform characterRoot;

    [Header("Display")]
    [SerializeField] private int maxAvatarCount = 30;

    private readonly HashSet<string> processedEncounters = new HashSet<string>();
    private readonly Queue<GameObject> activeAvatarsQueue = new Queue<GameObject>();

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

        foreach (Encounter encounter in encounters)
        {
            if (encounter == null)
            {
                continue;
            }

            string uniqueKey = encounter.GetDisplayKey();
            if (!processedEncounters.Add(uniqueKey))
            {
                continue;
            }

            SpawnAvatar(encounter);
        }
    }

    private void SpawnAvatar(Encounter data)
    {
        if (avatarPrefab == null || characterRoot == null)
        {
            Debug.LogWarning("avatarPrefab or characterRoot is not assigned.");
            return;
        }

        GameObject obj = Instantiate(avatarPrefab, characterRoot);
        obj.transform.localPosition = new Vector3(
            Random.Range(-6f, 6f),
            0f,
            Random.Range(2f, 10f)
        );

        AvatarView avatarView = obj.GetComponent<AvatarView>();
        if (avatarView == null)
        {
            avatarView = obj.AddComponent<AvatarView>();
        }

        avatarView.Initialize(data, 10f);

        if (obj.GetComponent<BillboardToCamera>() == null)
        {
            obj.AddComponent<BillboardToCamera>();
        }

        CrowdAvatar avatarScript = obj.GetComponent<CrowdAvatar>();
        if (avatarScript != null)
        {
            avatarScript.SetPlayerName(data.target_id);
        }

        activeAvatarsQueue.Enqueue(obj);

        while (activeAvatarsQueue.Count > maxAvatarCount)
        {
            GameObject oldestAvatar = activeAvatarsQueue.Dequeue();
            if (oldestAvatar != null)
            {
                Destroy(oldestAvatar);
            }
        }
    }
}
