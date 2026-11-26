using UnityEngine;
using UnityEngine.Events;

public class PadlockController : MonoBehaviour
{
    [Header("Wheel Settings")]

    // Assign the wheels in the inspector :D
    public Transform[] wheels;

    // The winning code digits
    public int[] correctCombination = { 7, 3, 5, 8}; 
    
    // Keeps track of each wheel's current digit
    private int[] currentValues;        

    [Header("Events")]
    // When the padlock is correct combination
    public UnityEvent onUnlocked;       

    void Start()
    {
        currentValues = new int[wheels.Length];
    }

    // When the player interacts with a wheel to turn it forward
    public void TurnWheelForward(int wheelIndex)
    {
        currentValues[wheelIndex] = (currentValues[wheelIndex] + 1) % 10;

        // Rotate the wheel visual by 36 degrees (360 degrees / 10 numbers = 36)
        wheels[wheelIndex].Rotate(Vector3.up, 36f, Space.Self);

        CheckCombination();
    }

    // When the player interacts with a wheel turn it backward
    public void TurnWheelBackward(int wheelIndex)
    {
        currentValues[wheelIndex] = (currentValues[wheelIndex] - 1 + 10) % 10;

        // Rotate back by 36 degrees (sine 360/10 is 36)
        wheels[wheelIndex].Rotate(Vector3.up, -36f, Space.Self);

        CheckCombination();
    }

    void CheckCombination()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            if (currentValues[i] != correctCombination[i])
            {
                return; // If it doesn't match the correct combination
            }
        }

        // Loop finishes if all wheels match the combination
        Debug.Log("Padlock Unlocked!");
        onUnlocked.Invoke();
    }
}