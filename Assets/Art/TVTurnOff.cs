using System.Collections;
using UnityEngine;
using FMODUnity;

/// Put this on an object with a Collider (Is Trigger = ON).
/// When the player enters:
///   1. "TV Screen" collapses like an old CRT (line -> dot) and is disabled
///   2. "Object To Enable" is enabled and expands like a CRT turning on (dot -> line -> full)
public class TVTurnOff : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Objects")]
    [Tooltip("The TV screen that turns OFF with the effect")]
    public GameObject tvScreen;
    [Tooltip("The object that turns ON with the effect")]
    public GameObject objectToEnable;

    [Header("Trigger")]
    public string playerTag = "Player";
    [Tooltip("Reverse everything when the player leaves")]
    public bool restoreOnExit = false;

    [Header("Music (optional)")]
    [Tooltip("Moves the song's 3D position to the object that turns on")]
    public TVSongSwitcher songSwitcher;

    [Header("FMOD Sounds")]
    public EventReference turnOffSound;
    public EventReference turnOnSound;

    [Header("Effect")]
    [Tooltip("Which local axis is the screen's height / width")]
    public Axis heightAxis = Axis.Y;
    public Axis widthAxis = Axis.X;
    public float squashToLineTime = 0.12f;
    public float lineToDotTime = 0.08f;
    [Range(0.001f, 0.2f)] public float lineThickness = 0.02f;
    [Tooltip("Pause between the old object turning off and the new one turning on")]
    public float delayBetween = 0.1f;

    Vector3 tvScale;
    Vector3 newScale;
    Coroutine running;
    bool isOff;

    void Awake()
    {
        if (tvScreen) tvScale = tvScreen.transform.localScale;
        if (objectToEnable)
        {
            newScale = objectToEnable.transform.localScale;
            objectToEnable.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || isOff) return;
        Run(Swap(tvScreen, tvScale, objectToEnable, newScale, true));
        isOff = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!restoreOnExit || !other.CompareTag(playerTag) || !isOff) return;
        Run(Swap(objectToEnable, newScale, tvScreen, tvScale, false));
        isOff = false;
    }

    void Run(IEnumerator routine)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(routine);
    }

    // Turn one object off with the effect, then turn the other on with the effect
    IEnumerator Swap(GameObject off, Vector3 offScale, GameObject on, Vector3 onScale, bool forward)
    {
        if (off && off.activeSelf) yield return PlayOff(off, offScale);
        yield return new WaitForSeconds(delayBetween);
        if (songSwitcher)
        {
            songSwitcher.MoveTo(on ? on.transform : null);
        }
        if (on) yield return PlayOn(on, onScale);
    }

    // Full size -> thin line -> dot -> disabled
    IEnumerator PlayOff(GameObject obj, Vector3 original)
    {
        Transform t = obj.transform;
        t.localScale = original;
        int h = (int)heightAxis, w = (int)widthAxis;

        PlaySound(turnOffSound, t.position);

        yield return Scale(t, h, original[h], original[h] * lineThickness, squashToLineTime);
        yield return Scale(t, w, original[w], 0f, lineToDotTime);

        obj.SetActive(false);
        t.localScale = original; // reset so it's ready for next time
    }

    // Enabled as a dot -> thin line -> full size
    IEnumerator PlayOn(GameObject obj, Vector3 original)
    {
        Transform t = obj.transform;
        int h = (int)heightAxis, w = (int)widthAxis;

        Vector3 start = original;
        start[w] = 0f;
        start[h] = original[h] * lineThickness;
        t.localScale = start;
        obj.SetActive(true);

        PlaySound(turnOnSound, t.position);

        yield return Scale(t, w, 0f, original[w], lineToDotTime);
        yield return Scale(t, h, original[h] * lineThickness, original[h], squashToLineTime);
    }

    void PlaySound(EventReference sound, Vector3 position)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound, position);
    }

    IEnumerator Scale(Transform t, int axis, float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, time / duration);
            Vector3 s = t.localScale;
            s[axis] = Mathf.Lerp(from, to, k);
            t.localScale = s;
            yield return null;
        }
        Vector3 end = t.localScale;
        end[axis] = to;
        t.localScale = end;
    }
}
