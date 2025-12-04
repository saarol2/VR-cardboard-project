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

    [Header("Spawn Points")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;

    void Start()
    {
        Debug.Log("BeerPongManager Started");

        if (player1SpawnPoint != null && player2SpawnPoint != null)
        {
            Debug.Log($"[LOCAL {PhotonNetwork.LocalPlayer.ActorNumber}] P1 spawn: {player1SpawnPoint.position}, P2 spawn: {player2SpawnPoint.position}");
        }
        else
        {
            Debug.LogWarning("Spawn points not assigned in inspector!");
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Joined room. IsMasterClient: " + PhotonNetwork.IsMasterClient);

        // Only master spawns the first ball
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(SpawnBallForCurrentPlayer), 2f);
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

        // Valitse spawn-piste pelaajan vuoron mukaan
        Transform spawnPoint = null;

        if (currentPlayerTurn == 1)
        {
            spawnPoint = player1SpawnPoint;
        }
        else if (currentPlayerTurn == 2)
        {
            spawnPoint = player2SpawnPoint;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point for current player is not assigned!");
            return;
        }

        Debug.Log($"[MASTER] Spawning ball for Player {currentPlayerTurn} at {spawnPoint.position}");

        currentBall = PhotonNetwork.Instantiate(
            ballPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

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
            else
            {
                Debug.LogError("Spawned ball has no PhotonView component!");
            }
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
        // Tämän pitäisi kutsua vain masterilla RPC:n kautta,
        // mutta varmistetaan silti:
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
