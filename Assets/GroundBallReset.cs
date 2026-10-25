
using UnityEngine;

public class GroundBallReset : MonoBehaviour
{
    [Header("Ground Reset")]
    public string groundBallTag = "Ball";
    public GoalScorer goalScorer;

    private void OnCollisionEnter(Collision collision)
    {
        TryReset(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReset(other.gameObject);
    }

    private void TryReset(GameObject ball)
    {
        if (!ball.CompareTag(groundBallTag))
            return;

        if (goalScorer != null)
        {
            goalScorer.ResetBall(ball);
        }
        else
        {
            Debug.LogWarning(
                "Assign GoalScorer to GroundBallReset!"
            );
        }
    }
}
