using UnityEngine;
using Photon.Pun;
using System.Collections;

public class BeerPongManager : MonoBehaviourPunCallbacks
{
    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public float ballRespawnDelay = 3f;

    private int currentPlayerTurn = 1; // Which player's turn (1 or 2)
    private GameObject currentBall;

    void Start()
    {
        Debug.Log("BeerPongManager Started");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Joined room. IsMasterClient: " + PhotonNetwork.IsMasterClient);
        
        // Only master spawns the first ball
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke("SpawnBallForCurrentPlayer", 2f);
        }
    }

    void SpawnBallForCurrentPlayer()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball prefab not assigned!");
            return;
        }

        // Destroy old ball if exists
        if (currentBall != null)
        {
            PhotonNetwork.Destroy(currentBall);
        }

        Debug.Log($"Spawning ball for Player {currentPlayerTurn}");
        
        currentBall = PhotonNetwork.Instantiate(ballPrefab.name, new Vector3(0, 1.6f, 0), Quaternion.identity);
        
        if (currentBall != null)
        {
            // Transfer ownership to the player whose turn it is
            int ownerActorNumber = GetActorNumberForPlayer(currentPlayerTurn);
            PhotonView pv = currentBall.GetComponent<PhotonView>();
            if (pv != null)
            {
                pv.TransferOwnership(ownerActorNumber);
                Debug.Log($"Ball ownership transferred to Player {currentPlayerTurn} (Actor {ownerActorNumber})");
            }

            // Add component to detect when ball is thrown
            BallWatcher watcher = currentBall.AddComponent<BallWatcher>();
            watcher.manager = this;
        }
    }

    int GetActorNumberForPlayer(int playerNumber)
    {
        var players = PhotonNetwork.PlayerList;
        Debug.Log($"Total players in room: {players.Length}");
        
        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log($"Player {i + 1}: ActorNumber = {players[i].ActorNumber}, IsMasterClient = {players[i].IsMasterClient}");
        }
        
        if (players.Length >= 2)
        {
            // Player 1 = Master Client (first player)
            // Player 2 = Second player
            if (playerNumber == 1)
            {
                // Return master client's actor number
                foreach (var p in players)
                {
                    if (p.IsMasterClient)
                    {
                        Debug.Log($"Returning Player 1 (Master): {p.ActorNumber}");
                        return p.ActorNumber;
                    }
                }
            }
            else if (playerNumber == 2)
            {
                // Return non-master client's actor number
                foreach (var p in players)
                {
                    if (!p.IsMasterClient)
                    {
                        Debug.Log($"Returning Player 2 (Non-Master): {p.ActorNumber}");
                        return p.ActorNumber;
                    }
                }
            }
        }
        
        Debug.LogWarning($"Could not find player {playerNumber}, returning local player");
        return PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public void OnBallThrown()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"Player {currentPlayerTurn} threw the ball!");
        
        // Switch turn
        currentPlayerTurn = (currentPlayerTurn == 1) ? 2 : 1;
        Debug.Log($"Turn switched to Player {currentPlayerTurn}");

        // Spawn new ball after delay
        StartCoroutine(RespawnBallAfterDelay());
    }

    IEnumerator RespawnBallAfterDelay()
    {
        yield return new WaitForSeconds(ballRespawnDelay);
        SpawnBallForCurrentPlayer();
    }
}

// Simple component to detect when ball has been thrown
public class BallWatcher : MonoBehaviour
{
    public BeerPongManager manager;
    private bool hasNotified = false;

    void Update()
    {
        if (hasNotified) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.useGravity && !rb.isKinematic)
        {
            // Ball has been thrown!
            hasNotified = true;
            if (manager != null)
            {
                manager.OnBallThrown();
            }
        }
    }
}
