using System;
using System.Collections.Generic;

[Serializable]
public class Encounter
{
    private static int nextNoneAvatarIndex = 1;
    private static readonly Dictionary<string, string> noneAvatarIdsByEncounterKey = new Dictionary<string, string>();

    public string my_id;
    public string target_id;
    public string timestamp;

    public string costume_id;
    public string[] message_ids;

    public string GetDisplayKey()
    {
        return $"{my_id}_{target_id}_{timestamp}";
    }

    public bool HasNoneTargetId()
    {
        return IsNoneLikeId(target_id);
    }

    public bool UsesPrefabDefaultAvatar()
    {
        return IsNoneLikeId(target_id) || IsGeneratedNoneDisplayId(target_id);
    }

    public string EnsureDisplayTargetId()
    {
        if (!HasNoneTargetId())
        {
            target_id = target_id.Trim();
            return target_id;
        }

        string noneAvatarKey = GetNoneAvatarKey();
        if (!noneAvatarIdsByEncounterKey.TryGetValue(noneAvatarKey, out string displayTargetId))
        {
            displayTargetId = $"None_{nextNoneAvatarIndex:00}";
            noneAvatarIdsByEncounterKey[noneAvatarKey] = displayTargetId;
            nextNoneAvatarIndex++;
        }

        target_id = displayTargetId;
        return target_id;
    }

    private string GetNoneAvatarKey()
    {
        string sourceId = string.IsNullOrWhiteSpace(my_id) ? "unknown_source" : my_id.Trim();
        string sourceTimestamp = string.IsNullOrWhiteSpace(timestamp) ? "unknown_timestamp" : timestamp.Trim();
        return $"{sourceId}_{sourceTimestamp}";
    }

    private static bool IsNoneLikeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string normalizedValue = value.Trim();
        return normalizedValue.Equals("None", StringComparison.OrdinalIgnoreCase) ||
               normalizedValue.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedNoneDisplayId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = value.Trim();
        if (!normalizedValue.StartsWith("None_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string numberText = normalizedValue.Substring("None_".Length);
        return int.TryParse(numberText, out _);
    }
}

[Serializable]
public class EncounterList
{
    public Encounter[] encounters;
}
