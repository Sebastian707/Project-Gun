using System;
using UnityEngine;

/// <summary>
/// Flickers a Light (and optionally an emissive material) and exposes the
/// current flicker value so audio can follow it exactly.
/// Flicker01: 0 = fully dark, 1 = full brightness.
/// </summary>
[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    public enum FlickerMode { SmoothNoise, BrokenBulb }

    [Header("Targets")]
    public Light targetLight;
    [Tooltip("Optional: renderer with an emissive material to flicker too.")]
    public Renderer emissiveRenderer;
    public Color emissionColor = Color.white;
    public float emissionMultiplier = 2f;

    [Header("Flicker")]
    public FlickerMode mode = FlickerMode.BrokenBulb;
    public float baseIntensity = 2f;
    [Range(0f, 1f)] public float minBrightness = 0.2f;
    public float noiseSpeed = 12f;

    [Header("Broken bulb mode")]
    [Tooltip("Chance per second that a dropout (near-black) burst begins.")]
    public float dropoutsPerSecond = 0.8f;
    public Vector2 dropoutDuration = new Vector2(0.03f, 0.2f);
    [Range(0f, 1f)] public float dropoutBrightness = 0.02f;

    /// <summary>Current normalised brightness 0..1 (use this to drive audio).</summary>
    public float Flicker01 { get; private set; } = 1f;

    /// <summary>True while a dropout is happening.</summary>
    public bool InDropout { get; private set; }

    /// <summary>Fired when a dropout starts (true) or ends (false). Good for click/zap one-shots.</summary>
    public event Action<bool> OnDropoutChanged;

    float _seed;
    float _dropoutTimer;
    MaterialPropertyBlock _mpb;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (!targetLight) targetLight = GetComponent<Light>();
        _seed = UnityEngine.Random.value * 100f;
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        float value = mode == FlickerMode.SmoothNoise ? SmoothNoise() : BrokenBulb();
        Flicker01 = Mathf.Clamp01(value);

        if (targetLight) targetLight.intensity = baseIntensity * Flicker01;

        if (emissiveRenderer)
        {
            emissiveRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionId, emissionColor * (Flicker01 * emissionMultiplier));
            emissiveRenderer.SetPropertyBlock(_mpb);
        }
    }

    float SmoothNoise()
    {
        float n = Mathf.PerlinNoise(_seed, Time.time * noiseSpeed);
        return Mathf.Lerp(minBrightness, 1f, n);
    }

    float BrokenBulb()
    {
        // Start / end dropouts
        if (InDropout)
        {
            _dropoutTimer -= Time.deltaTime;
            if (_dropoutTimer <= 0f) SetDropout(false);
        }
        else if (UnityEngine.Random.value < dropoutsPerSecond * Time.deltaTime)
        {
            _dropoutTimer = UnityEngine.Random.Range(dropoutDuration.x, dropoutDuration.y);
            SetDropout(true);
        }

        if (InDropout) return dropoutBrightness;
        return SmoothNoise();
    }

    void SetDropout(bool state)
    {
        InDropout = state;
        OnDropoutChanged?.Invoke(state);
    }
}
