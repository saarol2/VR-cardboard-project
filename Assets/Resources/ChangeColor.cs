using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class ChangeColorScrpt : Interactive
{

  PhotonView pv;

  void Awake ()
  {
    pv = GetComponent<PhotonView>();
  }

  public new void Interact()
  {
    if (pv == null) return;
    
    // Pyydä kaikkia pelaajia vaihtamaan väriä
    pv.RPC("ChangeColorRPC", RpcTarget.AllBuffered);
  }

  [PunRPC]
  void ChangeColorRPC()
  {
    Renderer renderer = GetComponent<Renderer>();
    if (renderer != null)
    {
      renderer.material.color = new Color(Random.value, Random.value, Random.value);
    }
    Debug.Log("Color changed for " + transform.name);
  }
}