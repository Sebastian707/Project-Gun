using UnityEngine;

public class RotateOnPuzzleSolved : MonoBehaviour
{

    [SerializeField] private Vector3 rotationAmount = new Vector3(0, 90, 0);

    public void RotateObject()
    {
        transform.Rotate(rotationAmount);
    }
}
