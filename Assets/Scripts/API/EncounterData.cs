using System;

[Serializable]
public class Encounter
{
    public string my_id;
    public string target_id;
    public string timestamp;

    public string costume_id;
    public string[] message_ids;

    public string GetDisplayKey()
    {
        return $"{my_id}_{target_id}_{timestamp}";
    }
}

[Serializable]
public class EncounterList
{
    public Encounter[] encounters;
}
