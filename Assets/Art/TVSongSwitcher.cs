using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// Plays ONE FMOD song and moves its 3D position to a new object when told to.
/// The song never restarts, it just keeps playing from the new location,
/// so your spatializer pans/attenuates it from wherever it now "is".
/// Put this on an object that is NEVER disabled (e.g. the TV root).
public class TVSongSwitcher : MonoBehaviour
{
    [Header("FMOD Song")]
    public EventReference song;
    public bool playOnStart = true;

    [Header("Position")]
    [Tooltip("Where the song starts (usually the TV Screen)")]
    public Transform startTarget;
    [Tooltip("Seconds to glide to the new location. 0 = jump instantly")]
    public float glideTime = 0.25f;

    EventInstance instance;
    Transform target;
    Vector3 position;
    Vector3 glideStart;
    float glideProgress = 1f;

    void Start()
    {
        target = startTarget ? startTarget : transform;
        position = target.position;

        if (playOnStart && !song.IsNull)
        {
            instance = RuntimeManager.CreateInstance(song);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
        }
    }

    /// Call this to move the song to another object. It keeps playing.
    public void MoveTo(Transform newTarget)
    {
        if (!newTarget || newTarget == target) return;

        target = newTarget;
        glideStart = position;
        glideProgress = glideTime <= 0f ? 1f : 0f;
    }

    void Update()
    {
        if (!target) return;

        if (glideProgress < 1f)
        {
            glideProgress += Time.deltaTime / glideTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(glideProgress));
            position = Vector3.Lerp(glideStart, target.position, k);
        }
        else
        {
            position = target.position; // also follows the target if it moves
        }

        if (instance.isValid())
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
    }

    /// Current song position in seconds. Returns false if the song isn't playing.
    public bool TryGetTime(out double seconds)
    {
        seconds = 0;
        if (!instance.isValid()) return false;

        instance.getPlaybackState(out PLAYBACK_STATE state);
        if (state != PLAYBACK_STATE.PLAYING) return false;

        instance.getTimelinePosition(out int ms);
        seconds = ms / 1000.0;
        return true;
    }

    void OnDestroy()
    {
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
        }
    }
}
