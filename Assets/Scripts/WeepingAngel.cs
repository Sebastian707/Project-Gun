using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WeepingAngel : MonoBehaviour
{
    private NavMeshAgent enemy;
    public GameObject player;

    void Start()
    {
        enemy = GetComponent<NavMeshAgent>();
    }

    private void OnBecameVisible()
    {
        enemy.isStopped = true;
        enemy.ResetPath();

    }

    private void OnBecameInvisible()
    {
        enemy.isStopped = false;
        enemy.SetDestination(player.transform.position);
    }

}