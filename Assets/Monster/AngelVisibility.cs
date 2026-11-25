using UnityEngine;

/// <summary>
/// Answers one question: "can this camera currently see any part of the angel,
/// with nothing (walls, props, etc.) in the way?"
/// Samples a 3x3x3 grid of points inside the angel's bounds. A point counts as seen if it is
/// inside the camera's view AND a line from the camera to it isn't blocked by an obstacle.
/// </summary>
public class AngelVisibility : MonoBehaviour
{
    [Tooltip("Layers that block line of sight (walls, doors, props). Do NOT include the Player layer.")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("Beyond this distance the angel is considered not seen (0 = unlimited).")]
    public float maxViewDistance = 0f;

    [Tooltip("How many sample points must be visible to count as 'seen'. 1 = even a hand counts.")]
    [Range(1, 27)] public int minVisiblePoints = 1;

    [Tooltip("Shrinks the sample grid inward so points on the very edge of the bounds don't count.")]
    [Range(0.5f, 1f)] public float boundsShrink = 0.9f;

    Renderer[] renderers;
    readonly Vector3[] points = new Vector3[27];

    void Awake() => renderers = GetComponentsInChildren<Renderer>(true);

    public bool IsSeenBy(Camera cam)
    {
        if (cam == null) return false;

        // Combine bounds of currently active renderers (so only the current pose counts)
        bool found = false;
        Bounds b = new Bounds();
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!found) return false;

        Vector3 camPos = cam.transform.position;
        if (maxViewDistance > 0f && Vector3.Distance(camPos, b.center) > maxViewDistance) return false;

        // Build sample grid
        int i = 0;
        Vector3 ext = b.extents * boundsShrink;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                    points[i++] = b.center + new Vector3(x * ext.x, y * ext.y, z * ext.z);

        int visible = 0;
        for (int p = 0; p < points.Length; p++)
        {
            // Inside the camera's view?
            Vector3 vp = cam.WorldToViewportPoint(points[p]);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;

            // Anything in the way?
            if (Physics.Linecast(camPos, points[p], out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Hitting the angel's own collider is fine (that means we see it)
                if (!hit.transform.IsChildOf(transform) && hit.transform != transform) continue;
            }

            if (++visible >= minVisiblePoints) return true;
        }
        return false;
    }
}
