using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using FMODUnity;

[RequireComponent(typeof(XRSocketInteractor))]
public class FuseBox : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string boolName = "IsDoorOpen";
    [SerializeField] private string requiredTag = "Fuse";

    [Header("Audio")]
    [SerializeField] private EventReference socketClickSound;
    [SerializeField] private EventReference doorOpenSound;

    private XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnFusePlaced);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnFusePlaced);
    }

    private void OnFusePlaced(SelectEnterEventArgs args)
    {
        // Not the fuse? Do nothing.
        if (!args.interactableObject.transform.CompareTag(requiredTag))
            return;

        StartCoroutine(LockFuse(args.interactableObject.transform));

        // Click sound from the socket
        if (!socketClickSound.IsNull)
            RuntimeManager.PlayOneShot(socketClickSound, transform.position);

        // Open the door
        doorAnimator.SetBool(boolName, true);

        // Door sound from the door object
        if (!doorOpenSound.IsNull)
            RuntimeManager.PlayOneShotAttached(doorOpenSound, doorAnimator.gameObject);
    }

    private IEnumerator LockFuse(Transform fuse)
    {
        yield return null;

        Rigidbody rb = fuse.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider c in fuse.GetComponentsInChildren<Collider>())
            c.enabled = false;
    }
}