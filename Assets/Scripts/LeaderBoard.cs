using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class LeaderBoard : MonoBehaviour
{
    public static LeaderBoard Instance;

    public GameObject playersHolder;

    [Header("Options")]
    public float refreshRate = 1f;
    public bool allowTabToggle = true;

    [Header("UI")]
    public GameObject[] slots;
    public TextMeshProUGUI[] scoreTexts;
    public TextMeshProUGUI[] nameTexts;

    [Header("Colors")]
    public Color gold = new Color(1f, 0.84f, 0f);
    public Color silver = new Color(0.75f, 0.75f, 0.75f);
    public Color bronze = new Color(0.8f, 0.5f, 0.2f);
    public Color normal = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        InvokeRepeating(nameof(Refresh), 1f, refreshRate);
    }

    public void Refresh()
    {
        if (!allowTabToggle) return;
        RenderLeaderboard();
    }

    public void ShowFinalLeaderboard()
    {
        allowTabToggle = false;
        playersHolder.SetActive(true);
        RenderLeaderboard();
    }

    private void RenderLeaderboard()
    {
        foreach (GameObject slot in slots)
            slot.SetActive(false);

        var sorted = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetKills(p))
            .ThenBy(p => GetDeaths(p))
            .ToList();

        for (int i = 0; i < sorted.Count && i < slots.Length; i++)
        {
            var player = sorted[i];

            slots[i].SetActive(true);
            nameTexts[i].text = string.IsNullOrEmpty(player.NickName) ? "unnamed" : player.NickName;
            scoreTexts[i].text = GetKills(player).ToString(); // Display kills only

            Color rankColor = i == 0 ? gold : i == 1 ? silver : i == 2 ? bronze : normal;
            nameTexts[i].color = rankColor;
            scoreTexts[i].color = rankColor;
        }
    }

    private int GetKills(Player p)
    {
        return p.CustomProperties.TryGetValue("Kills", out object val) ? (int)val : 0;
    }

    private int GetDeaths(Player p)
    {
        return p.CustomProperties.TryGetValue("Deaths", out object val) ? (int)val : 0;
    }

    private void Update()
    {
        if (allowTabToggle)
            playersHolder.SetActive(Input.GetKey(KeyCode.Tab));
    }
}
