using UnityEngine;
using UnityEngine.Events;

public class PadlockController : MonoBehaviour
{
    [Header("Wheel Settings")]
    public Transform[] wheels;          // Assign your 3 wheels here in the Inspector
    public int[] correctCombination = { 7, 3, 5 }; // The winning code digits
    private int[] currentValues;        // Keeps track of each wheel's current digit (0-9)

    [Header("Events")]
    public UnityEvent onUnlocked;       // What happens when solved (e.g., open door)

    void Start()
    {
        currentValues = new int[wheels.Length];
    }

    // Call this function when the player interacts with a wheel to turn it forward
    public void TurnWheelForward(int wheelIndex)
    {
        currentValues[wheelIndex] = (currentValues[wheelIndex] + 1) % 10;

        // Rotate the wheel visual by 36 degrees (360 degrees / 10 numbers = 36)
        wheels[wheelIndex].Rotate(Vector3.right, 36f);

        CheckCombination();
    }

    // Call this function when the player interacts with a wheel to turn it backward
    public void TurnWheelBackward(int wheelIndex)
    {
        currentValues[wheelIndex] = (currentValues[wheelIndex] - 1 + 10) % 10;

        // Rotate back by 36 degrees
        wheels[wheelIndex].Rotate(Vector3.right, -36f);

        CheckCombination();
    }

    void CheckCombination()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            if (currentValues[i] != correctCombination[i])
            {
                return; // Exit if any wheel doesn't match
            }
        }

        // If the loop finishes, all wheels match!
        Debug.Log("Padlock Unlocked!");
        onUnlocked.Invoke();
    }
}