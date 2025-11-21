using UnityEngine;
using Photon.Pun;

public class gravityToggle : Interactive
{
    PhotonView pv;
    Rigidbody rb;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }

    public new void Interact()
    {
        if (pv == null) return;

        // Ask Photon to run EnableGravity() on ALL clients
        pv.RPC("EnableGravity", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void EnableGravity()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;
    }
}
