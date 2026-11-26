
using UnityEngine;
using FMODUnity;

public class TriggerOn : MonoBehaviour
{
    public GameObject objectToTurnOn;
    public EventReference soundEvent;

    private void OnTriggerEnter(Collider other)
    {
        objectToTurnOn.SetActive(true);
        RuntimeManager.PlayOneShot(soundEvent, transform.position);
        gameObject.SetActive(false);
    }
}
