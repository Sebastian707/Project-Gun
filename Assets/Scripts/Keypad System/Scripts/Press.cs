using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Collider))]
public class Press : MonoBehaviour
{
    [Header("Button Settings")]
    public string buttonValue;
    public EventReference ClickSound;

    [Header("Keypad Reference")]
    public KeypadManager keypadManager;

    [Header("Fingertip Detection")]
    [Tooltip("Tag set on the fingertip trigger colliders.")]
    public string fingertipTag = "Fingertip";

    [Tooltip("Minimum seconds between presses, to avoid double-firing.")]
    public float cooldown = 0.3f;

    private float lastPressTime = -10f;

    private void Reset()
    {
        // Make the button collider a trigger by default
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(fingertipTag))
            return;

        if (Time.time - lastPressTime < cooldown)
            return;

        lastPressTime = Time.time;
        Interact();
    }

    private void Interact()
    {
        keypadManager.ButtonPressed(buttonValue);

        if (!ClickSound.IsNull)
            RuntimeManager.PlayOneShot(ClickSound);
    }
}