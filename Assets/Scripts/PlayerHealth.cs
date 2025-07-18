using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class PlayerHealth : MonoBehaviourPun
{
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("UI References")]
    public Slider healthSlider; // Assign via Inspector or Find in children

    public PlayerStateMachine _playerStateMachine;
    public bool isLocalPlayer;

    private KillDeathTracker killDeathTracker;

    void Start()
    {
        if (_playerStateMachine == null)
            _playerStateMachine = GetComponent<PlayerStateMachine>();

        killDeathTracker = GetComponent<KillDeathTracker>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        UpdateHealthUI();
    }

    [PunRPC]
    public void TakeDamage(int damageAmount, int attackerViewID)
    {
        if (_playerStateMachine != null && _playerStateMachine.isShieldActive)
        {
            Debug.Log("Shield is active! No damage taken.");
            return;
        }

        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Player health: " + health);

        if (health <= 0)
        {
            Debug.Log("Player died!");

            if (photonView.IsMine && killDeathTracker != null)
                killDeathTracker.AddDeath();

            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null && attackerView.IsMine && attackerView.gameObject != this.gameObject)
            {
                var attackerKD = attackerView.GetComponent<KillDeathTracker>();
                if (attackerKD != null)
                    attackerKD.AddKill();
            }

            Die();

            if (isLocalPlayer)
            {
                RoomManager.Instance.SpawnPlayer();
            }    
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
            healthSlider.value = (health / maxHealth) * 100f;
    }

    void Die()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(RespawnAfterDelay(5f));
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        if (GameTimerPUN.Instance != null && GameTimerPUN.Instance.isGameOver)
        {
            Debug.Log("Game over -- no respawn.");
            yield break;
        }

        if (GameTimerPUN.Instance != null && !GameTimerPUN.Instance.isGameOver)
        {
            RoomManager.Instance.SpawnPlayer();
        }

        yield return null;
    }
}