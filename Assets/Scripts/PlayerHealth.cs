using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class PlayerHealth : MonoBehaviourPun
{
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("UI References")]
    public Slider healthSlider;

    public PlayerStateMachine _playerStateMachine;
    public bool isLocalPlayer;

    private KillDeathTracker killDeathTracker;
    private bool isDead = false;

    void Start()
    {
        if (_playerStateMachine == null)
            _playerStateMachine = GetComponent<PlayerStateMachine>();

        killDeathTracker = GetComponent<KillDeathTracker>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
        }    

        UpdateHealthUI();
    }

    [PunRPC]
    public void TakeDamage(int damageAmount, int attackerViewID)
    {
        if (isDead) return;

        if (_playerStateMachine != null && _playerStateMachine.isShieldActive)
        {
            Debug.Log("Shield is active! No damage taken.");
            return;
        }

        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        AudioManager.Instance?.playhit();
        UpdateHealthUI();

        Debug.Log("Player health: " + health);

        if (health <= 0 && !isDead)
        {
            Debug.Log("Player died!");
            isDead = true;

            if (photonView.IsMine)
            {
                killDeathTracker?.AddDeath();

                PhotonView attackerView = PhotonView.Find(attackerViewID);
                if (attackerView != null)
                {
                    attackerView.RPC("RPC_RewardKill", attackerView.Owner);
                }

                // StartCoroutine(RespawnAfterDelay(5f));
                StartCoroutine(HideAndWaitForRespawn(5f));
            }
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
            healthSlider.value = health;
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        if (GameTimerPUN.Instance != null && GameTimerPUN.Instance.isGameOver)
        {
            Debug.Log("Game over -- no respawn.");
            yield break;
        }

        yield return new WaitForSeconds(delay);

        if (isLocalPlayer)
        {
            RoomManager.Instance.SpawnPlayer();
        }

        PhotonNetwork.Destroy(gameObject);
    }

    IEnumerator HideAndWaitForRespawn(float delay)
    {
        // Immediately disable this body
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        PlayerStateMachine sm = GetComponent<PlayerStateMachine>();
        if (sm != null) sm.enabled = false;

        // Hide the body off-screen
        transform.position = Vector3.down * 9999f;

        // Stop here if game is over
        if (GameTimerPUN.Instance != null && GameTimerPUN.Instance.isGameOver)
        {
            Debug.Log("Game over -- no respawn.");
            yield break;
        }

        // Wait
        yield return new WaitForSeconds(delay);

        // ✅ Respawn FIRST
        if (isLocalPlayer)
        {
            RoomManager.Instance.SpawnPlayer();
        }

        // ✅ THEN destroy the dead body
        PhotonNetwork.Destroy(photonView);  // Better: destroy by view, not by object
    }
    
}