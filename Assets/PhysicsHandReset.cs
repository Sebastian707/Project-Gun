using UnityEngine;

public class PhysicsHandReset : MonoBehaviour
{
    public Transform target;       // LeftHand
    public float maxDistance = 0.5f;
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        if (Vector3.Distance(rb.position, target.position) > maxDistance)
        {
            rb.position = target.position;
            rb.rotation = target.rotation;
            rb.linearVelocity = Vector3.zero;   // use rb.velocity on older Unity
            rb.angularVelocity = Vector3.zero;
        }
    }
}