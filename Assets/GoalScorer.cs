
using UnityEngine;
using TMPro;
using System.Collections;
using FMODUnity;

public class GoalScorer : MonoBehaviour
{
    [Header("Scoreboard")]
    public TMP_Text scoreText;
    public int pointsPerGoal = 1;

    [Header("Confetti")]
    public ParticleSystem confetti;

    [Header("FMOD Sound")]
    public EventReference goalSound;

    [Header("Ball Respawn")]
    public Transform ballSpawnPoint;
    public float respawnDelay = 1.5f;

    private int score = 0;
    private bool goalScored = false;

    private void Start()
    {
        UpdateScore();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (goalScored)
            return;

        if (!other.CompareTag("Ball"))
            return;

        goalScored = true;

        // Add points
        score += pointsPerGoal;
        UpdateScore();

        // Play confetti
        if (confetti != null)
        {
            confetti.Play();
        }

        // Play FMOD goal sound
        if (!goalSound.IsNull)
        {
            RuntimeManager.PlayOneShot(
                goalSound,
                transform.position
            );
        }

        // Stop ball physics
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Hide the ball
        other.gameObject.SetActive(false);

        // Respawn ball after delay
        StartCoroutine(RespawnBall(other.gameObject, rb));
    }

    private IEnumerator RespawnBall(GameObject ball, Rigidbody rb)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (ballSpawnPoint == null)
        {
            Debug.LogWarning(
                "Ball Spawn Point is not assigned!"
            );
            yield break;
        }

        ball.transform.SetPositionAndRotation(
            ballSpawnPoint.position,
            ballSpawnPoint.rotation
        );

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ball.SetActive(true);
        goalScored = false;
    }

    private void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
