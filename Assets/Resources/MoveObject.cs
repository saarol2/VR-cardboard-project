using UnityEngine;
using Photon.Pun;

public class MoveObject : Interactive
{
  public float pushForce = 5f; // Tönäisyn voima
  
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
    
    // Etsi kamera (pelaajan katse)
    Camera mainCamera = Camera.main;
    if (mainCamera != null)
    {
      // Laske tönäisyn suunta
      Vector3 direction = mainCamera.transform.forward;
      
      // Pyydä kaikkia pelaajia tönäisemään objektia
      pv.RPC("PushObject", RpcTarget.AllBuffered, direction);
    }
  }
  
  [PunRPC]
  void PushObject(Vector3 direction)
  {
    if (rb == null) rb = GetComponent<Rigidbody>();
    if (rb != null)
    {
      rb.AddForce(direction * pushForce, ForceMode.Impulse);
      Debug.Log("Pushed " + transform.name + " in direction: " + direction);
    }
  }
}