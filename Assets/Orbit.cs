using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform target;
    public float radius = 5f;
    public float speed = 50f;

    private float angle;

    void Update()
    {
        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        transform.position = target.position + new Vector3(x, 0, z);
    }
}
