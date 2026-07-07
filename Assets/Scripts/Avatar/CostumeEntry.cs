using System;
using UnityEngine;

[Serializable]
public class CostumeEntry
{
    public string id;
    public string displayName;
    public string spriteAddress;

    public Sprite standSprite;
    public Sprite walkSprite;
    public Sprite sitSprite;

    public bool HasAnySprite()
    {
        return standSprite != null || walkSprite != null || sitSprite != null;
    }
}
