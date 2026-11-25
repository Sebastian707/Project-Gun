using UnityEngine;

/// <summary>
/// Generates an electrical hum / buzz in code (no audio files) whose volume,
/// pitch and crackle follow LightFlicker.Flicker01 sample-accurately.
/// Put on the same GameObject as an AudioSource (set Spatial Blend = 1 for 3D).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProceduralFlickerAudio : MonoBehaviour
{
    public LightFlicker flicker;

    [Header("Hum")]
    public float humFrequency = 100f;        // 100 Hz = 50 Hz mains rectified; use 120 for 60 Hz regions
    [Range(0f, 1f)] public float humVolume = 0.25f;
    [Range(0f, 1f)] public float harmonics = 0.5f;
    public float pitchDrop = 0.15f;          // how much pitch sags when the light dims

    [Header("Crackle / buzz")]
    [Range(0f, 1f)] public float noiseVolume = 0.15f;
    [Range(0f, 1f)] public float crackleOnFlicker = 0.6f; // extra noise when brightness is changing fast

    [Header("Dropout click")]
    [Range(0f, 1f)] public float clickVolume = 0.6f;

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("Smoothing (seconds) so it doesn't zipper. Keep small for tight sync.")]
    public float smoothTime = 0.004f;

    // Written on main thread, read on audio thread
    volatile float _target = 1f;
    volatile float _changeRate;
    volatile bool _clickPending;

    // Audio-thread state
    float _phase, _smoothed = 1f, _lastTarget = 1f;
    float _clickEnv, _noiseLP;
    int _sampleRate;
    System.Random _rng = new System.Random();

    void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        var src = GetComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = true;
        // OnAudioFilterRead only runs while the source is playing.
        // If you hear nothing, assign any looped (even silent) clip here.
        if (!src.isPlaying) src.Play();
    }

    void OnEnable()
    {
        if (flicker) flicker.OnDropoutChanged += OnDropout;
    }

    void OnDisable()
    {
        if (flicker) flicker.OnDropoutChanged -= OnDropout;
    }

    void OnDropout(bool starting)
    {
        _clickPending = true; // click on both start and end of a dropout
    }

    void Update()
    {
        if (!flicker) return;
        float v = flicker.Flicker01;
        _changeRate = Mathf.Abs(v - _target) / Mathf.Max(Time.deltaTime, 0.0001f);
        _target = v;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float target = _target;
        float changeAmount = Mathf.Clamp01(_changeRate * 0.05f);
        float smoothCoef = 1f - Mathf.Exp(-1f / (Mathf.Max(smoothTime, 0.0005f) * _sampleRate));

        if (_clickPending) { _clickEnv = 1f; _clickPending = false; }

        for (int i = 0; i < data.Length; i += channels)
        {
            _smoothed += (target - _smoothed) * smoothCoef;

            // Hum: fundamental + harmonics, pitch sags with brightness
            float freq = humFrequency * (1f - pitchDrop * (1f - _smoothed));
            _phase += freq / _sampleRate;
            if (_phase > 1f) _phase -= 1f;
            float p = _phase * Mathf.PI * 2f;
            float hum = Mathf.Sin(p)
                      + harmonics * 0.5f * Mathf.Sin(p * 2f)
                      + harmonics * 0.35f * Mathf.Sin(p * 3f)
                      + harmonics * 0.2f * Mathf.Sin(p * 5f);
            hum *= humVolume;

            // Buzz noise (low-passed white noise), louder while flicker is moving
            float white = (float)(_rng.NextDouble() * 2.0 - 1.0);
            _noiseLP += (white - _noiseLP) * 0.35f;
            float noise = _noiseLP * noiseVolume * (1f + crackleOnFlicker * changeAmount * 4f);

            // Click at dropout edges
            float click = 0f;
            if (_clickEnv > 0.0005f)
            {
                click = white * _clickEnv * clickVolume;
                _clickEnv *= 0.995f;
            }

            // Everything scaled by brightness: dark = (almost) silent
            float sample = ((hum + noise) * _smoothed + click) * masterVolume;

            for (int c = 0; c < channels; c++) data[i + c] = sample;
        }
    }
}
