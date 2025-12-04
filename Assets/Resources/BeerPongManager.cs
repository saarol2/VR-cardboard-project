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

    [Header("Score")]
    public int player1Score = 0;
    public int player2Score = 0;
    public int maxScore = 6;

    private bool gameOver = false;

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
        if (gameOver)
        {
            Debug.Log("[MASTER] Game is over, not spawning new ball.");
            return;
        }

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

        Transform spawnPoint = (currentPlayerTurn == 1) ? player1SpawnPoint : player2SpawnPoint;

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
            int ownerActorNumber = GetActorNumberForPlayer(currentPlayerTurn);
            PhotonView pv = currentBall.GetComponent<PhotonView>();
            if (pv != null)
            {
                pv.TransferOwnership(ownerActorNumber);
                Debug.Log($"Ball ownership transferred to Player {currentPlayerTurn} (Actor {ownerActorNumber})");
                Debug.Log($"AFTER TRANSFER: photonView.OwnerActorNr = {pv.OwnerActorNr}");
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
            if (playerNumber == 1)
            {
                foreach (var p in players)
                    if (p.IsMasterClient)
                        return p.ActorNumber;
            }
            else if (playerNumber == 2)
            {
                foreach (var p in players)
                    if (!p.IsMasterClient)
                        return p.ActorNumber;
            }
        }

        Debug.LogWarning($"Could not find player {playerNumber}, returning local player");
        return PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public void OnBallThrown()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (gameOver) return;

        Debug.Log($"Player {currentPlayerTurn} threw the ball!");

        // Switch turn
        currentPlayerTurn = (currentPlayerTurn == 1) ? 2 : 1;
        Debug.Log($"Turn switched to Player {currentPlayerTurn}");

        // Tulostetaan pisteet kaikille jokaisen heiton jälkeen
        photonView.RPC(nameof(RPC_PrintScores), RpcTarget.All);

        // Spawn new ball after delay
        StartCoroutine(RespawnBallAfterDelay());
    }

    IEnumerator RespawnBallAfterDelay()
    {
        yield return new WaitForSeconds(ballRespawnDelay);
        SpawnBallForCurrentPlayer();
    }

    // === PISTEENLASKU ===
    // Kuppi, jonka omistaja on 'cupOwnerPlayer', osui
    public void OnCupHit(int cupOwnerPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (gameOver) return;

        // Jos Player 1:n kuppi osuu -> Player 2 saa pisteen
        int scoringPlayer = (cupOwnerPlayer == 1) ? 2 : 1;

        // Päivitetään pisteet masterilla
        if (scoringPlayer == 1)
            player1Score++;
        else
            player2Score++;

        bool someoneWon = (player1Score >= maxScore || player2Score >= maxScore);
        int winnerPlayer = 0;
        if (someoneWon)
        {
            winnerPlayer = (player1Score >= maxScore) ? 1 : 2;
            gameOver = true;
        }

        // Synkataan pisteet ja mahdollinen voittaja kaikille
        photonView.RPC(nameof(RPC_SyncScoreAndMaybeEnd),
            RpcTarget.All,
            player1Score,
            player2Score,
            winnerPlayer);
    }

    [PunRPC]
    void RPC_SyncScoreAndMaybeEnd(int p1Score, int p2Score, int winnerPlayer)
    {
        player1Score = p1Score;
        player2Score = p2Score;

        Debug.Log($"Score updated - P1: {player1Score}, P2: {player2Score}");

        if (winnerPlayer != 0)
        {
            gameOver = true;
            Debug.Log($"GAME OVER! Player {winnerPlayer} wins!");

            // Ei enää uutta palloa
            if (PhotonNetwork.IsMasterClient && currentBall != null)
            {
                PhotonNetwork.Destroy(currentBall);
                currentBall = null;
            }
        }
    }

    // RPC, joka tulostaa pisteet jokaisen heiton jälkeen kaikkien konsoleihin
    [PunRPC]
    void RPC_PrintScores()
    {
        Debug.Log($"SCORE UPDATE → Player 1: {player1Score}, Player 2: {player2Score}");
    }
}