using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] string gameVersion = "0.1";

    // Drag HeadAvatar (cube-only) here
    [SerializeField] GameObject headAvatarPrefab;

    // NEW: Spawn points for Beer Pong
    [Header("Spawn Points")]
    [SerializeField] Transform player1SpawnPoint;
    [SerializeField] Transform player2SpawnPoint;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 2 }, null); // Changed to 2 players
    }

    public override void OnJoinedRoom()
    {
        if (!headAvatarPrefab)
        {
            Debug.LogError("No HeadAvatar prefab assigned in NetBootstrap.");
            return;
        }

        // Determine spawn position based on player count
        Vector3 pos;
        Quaternion rot = Quaternion.identity;

        int playerIndex = PhotonNetwork.CurrentRoom.PlayerCount - 1;

        if (playerIndex == 0 && player1SpawnPoint != null)
        {
            pos = player1SpawnPoint.position;
            rot = player1SpawnPoint.rotation;
        }
        else if (playerIndex == 1 && player2SpawnPoint != null)
        {
            pos = player2SpawnPoint.position;
            rot = player2SpawnPoint.rotation;
        }
        else
        {
            // Fallback if spawn points not set
            Debug.LogWarning("Spawn points not assigned! Using default positions.");
            Vector3[] fallbackSpawns = {
                new Vector3(-0.5f, 1.6f, 0f),
                new Vector3( 0.5f, 1.6f, 0f)
            };
            pos = fallbackSpawns[Mathf.Clamp(playerIndex, 0, 1)];
        }

        // Spawn your network head
        GameObject myHead = PhotonNetwork.Instantiate(headAvatarPrefab.name, pos, rot);

        // Snap *local rig* (parent of Main Camera) to spawn
        var pv = myHead.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Transform rig = cam.transform.parent ? cam.transform.parent : cam.transform;
                rig.SetPositionAndRotation(pos, rot);
            }
        }
    }
}