using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameTimerPUN : MonoBehaviourPunCallbacks
{
    public static GameTimerPUN Instance;
    public float countdownTime = 300f;
    public bool isGameOver = false;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[GameTimer] Joined Room. Starting timer...");
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        float timeLeft = countdownTime;

        while (timeLeft > 0f)
        {
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        isGameOver = true;
        LeaderBoard.Instance?.ShowFinalLeaderboard();
        ShowLeaderboardInConsole();
    }

    private void ShowLeaderboardInConsole()
    {
        Debug.Log("========= LEADERBOARD =========");

        // Inside ShowLeaderboardInConsole()
        var sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(p => p.CustomProperties.TryGetValue("Kills", out var k) ? (int)k : 0)
            .ThenBy(p => p.CustomProperties.TryGetValue("Deaths", out var d) ? (int)d : 0)
            .ToList();

        int currentRank = 1;

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Player p = sortedPlayers[i];
            string name = string.IsNullOrEmpty(p.NickName) ? "Unnamed" : p.NickName;
            int score = p.GetScore();

            // Tie-breaking logic (optional)
            if (i > 0 && score == sortedPlayers[i - 1].GetScore())
            {
                currentRank = sortedPlayers[i - 1].GetScore(); // Same rank
            }

            Debug.Log($"Rank {i + 1}: {name} — Score: {score}");

            // Optional: Play win/lose music
            if (p.IsLocal)
            {
                if (i == 0)
                    AudioManager.Instance?.PlayGameOverWin();
                else
                    AudioManager.Instance?.PlayGameOverLose();
            }
        }

        Debug.Log("========= END =========");

        gameOverPanel.SetActive(true);
        restartButton.onClick.AddListener(RestartGame);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void QuitGame()
    {
        Debug.Log("Quit Button clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
