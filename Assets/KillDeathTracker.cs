using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class KillDeathTracker : MonoBehaviourPun
{
    public int kills = 0;
    public int deaths = 0;

    private const string KILLS_KEY = "Kills";

    void Start()
    {
        if (photonView.IsMine)
        {
            UpdateKillProperty(0); // Initialize
        }
    }

    public void AddKill()
    {
        if (photonView.IsMine)
        {
            kills++;
            UpdateKillProperty(kills);
        }
    }

    public void AddDeath()
    {
        if (photonView.IsMine)
        {
            deaths++;
        }
    }

    private void UpdateKillProperty(int value)
    {
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
        hash[KILLS_KEY] = value;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public static int GetKills(Player player)
    {
        if (player.CustomProperties.ContainsKey(KILLS_KEY))
        {
            return (int)player.CustomProperties[KILLS_KEY];
        }
        return 0;
    }
}
