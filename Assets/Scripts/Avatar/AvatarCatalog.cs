using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DOOH/Avatar Catalog")]
public class AvatarCatalog : ScriptableObject
{
    public List<CostumeEntry> costumes = new();
    public List<MessageEntry> messages = new();

    private Dictionary<string, CostumeEntry> costumeMap;
    private Dictionary<string, MessageEntry> messageMap;

    public void Initialize()
    {
        costumeMap = new Dictionary<string, CostumeEntry>();
        messageMap = new Dictionary<string, MessageEntry>();

        foreach (var costume in costumes)
        {
            if (costume == null || string.IsNullOrWhiteSpace(costume.id))
            {
                continue;
            }

            if (!costumeMap.ContainsKey(costume.id))
            {
                costumeMap.Add(costume.id, costume);
            }
        }

        foreach (var message in messages)
        {
            if (!messageMap.ContainsKey(message.id))
            {
                messageMap.Add(message.id, message);
            }
        }
    }

    public bool TryGetCostume(string id, out CostumeEntry costume)
    {
        if (costumeMap == null) Initialize();

        if (string.IsNullOrWhiteSpace(id))
        {
            costume = null;
            return false;
        }

        return costumeMap.TryGetValue(id, out costume);
    }

    public bool TryGetMessage(string id, out MessageEntry message)
    {
        if (messageMap == null) Initialize();
        return messageMap.TryGetValue(id, out message);
    }
}
