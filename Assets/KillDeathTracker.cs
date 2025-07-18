using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;

public class KillDeathTracker : MonoBehaviourPunCallbacks
{
    public int kills;
    public int deaths;

    private void Start()
    {
        if (photonView.IsMine)
        {
            ResetStats();
        }
    }

    public void AddKill()
    {
        kills++;
        UpdatePlayerProps();
    }

    public void AddDeath()
    {
        deaths++;
        UpdatePlayerProps();
    }

    private void ResetStats()
    {
        kills = 0;
        deaths = 0;
        UpdatePlayerProps();
    }

    private void UpdatePlayerProps()
    {
        if (photonView.IsMine)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["Kills"] = kills;
            props["Deaths"] = deaths;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            PhotonNetwork.LocalPlayer.AddScore(kills); // optional: sync score to kills
        }
    }
}
