using System.Collections;
using UnityEngine;
using Photon.Pun;
using Cinemachine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [Header("Player Object")]
    public GameObject player;

    [Header("Player Spawn Point")]
    public Transform spanPoint;

    [Header("Free Look Camera")]
    public CinemachineFreeLook freeLook;

    [Header("Camera UI")]
    public GameObject roomCam;

    [Header("UI")]
    public GameObject nickNameUI;
    public GameObject connectingUI;

    [Header("Room Name")]
    public string roomName = "test";

    string nickName = "unnamed";

    private void Awake()
    {
        Instance = this;
    }

    public void SetNickname(string _name)
    {
        nickName = _name;
    }

    public void OnJoinButtonPressed()
    {
        Debug.Log("Connecting...");
        Debug.Log(roomName);
        PhotonNetwork.JoinOrCreateRoom(roomName, new Photon.Realtime.RoomOptions(), null);
        nickNameUI.SetActive(false);
        connectingUI.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmInGame);
        Debug.Log("Room Joined");
        roomCam.SetActive(false);
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        // Step 1: Instantiate player at spanPoint
        GameObject _player = PhotonNetwork.Instantiate(player.name, spanPoint.position, Quaternion.identity);
        _player.GetComponent<PlayerHealth>().isLocalPlayer = true;

        // Step 2: Clear velocity and force ground recheck
        var stateMachine = _player.GetComponent<PlayerStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.velocity = Vector3.zero;
            stateMachine.isGrounded = false;
            stateMachine.Invoke("ForceGroundCheck", 0.1f); // optional safety
        }

        // Step 3: Adjust position using raycast to snap to ground
        if (Physics.Raycast(spanPoint.position, Vector3.down, out RaycastHit hit, 10f))
        {
            CharacterController cc = _player.GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 groundedPos = hit.point;
                groundedPos.y += cc.height / 2f;

                cc.enabled = false;
                _player.transform.position = groundedPos;
                cc.enabled = true;
            }
        }

        // Step 4: Assign player nickname and camera follow
        PhotonView view = _player.GetComponent<PhotonView>();
        PhotonNetwork.LocalPlayer.NickName = nickName;

        if (view != null && view.IsMine)
        {
            view.RPC("SetPlayerName", RpcTarget.AllBuffered, nickName);

            if (freeLook != null)
            {
                Transform lookAt = _player.transform.GetChild(1);
                freeLook.Follow = lookAt;
                freeLook.LookAt = lookAt;
            }
        }
    }
}
