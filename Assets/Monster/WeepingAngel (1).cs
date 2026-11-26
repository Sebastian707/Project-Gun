using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Weeping Angel brain.
///  - Dormant until the player sees it for the first time with a clear line of sight.
///  - After that it only moves while NOT being seen, and freezes the instant it is.
///  - Every time it covers 'stepDistance' it snaps to the next pose and fires OnStep.
/// Works with a NavMeshAgent if one is on the object, otherwise moves in a straight line.
/// </summary>
[RequireComponent(typeof(AngelVisibility))]
[RequireComponent(typeof(AngelPoser))]
public class WeepingAngel : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                 // Auto-found by "Player" tag if empty
    public Camera playerCamera;              // Auto-uses Camera.main if empty

    [Header("Movement")]
    public float speed = 5f;
    [Tooltip("Distance travelled between pose changes / footstep sounds.")]
    public float stepDistance = 1.2f;
    public float turnSpeed = 10f;
    [Tooltip("Ignore height difference when moving directly (no NavMesh).")]
    public bool lockYAxis = true;
    [Tooltip("Use NavMeshAgent for pathfinding if one is attached.")]
    public bool useNavMeshIfAvailable = true;

    [Header("Catching the player")]
    public float catchDistance = 1.3f;

    [Header("Events")]
    public UnityEvent onActivated;           // First time the player sees it
    public UnityEvent onCaught;              // Reached the player
    public event Action OnStep;              // Fired for each pose change (used by footsteps)

    public bool IsActivated { get; private set; }
    public bool IsSeen { get; private set; }

    AngelVisibility visibility;
    AngelPoser poser;
    NavMeshAgent agent;

    public float freezeTime = 10f;
    float currFreezeTime = 0f;
    public float decayMult = 2;
    public float glanceBonus = 3f;
    float travelled;
    Vector3 lastPos;
    bool caught;

    void Awake()
    {
        visibility = GetComponent<AngelVisibility>();
        poser = GetComponent<AngelPoser>();
        agent = useNavMeshIfAvailable ? GetComponent<NavMeshAgent>() : null;
        if (agent != null)
        {
            agent.speed = speed;
            agent.updateRotation = false;
            // Snap to full speed / hard stop: an angel never eases in or out.
            agent.acceleration = 1000f;
            agent.autoBraking = false;
        }
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (playerCamera == null) playerCamera = Camera.main;
        lastPos = transform.position;
        if (player != null) poser.AimHeadAt(player);
    }

    void Update()
    {
        //Debug.Log($"{currFreezeTime} | {freezeTime}");
        if (player == null || playerCamera == null || caught) return;
        bool lastSeenState = IsSeen;
        IsSeen = visibility.IsSeenBy(playerCamera);
        // if glanced at monster then add glancebonus :)
        if (lastSeenState != IsSeen & IsSeen)
        {
            currFreezeTime = (currFreezeTime - glanceBonus + freezeTime) % freezeTime;
            
        }
        // Dormant: wait for the first sighting
        if (!IsActivated)
        {
            if (IsSeen)
            {
                IsActivated = true;
                poser.AimHeadAt(player);   // snap its gaze onto you
                onActivated?.Invoke();
            }
            lastPos = transform.position;
            return;
        }
        if (IsSeen) {
            if (currFreezeTime > 0) currFreezeTime -= Time.deltaTime * decayMult;

        } else
        {
            if (currFreezeTime <= freezeTime) {
                currFreezeTime += Time.deltaTime;
            }
            
        }
        // Seen: freeze completely
        if (IsSeen)
        {
            Freeze();
            lastPos = transform.position;
            return;
        }

        // Unseen: move
        if (currFreezeTime < freezeTime) return;
        float flatDist = DistanceToPlayer();
        if (flatDist <= catchDistance)
        {
            caught = true;
            Freeze();
            onCaught?.Invoke();
            return;
        }

        if (agent != null && agent.isOnNavMesh) MoveWithAgent();
        else MoveDirect();

        // Count distance moved this frame and trigger pose swaps
        travelled += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;

        while (travelled >= stepDistance)
        {
            travelled -= stepDistance;
            poser.NextPose();
            OnStep?.Invoke();
        }
    }

    float DistanceToPlayer()
    {
        Vector3 d = player.position - transform.position;
        if (lockYAxis) d.y = 0f;
        return d.magnitude;
    }

    void MoveDirect()
    {
        Vector3 dir = player.position - transform.position;
        if (lockYAxis) dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.001f) return;
        dir /= dist;

        float move = Mathf.Min(speed * Time.deltaTime, dist - catchDistance * 0.5f);
        transform.position += dir * Mathf.Max(0f, move);
        Face(dir);
    }

    void MoveWithAgent()
    {
        agent.isStopped = false;
        agent.speed = speed;
        agent.SetDestination(player.position);

        Vector3 v = agent.desiredVelocity; v.y = 0f;
        if (v.sqrMagnitude > 0.01f) Face(v.normalized);
    }

    void Freeze()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        // Compensate for however the model's body is oriented relative to the root transform.
        Quaternion target = Quaternion.Euler(0f, -poser.FacingYawOffset, 0f) * Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
