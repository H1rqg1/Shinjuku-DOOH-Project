using UnityEngine;
using TMPro; // ★「using UnityEngine.UI;」からこれに書き換えます

public class AvatarCounter : MonoBehaviour
{
    [Header("人数を表示するテキスト")]
    public TextMeshProUGUI counterText; // ★「Text」から「TextMeshProUGUI」に書き換えます

    void Start()
    {
        InvokeRepeating("UpdateCount", 0f, 1f);
    }

    void UpdateCount()
    {
        int count = FindObjectsOfType<CrowdAvatar>().Length;

        if (counterText != null)
        {
            counterText.text = "現在のすれ違い人数： " + count + " 人";
        }
    }
}