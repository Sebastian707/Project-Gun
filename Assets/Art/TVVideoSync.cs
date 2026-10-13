using UnityEngine;
using UnityEngine.Video;

/// Keeps a Unity VideoPlayer in sync with the FMOD song from TVSongSwitcher.
/// FMOD is the master clock: the video follows it. Small drift is corrected by
/// nudging playback speed (invisible), big drift (or the song looping) by seeking.
/// Put this on the same object as the VideoPlayer, which must NEVER be disabled
/// (don't put it on the TV Screen, that object gets turned off).
[RequireComponent(typeof(VideoPlayer))]
public class TVVideoSync : MonoBehaviour
{
    public TVSongSwitcher song;

    [Tooltip("Seek if video is off by more than this many seconds")]
    public float hardSeekThreshold = 0.3f;
    [Tooltip("Ignore drift smaller than this many seconds")]
    public float deadZone = 0.03f;
    [Tooltip("Seconds to subtract if the video appears slightly ahead/behind of the audio")]
    public float manualOffset = 0f;

    VideoPlayer video;
    bool started;

    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.skipOnDrop = true;
        video.audioOutputMode = VideoAudioOutputMode.None; // FMOD provides the sound
        video.Prepare();
    }

    void Update()
    {
        if (!song || !video.isPrepared) return;

        if (!song.TryGetTime(out double songTime))
        {
            if (video.isPlaying) video.Pause();
            return;
        }

        double target = songTime + manualOffset;

        // Start the video once the song is running
        if (!started)
        {
            video.time = target;
            video.Play();
            started = true;
            return;
        }

        if (!video.isPlaying) video.Play();

        double drift = target - video.time; // positive = video is behind

        if (System.Math.Abs(drift) > hardSeekThreshold)
        {
            video.playbackSpeed = 1f;
            video.time = target % video.length; // seek (also handles song loops)
        }
        else if (System.Math.Abs(drift) > deadZone)
        {
            // Gently speed up / slow down to catch up
            video.playbackSpeed = 1f + Mathf.Clamp((float)drift * 0.5f, -0.05f, 0.05f);
        }
        else
        {
            video.playbackSpeed = 1f;
        }
    }
}
