using UnityEngine;
using UnityEngine.InputSystem;

public class FingertipToggle : MonoBehaviour
{
    public enum ActivationMode
    {
        Select,
        Activate
    }

    [Header("Target")]
    [Tooltip("The fingertip object to turn on/off. Don't put this script on the fingertip itself, or it will disable itself.")]
    public GameObject fingertip;

    [Header("Mode")]
    [Tooltip("Which input turns the fingertip on. The fingertip is only on when this input is pressed and the other one is NOT.")]
    public ActivationMode mode = ActivationMode.Select;

    [Header("Inputs (from the XRI controller actions)")]
    public InputActionReference selectAction;
    public InputActionReference activateAction;

    private void OnEnable()
    {
        // Safe to call even if something else already enabled them
        if (selectAction != null) selectAction.action.Enable();
        if (activateAction != null) activateAction.action.Enable();
    }

    private void Update()
    {
        if (fingertip == null)
            return;

        bool selectPressed = selectAction != null && selectAction.action.IsPressed();
        bool activatePressed = activateAction != null && activateAction.action.IsPressed();

        bool shouldBeOn = mode == ActivationMode.Select
            ? selectPressed && !activatePressed
            : activatePressed && !selectPressed;

        if (fingertip.activeSelf != shouldBeOn)
            fingertip.SetActive(shouldBeOn);
    }

    private void OnDisable()
    {
        if (fingertip != null)
            fingertip.SetActive(false);
    }
}