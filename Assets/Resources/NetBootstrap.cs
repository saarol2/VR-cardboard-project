using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] string gameVersion = "0.1";

    // Drag HeadAvatar (cube-only) here
    [SerializeField] GameObject headAvatarPrefab;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 3 }, null);
    }


    public override void OnJoinedRoom()
    {
        if (!headAvatarPrefab)
        {
            Debug.LogError("No HeadAvatar prefab assigned in NetBootstrap.");
            return;
        }

        int i = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        Vector3[] spawns = {
        new Vector3(-0.5f, 1.6f, 0f),
        new Vector3( 0.5f, 1.6f, 0f),
        new Vector3(-0.5f, 1.6f, 0.6f),
        new Vector3( 0.5f, 1.6f, 0.6f),
    };
        Vector3 pos = spawns[Mathf.Clamp(i, 0, spawns.Length - 1)];

        // Spawn your network head
        GameObject myHead = PhotonNetwork.Instantiate(headAvatarPrefab.name, pos, Quaternion.identity);

        // Snap *local rig* (parent of Main Camera) to spawn
        var pv = myHead.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Transform rig = cam.transform.parent ? cam.transform.parent : cam.transform; // <-- use parent if present
                rig.SetPositionAndRotation(pos, Quaternion.identity);
                // (optional) point forward into the room:
                rig.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
        }
    }
}
