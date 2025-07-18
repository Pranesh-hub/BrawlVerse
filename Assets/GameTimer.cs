using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameTimer : NetworkBehaviour
{
    public static GameTimer Instance;

    [SerializeField] private float gameDuration = 10f; // Game duration in seconds

    [Networked] private float gameStartTime { get; set; }

    private bool hasEnded = false;
    private bool hasSpawned = false;

    // Public properties to access time remaining and game status
    public float TimeRemaining
    {
        get
        {
            if (!hasSpawned) return gameDuration;
            return Mathf.Max(0f, gameDuration - (float)(Runner.SimulationTime - gameStartTime));
        }
    }

    public bool IsGameOver => hasSpawned && TimeRemaining <= 0f;

    public override void Spawned()
    {
        Instance = this;
        hasSpawned = true;

        if (Object.HasStateAuthority)
        {
            gameStartTime = (float)Runner.SimulationTime;
            Debug.Log("[GameTimer] Game started at SimulationTime: " + gameStartTime);
        }

        // Start time printing coroutine
        StartCoroutine(PrintTimeLeft());
    }

    void Update()
    {
        if (!hasSpawned) return;

        if (!hasEnded && IsGameOver)
        {
            hasEnded = true;
            EndGame();
        }
    }

    private void EndGame()
    {
        Debug.Log("[GameTimer] Game has ended. Quitting...");

        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Button clicked");
        Application.Quit();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private System.Collections.IEnumerator PrintTimeLeft()
    {
        while (!IsGameOver)
        {
            Debug.Log("[GameTimer] Time left: " + Mathf.CeilToInt(TimeRemaining) + " seconds");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("[GameTimer] Time is up!");
    }
}
