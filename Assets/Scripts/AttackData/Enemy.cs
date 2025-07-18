using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float pushForce = 10f;
    public float knockbackTime = 0.5f;
    public float fallThreshold = -10f;
    public Vector2 respawnAreaX = new Vector2(-5f, 5f);
    public Vector2 respawnAreaZ = new Vector2(-5f, 5f);
    public int health = 100;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isKnockedBack = false;

    private GameObject lastHitBy; // 🔥 Track who last hit this enemy

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Auto-assign player if not set
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
    }

    void Update()
    {
        if (transform.position.y < fallThreshold)
        {
            Respawn();
            return;
        }

        if (!isKnockedBack && player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector3 pushDir = (collision.transform.position - transform.position).normalized;
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
                playerRb.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }

    public void Knockback(Vector3 force)
    {
        if (isKnockedBack) return;

        isKnockedBack = true;
        agent.enabled = false;
        rb.AddForce(force, ForceMode.Impulse);
        Invoke(nameof(RecoverFromKnockback), knockbackTime);
    }

    void RecoverFromKnockback()
    {
        isKnockedBack = false;
        agent.enabled = true;
    }

    void Respawn()
    {
        float x = Random.Range(respawnAreaX.x, respawnAreaX.y);
        float z = Random.Range(respawnAreaZ.x, respawnAreaZ.y);
        transform.position = new Vector3(x, 3f, z);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isKnockedBack = false;
        agent.enabled = true;
        health = 100;
    }

    /// <summary>
    /// Called by the player's attack logic
    /// </summary>
    public void OnHitByPlayer(Vector3 direction, float force, int damage = 10, GameObject attacker = null)
    {
        Knockback(direction.normalized * force);
        health -= damage;
        lastHitBy = attacker;

        Debug.Log($"Enemy took {damage} damage from {attacker?.name}, health = {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died");

        if (lastHitBy != null)
        {
            KillDeathTracker tracker = lastHitBy.GetComponent<KillDeathTracker>();
            if (tracker != null)
            {
                tracker.AddKill();
            }
        }

        Destroy(gameObject); // Or optionally Respawn();
    }
}
