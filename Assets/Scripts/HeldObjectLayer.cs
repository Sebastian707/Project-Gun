using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class HeldObjectLayer : MonoBehaviour
{
    public string heldLayerName = "HeldObject";
    public float restoreDelay = 0.3f;

    XRGrabInteractable grab;
    Collider[] cols;
    int[] originalLayers;
    int heldLayer;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        heldLayer = LayerMask.NameToLayer(heldLayerName);
        cols = GetComponentsInChildren<Collider>();
        originalLayers = new int[cols.Length];
        for (int i = 0; i < cols.Length; i++)
            originalLayers[i] = cols[i].gameObject.layer;
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        StopAllCoroutines();
        foreach (var c in cols) c.gameObject.layer = heldLayer;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        StartCoroutine(Restore());
    }

    IEnumerator Restore()
    {
        yield return new WaitForSeconds(restoreDelay);
        for (int i = 0; i < cols.Length; i++)
            cols[i].gameObject.layer = originalLayers[i];
    }
}