using UnityEngine;
using Photon.Pun;

public class BeerPongCup : MonoBehaviourPun
{
    public int ownerPlayer = 1; // Which player owns this cup (1 or 2)

    private bool alreadyHit = false;

    void OnTriggerEnter(Collider other)
    {
        if (alreadyHit) return;
        if (other.GetComponent<BallThrower>() == null) return;

        alreadyHit = true;

        Debug.Log(
            $"Ball hit Player {ownerPlayer}'s cup! " +
            $"(local actor {PhotonNetwork.LocalPlayer.ActorNumber}, isMaster={PhotonNetwork.IsMasterClient})"
        );

        photonView.RPC(nameof(RPC_NotifyCupHit), RpcTarget.MasterClient);
        photonView.RPC(nameof(RPC_DestroyCup), RpcTarget.All);
    }

    [PunRPC]
    void RPC_NotifyCupHit()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log($"RPC_NotifyCupHit on MasterClient for Player {ownerPlayer}'s cup");

        BeerPongManager manager = FindObjectOfType<BeerPongManager>();
        if (manager != null)
        {
            manager.OnCupHit(ownerPlayer);
        }
        else
        {
            Debug.LogError("BeerPongManager not found in scene!");
        }
    }

    [PunRPC]
    void RPC_DestroyCup()
    {
        Debug.Log(
            $"RPC_DestroyCup: Cup for Player {ownerPlayer} despawned on client {PhotonNetwork.LocalPlayer.ActorNumber}"
        );

        Destroy(gameObject);
    }
}