using UnityEngine;
using Photon.Pun;

public class BeerPongCup : MonoBehaviour
{
    public int ownerPlayer = 1; // Which player owns this cup (1 or 2)

    void OnTriggerEnter(Collider other)
    {
        // Just log for now
        if (other.GetComponent<BallThrower>() != null)
        {
            Debug.Log($"Ball hit Player {ownerPlayer}'s cup!");
        }
    }
}
