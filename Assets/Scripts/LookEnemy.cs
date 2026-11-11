using UnityEngine;
using System.Linq;
using UnityEngine.AI;
public class EnemyScript : MonoBehaviour
{
    public float freezeTime = 10;
    //same if insta stop
    public float maxFrozenTime = 10;
    public float currFrozenTime;
    public GameObject player;

    public float freezeMulti = 2;
    private NavMeshAgent agent;
    //https://www.youtube.com/watch?v=38BtpQWEct4
    [SerializeField] private Camera playerCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currFrozenTime <= maxFrozenTime)
        {
            currFrozenTime += Time.deltaTime;
        }
        
        if (isVisible())
        {
            //this.GetComponent<Renderer>().material.color = Color.white;
            if (currFrozenTime >= 0) {
                currFrozenTime -= Time.deltaTime * freezeMulti;
            }
            
        }
        else
        {
            //this.GetComponent<Renderer>().material.color = Color.red;
        }
        if (currFrozenTime > freezeTime)
        {
            agent.SetDestination(player.transform.position);
            agent.isStopped = false;
        }
        else
        {
            agent.isStopped = true;
        }
    }
    private bool isVisible()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        if (planes.All(plane => plane.GetDistanceToPoint(this.transform.position) >= 0))
        {
            Physics.Raycast(this.transform.position, playerCamera.transform.position - this.transform.position, out RaycastHit hitInfo);
            if (hitInfo.collider.gameObject.tag == "player") return true;
            return false;
        }
        return false;
    }
}