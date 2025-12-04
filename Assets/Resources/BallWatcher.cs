using UnityEngine;
using Photon.Pun;

public class BallWatcher : MonoBehaviourPun
{
    private bool hasNotified = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (hasNotified || rb == null) return;

        // Vain pallon omistaja tarkkailee heittoa
        if (!photonView.IsMine) return;

        // Ehto milloin tulkitaan, että pallo on "heitetty"
        // Voit säätää rajoja (velocity tms.)
        if (rb.useGravity && !rb.isKinematic && rb.linearVelocity.magnitude > 0.2f)
        {
            hasNotified = true;

            Debug.Log($"BallWatcher: Ball thrown by actor {photonView.Owner.ActorNumber}, notifying MasterClient");

            // Kerrotaan masterille että pallo on heitetty
            photonView.RPC(nameof(RPC_NotifyThrown), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RPC_NotifyThrown()
    {
        // Tämä ajetaan MASTER-KLIENTILLÄ
        Debug.Log("BallWatcher: RPC_NotifyThrown received on MasterClient");

        BeerPongManager manager = FindObjectOfType<BeerPongManager>();
        if (manager != null)
        {
            manager.OnBallThrown();
        }
        else
        {
            Debug.LogError("BeerPongManager not found in scene!");
        }
    }
}
