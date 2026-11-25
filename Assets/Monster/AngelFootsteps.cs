using UnityEngine;
using FMODUnity;

/// <summary>
/// Plays a single FMOD event every time the angel takes a step (pose change).
/// Requires FMOD for Unity 2.0+ (uses EventReference).
/// </summary>
[RequireComponent(typeof(WeepingAngel))]
public class AngelFootsteps : MonoBehaviour
{
    [Tooltip("Your footstep / stone-grind FMOD event.")]
    public EventReference stepEvent;

    WeepingAngel angel;

    void Awake() => angel = GetComponent<WeepingAngel>();
    void OnEnable() => angel.OnStep += PlayStep;
    void OnDisable() => angel.OnStep -= PlayStep;

    void PlayStep()
    {
        if (!stepEvent.IsNull)
            RuntimeManager.PlayOneShot(stepEvent, transform.position);
    }
}
