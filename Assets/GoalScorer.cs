
using UnityEngine;
using TMPro;
using System.Collections;
using FMODUnity;

public class GoalScorer : MonoBehaviour
{
    [Header("Scoreboard")]
    public TMP_Text scoreText;
    public int pointsPerGoal = 1;

    [Header("Win Message")]
    public int targetScore = 5;
    public string winMessage = "YOU WIN!";
    public bool stopScoringAtTarget = true;

    [Header("Confetti Prefab")]
    public ParticleSystem confettiPrefab;
    public Transform confettiSpawnPoint;

    [Header("FMOD Sound")]
    public EventReference goalSound;

    [Header("Ball Respawn")]
    public Transform ballSpawnPoint;
    public float respawnDelay = 1.5f;

    private int score = 0;
    private bool goalScored = false;
    private bool respawnInProgress = false;
    private bool gameWon = false;

    private void Start()
    {
        UpdateScore();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        if (goalScored || respawnInProgress)
            return;

        if (gameWon && stopScoringAtTarget)
            return;

        goalScored = true;

        // Add points.
        score += pointsPerGoal;
        UpdateScore();

        // Play confetti.
        PlayConfetti();

        // Play FMOD goal sound.
        if (!goalSound.IsNull)
        {
            RuntimeManager.PlayOneShot(
                goalSound,
                transform.position
            );
        }

        // Check whether the target score was reached.
        if (!gameWon && score >= targetScore)
        {
            gameWon = true;

            if (scoreText != null)
                scoreText.text = winMessage;
        }

        // Reset the ball.
        ResetBall(other.gameObject);
    }

    private void PlayConfetti()
    {
        if (confettiPrefab == null)
        {
            Debug.LogWarning("Confetti Prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition = confettiSpawnPoint != null
            ? confettiSpawnPoint.position
            : transform.position;

        ParticleSystem effect = Instantiate(
            confettiPrefab,
            spawnPosition,
            Quaternion.identity
        );

        effect.Play(true);

        float lifetime = effect.main.duration
            + effect.main.startLifetime.constantMax
            + 1f;

        Destroy(effect.gameObject, lifetime);
    }

    public void ResetBall(GameObject ball)
    {
        if (respawnInProgress || ball == null)
            return;

        respawnInProgress = true;

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ball.SetActive(false);
        StartCoroutine(RespawnBall(ball));
    }

    private IEnumerator RespawnBall(GameObject ball)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (ballSpawnPoint == null)
        {
            Debug.LogWarning("Ball Spawn Point is not assigned!");
            respawnInProgress = false;
            goalScored = false;
            yield break;
        }

        ball.transform.SetPositionAndRotation(
            ballSpawnPoint.position,
            ballSpawnPoint.rotation
        );

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ball.SetActive(true);

        goalScored = false;
        respawnInProgress = false;
    }

    private void UpdateScore()
    {
        if (scoreText == null)
            return;

        if (gameWon)
            scoreText.text = winMessage;
        else
            scoreText.text = "Score: " + score;
    }
}
