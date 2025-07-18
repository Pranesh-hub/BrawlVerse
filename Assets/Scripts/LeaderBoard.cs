using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using TMPro;
using Photon.Realtime;
using UnityEngine;

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
        playersHolder.SetActive(false);
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
            .OrderByDescending(p => p.GetScore())
            .ThenBy(p => p.NickName)
            .ToList();

        for (int i = 0; i < sorted.Count && i < slots.Length; i++)
        {
            Player p = sorted[i];

            slots[i].SetActive(true);
            nameTexts[i].text = string.IsNullOrEmpty(p.NickName) ? "unnamed" : p.NickName;
            scoreTexts[i].text = p.GetScore().ToString();

            Color color = i == 0 ? gold : i == 1 ? silver : i == 2 ? bronze : normal;
            nameTexts[i].color = color;
            scoreTexts[i].color = color;
        }
    }

    private void Update()
    {
        if (allowTabToggle)
            playersHolder.SetActive(Input.GetKey(KeyCode.Tab));
    }

    public void ActivateDuringGame()
    {
        allowTabToggle = true;
        playersHolder.SetActive(false); // Initially hidden
    }
}
