using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;

public class BeerPongManager : MonoBehaviourPunCallbacks
{
    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public float ballRespawnDelay = 5f;

    private int currentPlayerTurn = 1;
    private GameObject currentBall;

    [Header("Spawn Points")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;

    [Header("Score")]
    public int player1Score = 0;
    public int player2Score = 0;
    public int maxScore = 6;

    [Header("Win Effects")]
    public AudioClip winSound;
    private AudioSource audioSource;

    private bool gameOver = false;
    private GameObject winTextPlayer1;
    private GameObject winTextPlayer2;

    void Start()
    {
        Debug.Log("BeerPongManager Started");
        
        // Add audiosource component
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.volume = 1.0f;
        
        Debug.Log($"AudioSource created. Win sound assigned: {(winSound != null)}");

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

        photonView.RPC(nameof(RPC_PrintScores), RpcTarget.All);

        // Spawn new ball after delay
        StartCoroutine(RespawnBallAfterDelay());
    }

    IEnumerator RespawnBallAfterDelay()
    {
        yield return new WaitForSeconds(ballRespawnDelay);
        SpawnBallForCurrentPlayer();
    }

    // === SCORING ===
    public void OnCupHit(int cupOwnerPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (gameOver) return;

        // If Player 1's cup is hit -> Player 2 scores
        int scoringPlayer = (cupOwnerPlayer == 1) ? 2 : 1;

        // Update scores on the master client
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

        // Sync scores and possible winner to all
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

            // No more new balls
            if (PhotonNetwork.IsMasterClient && currentBall != null)
            {
                PhotonNetwork.Destroy(currentBall);
                currentBall = null;
            }

            // Play win sound
            PlayWinSound();

            // Show win texts to both players
            ShowWinText(winnerPlayer);
        }
    }

    void PlayWinSound()
    {
        Debug.Log($"PlayWinSound called - winSound: {winSound != null}, audioSource: {audioSource != null}");
        
        if (winSound == null)
        {
            Debug.LogError("Win sound AudioClip is NOT assigned in Inspector!");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null! Creating one now...");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            audioSource.volume = 1.0f;
        }
        
        Debug.Log($"Playing win sound: {winSound.name}, volume: {audioSource.volume}");
        audioSource.PlayOneShot(winSound, 1.0f);
    }

    void ShowWinText(int winnerPlayer)
    {
        string winMessage = $"Player {winnerPlayer} won!";

        // Create text for Player 1
        winTextPlayer1 = CreateWinTextObject(
            winMessage,
            new Vector3(0, 10, 0),
            Quaternion.Euler(0, 90, 0) // Facing Player 1
        );

        // Create text for Player 2
        winTextPlayer2 = CreateWinTextObject(
            winMessage,
            new Vector3(0, 10, 0),
            Quaternion.Euler(0, -90, 0) // Facing Player 2
        );

        Debug.Log($"Win texts created for Player {winnerPlayer}");
    }

    GameObject CreateWinTextObject(string message, Vector3 position, Quaternion rotation)
    {
        GameObject textObj = new GameObject("WinText");
        textObj.transform.position = position;
        textObj.transform.rotation = rotation;

            // Lisää TextMeshPro component
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.fontStyle = FontStyles.Bold;
        
        tmp.rectTransform.sizeDelta = new Vector2(20, 6);
        tmp.fontMaterial.SetFloat("_CullMode", 2);

        return textObj;
    }

    // RPC that prints scores to all consoles after each throw
    [PunRPC]
    void RPC_PrintScores()
    {
        Debug.Log($"SCORE UPDATE → Player 1: {player1Score}, Player 2: {player2Score}");
    }
}