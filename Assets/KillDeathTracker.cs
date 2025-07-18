using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;

public class KillDeathTracker : MonoBehaviourPun
{
    public int deaths = 0;

    public void AddDeath()
    {
        if (photonView.IsMine)
        {
            deaths++;
        }
    }

    [PunRPC]
    public void RPC_RewardKill()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.LocalPlayer.AddScore(1);
            Debug.Log("Kill rewarded. Current score: " + PhotonNetwork.LocalPlayer.GetScore());
        }
    }

    public static int GetKills(Player player)
    {
        return player.GetScore();
    }
}
