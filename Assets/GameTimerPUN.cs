using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameTimerPUN : MonoBehaviourPunCallbacks
{
    public static bool isGameStarted = false;
    public static GameTimerPUN Instance;

    public float countdownTime = 300f;
    public bool isGameOver = false;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;
    public TextMeshProUGUI timerTextUI;
    public TextMeshProUGUI playerCountText;
    public Button startGameButton;
    public TextMeshProUGUI preGameCountdownText;

    private Color defaultColor = Color.white;
    private Color warningColor = Color.yellow;
    private Color dangerColor = new Color(0.8f, 0f, 0f);

    private const float preCountdownDuration = 5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (timerTextUI != null)
            timerTextUI.gameObject.SetActive(false);

        if (preGameCountdownText != null)
            preGameCountdownText.gameObject.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[GameTimer] Joined Room. Waiting for all players...");

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CheckAndStartGame());
            SetupStartButton();
        }

        if (timerTextUI != null)
            timerTextUI.gameObject.SetActive(true);

        if (playerCountText != null)
            playerCountText.gameObject.SetActive(true);

        UpdatePlayerCount();

        LeaderBoard.Instance?.ActivateDuringGame();
        StartCoroutine(WaitForStartAndCountdown());
    }

    private IEnumerator CheckAndStartGame()
    {
        while (PhotonNetwork.CurrentRoom.PlayerCount < 3)
        {
            Debug.Log($"Waiting... {PhotonNetwork.CurrentRoom.PlayerCount}/3");
            yield return new WaitForSeconds(0.5f);
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("startTime"))
        {
            Debug.Log("Auto-starting game (3 or more players joined).");
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["startTime"] = PhotonNetwork.Time + preCountdownDuration;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    private void SetupStartButton()
    {
        if (startGameButton != null && PhotonNetwork.IsMasterClient)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(ManuallyStartGame);
        }
    }

    private void ManuallyStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("startTime"))
        {
            Debug.Log("Manually starting game by MasterClient.");
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["startTime"] = PhotonNetwork.Time + preCountdownDuration;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(false);
    }

    private IEnumerator WaitForStartAndCountdown()
    {
        while (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("startTime"))
            yield return null;

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(false);

        double startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["startTime"];
        double countdownStartTime = startTime - preCountdownDuration;
        double now = PhotonNetwork.Time;

        if (now < countdownStartTime)
        {
            yield return new WaitForSeconds((float)(countdownStartTime - now));
        }

        yield return StartCoroutine(ShowPreGameCountdown((float)(startTime - PhotonNetwork.Time)));

        // Start actual game timer
        double endTime = startTime + countdownTime;
        isGameStarted = true;

        while (PhotonNetwork.Time < endTime)
        {
            float timeLeft = (float)(endTime - PhotonNetwork.Time);
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            timerTextUI.text = $"{minutes:00}:{seconds:00}";

            if (timeLeft <= 30f)
                timerTextUI.color = dangerColor;
            else if (timeLeft <= 120f)
                timerTextUI.color = warningColor;
            else
                timerTextUI.color = defaultColor;

            yield return null;
        }

        isGameOver = true;

        if (timerTextUI != null)
            timerTextUI.gameObject.SetActive(false);

        if (playerCountText != null)
            playerCountText.gameObject.SetActive(true);

        LeaderBoard.Instance?.ShowFinalLeaderboard();
        ShowLeaderboardInConsole();
    }

    private IEnumerator ShowPreGameCountdown(float waitSeconds)
    {
        if (preGameCountdownText == null)
        {
            Debug.LogWarning("preGameCountdownText is not assigned!");
            yield break;
        }

        preGameCountdownText.gameObject.SetActive(true);

        int wholeSeconds = Mathf.FloorToInt(waitSeconds);
        for (int i = wholeSeconds; i > 0; i--)
        {
            preGameCountdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        preGameCountdownText.text = "BATTLE!";
        yield return new WaitForSeconds(1f);

        preGameCountdownText.gameObject.SetActive(false);
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && PhotonNetwork.InRoom)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = 10;
            playerCountText.text = $"{current}/{max} Players";
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();
    }

    private void ShowLeaderboardInConsole()
    {
        Debug.Log("========= LEADERBOARD =========");

        var sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(p => p.GetScore())
            .ThenBy(p => p.NickName)
            .ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Player p = sortedPlayers[i];
            string name = string.IsNullOrEmpty(p.NickName) ? "Unnamed" : p.NickName;
            int kills = p.GetScore();

            Debug.Log($"Rank {i + 1}: {name} — Kills: {kills}");

            if (p.IsLocal)
            {
                if (i < 3)
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
