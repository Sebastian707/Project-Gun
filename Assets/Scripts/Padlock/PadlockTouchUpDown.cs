using UnityEngine;

public class PadlockTouchUpDown : MonoBehaviour
{
    public PadlockController padlock;
    public int wheelIndex = 0;
    public bool turnForward = true;
    public float cooldown = 0.25f; // Prevents one touch from firing 10 times in a row

    private float lastTriggerTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (padlock == null) return;
        if (Time.time - lastTriggerTime < cooldown) return;

        // Optional: check if the object touching has a specific tag or name, e.g. "PlayerHand"
        lastTriggerTime = Time.time;

        if (turnForward)
        {
            padlock.TurnWheelForward(wheelIndex);
        }
        else
        {
            padlock.TurnWheelBackward(wheelIndex);
        }
    }
}