using UnityEngine;

/// <summary>
/// Handles the "no animation, just snap between poses" look.
/// Easiest setup: Tools > Weeping Angel > Pose Generator builds the clips and assigns them here.
/// Two ways to supply poses:
///  1) AnimationClips: single-pose clips (we sample one frame of each).
///  2) ChildObjects:   duplicate the model as children, pose each one, and we toggle them on/off.
/// In clip mode the head also snaps to face the player on every pose change.
/// </summary>
public class AngelPoser : MonoBehaviour
{
    public enum PoseMode { AnimationClips, ChildObjects }

    public PoseMode mode = PoseMode.AnimationClips;

    [Header("Mode: AnimationClips")]
    [Tooltip("Object that holds the rig the clips animate (usually the model root). Defaults to this object.")]
    public GameObject rigRoot;
    public AnimationClip[] poseClips;
    [Tooltip("Time in seconds to sample from each clip.")]
    public float sampleTime = 0f;

    [Header("Mode: ChildObjects")]
    public GameObject[] poseObjects;

    [Header("General")]
    [Tooltip("Pick the next pose randomly instead of cycling in order (never repeats the same pose twice).")]
    public bool randomOrder = true;

    [Header("Head (clip mode only)")]
    [Tooltip("Snap the head around to stare at the target every time the pose changes.")]
    public bool headTracksTarget = true;
    [Tooltip("Max degrees the head may turn away from the body's facing. Over 90 = creepy owl neck.")]
    public float maxHeadTurn = 110f;

    /// <summary>
    /// Yaw (degrees) between this object's forward axis and the direction the model's body actually faces,
    /// measured from the hips at startup. WeepingAngel uses it so the angel walks face-first
    /// no matter how the FBX was imported.
    /// </summary>
    public float FacingYawOffset { get; private set; }

    int current = -1;
    Transform head, thighL, thighR, lastTarget;

    int PoseCount => mode == PoseMode.AnimationClips
        ? (poseClips != null ? poseClips.Length : 0)
        : (poseObjects != null ? poseObjects.Length : 0);

    void Awake()
    {
        if (rigRoot == null) rigRoot = gameObject;

        // An Animator would overwrite our sampled poses, so shut it off.
        foreach (var anim in GetComponentsInChildren<Animator>(true)) anim.enabled = false;

        // Make sure skinned meshes still render / report bounds correctly after sampling.
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "head") head = t;
            else if (t.name == "thigh.l") thighL = t;
            else if (t.name == "thigh.r") thighR = t;
        }

        // Measure which way the body faces relative to the root (before any pose is applied).
        if (thighL != null && thighR != null)
        {
            Vector3 right = thighR.position - thighL.position; right.y = 0f;
            Vector3 rootFwd = transform.forward; rootFwd.y = 0f;
            if (right.sqrMagnitude > 1e-6f && rootFwd.sqrMagnitude > 1e-6f)
            {
                Vector3 bodyFwd = Vector3.Cross(right.normalized, Vector3.up);
                FacingYawOffset = Vector3.SignedAngle(rootFwd, bodyFwd, Vector3.up);
            }
        }
        if (rigRoot != gameObject && poseClips != null && poseClips.Length > 0)
            Debug.LogWarning("[AngelPoser] rigRoot differs from this object; make sure clips were generated from the same hierarchy.", this);

        SetPose(5);
    }

    public void NextPose()
    {
        int count = PoseCount;
        if (count == 0) return;

        int next;
        if (randomOrder && count > 1)
        {
            do { next = Random.Range(0, count); } while (next == current);
        }
        else next = (current + 1) % count;

        SetPose(next);
    }

    /// <summary>Set who the head should stare at, and re-apply the current pose with that gaze.</summary>
    public void AimHeadAt(Transform target)
    {
        lastTarget = target;
        if (current >= 0) SetPose(current);
    }

    public void SetPose(int index)
    {
        int count = PoseCount;
        if (count == 0) return;
        index = Mathf.Clamp(index, 0, count - 1);
        current = index;

        if (mode == PoseMode.AnimationClips)
        {
            if (poseClips[index] != null)
                poseClips[index].SampleAnimation(rigRoot, sampleTime);
            ApplyHeadLook();
        }
        else
        {
            for (int i = 0; i < poseObjects.Length; i++)
                if (poseObjects[i] != null) poseObjects[i].SetActive(i == index);
        }
    }

    void ApplyHeadLook()
    {
        if (!headTracksTarget || lastTarget == null || head == null || thighL == null || thighR == null) return;

        // Body facing from the hips (works regardless of how the model root is rotated)
        Vector3 right = thighR.position - thighL.position; right.y = 0f;
        if (right.sqrMagnitude < 1e-6f) return;
        Vector3 fwd = Vector3.Cross(right.normalized, Vector3.up);

        Vector3 to = lastTarget.position - head.position; to.y = 0f;
        if (to.sqrMagnitude < 1e-6f) return;

        float yaw = Mathf.Clamp(Vector3.SignedAngle(fwd, to, Vector3.up), -maxHeadTurn, maxHeadTurn);
        head.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * head.rotation;
    }
}
