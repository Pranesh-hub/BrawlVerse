using Photon.Pun;
using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    private AttackData data;

    public PlayerAttackState(PlayerStateMachine ctx, PlayerStateFactory factory, AttackData attack)
        : base(ctx, factory)
    {
        data = attack;
    }

    public override void EnterState()
    {
        ctx.runtimeOverride["AttackBase"] = data.animation;
        ctx.animator.SetTrigger("isAttacking");
        ctx.animator.SetBool("IsAttacking", true);
        PlayAttackSound(data.attackName);
    }

    public override void UpdateState() { }

    public override void ExitState()
    {
        ctx.animator.SetBool("IsAttacking", false);
    }

    public void ApplyDamage()
    {
        if (!ctx.attackOriginMap.TryGetValue(data.AttackOriginName, out var origin))
        {
            Debug.LogWarning($"No attack origin found for '{data.AttackOriginName}'. Using player transform as fallback.");
            origin = ctx.transform;
        }

        Collider[] hits = Physics.OverlapSphere(origin.position, data.range, ctx.EnemyLayer);

        foreach (var hit in hits)
        {
            AttackEvents.Broadcast(ctx.gameObject, hit.gameObject, data);

            if (hit.TryGetComponent<PlayerStateMachine>(out var psm))
            {
                if (psm.wasParried)
                {
                    Debug.Log("Skipped due to parry");
                    continue;
                }
            }

            if (hit.TryGetComponent<PlayerHealth>(out var health))
            {
                PhotonView targetView = health.GetComponent<PhotonView>();
                PhotonView attackerView = ctx.GetComponent<PhotonView>();

                if (targetView != null && attackerView != null)
                {
                    int attackerID = attackerView.ViewID;
                    targetView.RPC("TakeDamage", RpcTarget.All, data.damage, attackerID);
                }
            }

            if (hit.TryGetComponent<EnemyAI>(out var enemy))
            {
                Vector3 dir = (hit.transform.position - origin.position).normalized;
                enemy.OnHitByPlayer(dir, data.pushForce, data.damage, ctx.gameObject);
            }

            if (hit.attachedRigidbody != null)
            {
                Vector3 pushDir = (hit.transform.position - origin.position).normalized;
                hit.attachedRigidbody.AddForce(pushDir * data.pushForce, ForceMode.Impulse);
            }
        }
    }
    private void PlayAttackSound(string attack)
    {
        switch (attack.ToLower())
        {
            case "kick":
                AudioManager.Instance?.PlayKick();
                break;
            case "stomp":
                AudioManager.Instance?.PlayStomp();
                break;
            case "uppercut":
                AudioManager.Instance?.PlayUppercut();
                break;
            case "push":
                AudioManager.Instance?.PlayPush();
                break;
            case "headbutt":
                AudioManager.Instance?.PlayHeadbutt();
                break;
            default:
                Debug.Log($"[Audio] No sound mapped for attack: {attack}");
                break;
        }
    }

}
