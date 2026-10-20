
using UnityEngine;

public class MovingGoal : MonoBehaviour
{
    public Vector3 localDirection = Vector3.right;
    public float distance = 1.5f;
    public float speed = 1.5f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float offset =
            Mathf.Sin(Time.time * speed) * distance;

        transform.localPosition =
            startPosition + localDirection.normalized * offset;
    }
}
