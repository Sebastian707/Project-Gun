
using UnityEngine;
using FMODUnity;

public class RingSizeButton : MonoBehaviour
{
    [Header("Ring Settings")]
    public Transform ring;
    public float sizeMultiplier = 1.5f;

    [Header("Hand Detection")]
    public string handTag = "Hand";

    [Header("Cooldown")]
    public float pressCooldown = 2f;
    private float nextPressTime = 0f;

    [Header("FMOD Sound")]
    public EventReference resizeSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(handTag))
            return;

        // Check cooldown.
        if (Time.time < nextPressTime)
            return;

        if (ring == null)
        {
            Debug.LogWarning("Ring is not assigned!");
            return;
        }

        // Start cooldown.
        nextPressTime = Time.time + pressCooldown;

        // Enlarge the ring.
        ring.localScale *= sizeMultiplier;

        // Play FMOD sound.
        if (!resizeSound.IsNull)
        {
            RuntimeManager.PlayOneShot(
                resizeSound,
                ring.position
            );
        }
    }
}
