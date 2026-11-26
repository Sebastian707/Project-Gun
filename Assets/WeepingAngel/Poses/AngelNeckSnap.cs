using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using FMODUnity;

/// <summary>
/// VR-safe catch sequence for the angel:
///  1. Locks player movement (but NOT head tracking).
///  2. Plays the FMOD snap sound.
///  3. Rolls the whole XR rig 90 degrees around the head, so the world tilts like the neck broke
///     (the camera itself can't be rotated in VR, headset tracking overrides it).
///  4. Fades to black with a head-locked quad.
///  5. Shows a world-space "GAME OVER" text in front of you. Press a trigger/grip on either
///     controller to restart (it also restarts by itself after 'autoRestartAfter' seconds).
/// No Canvas, no OnGUI, no XR Interaction Toolkit needed. Add to the same object as WeepingAngel.
/// </summary>
[RequireComponent(typeof(WeepingAngel))]
public class AngelNeckSnap : MonoBehaviour
{
    [Header("Sound")]
    public EventReference snapEvent;

    [Header("Neck snap")]
    public float snapAngle = 90f;
    [Tooltip("Seconds the snap takes. Very short = violent.")]
    public float snapDuration = 0.06f;
    [Tooltip("Turn this off if the tilt makes testers sick: you get sound + black-out only.")]
    public bool tiltView = true;

    [Header("XR rig")]
    [Tooltip("The XR Origin / player rig that holds the camera. Empty = the camera's top-most parent.")]
    public Transform rigRoot;

    [Header("Player lock")]
    [Tooltip("Extra scripts to disable on catch. Locomotion, movement and CharacterController are found automatically.")]
    public Behaviour[] disableOnCatch;

    [Header("Game over")]
    public string title = "GAME OVER";
    public string subtitle = "Press trigger to restart";
    public float blackoutDelay = 1.2f;
    public float fadeDuration = 0.8f;
    [Tooltip("Restart automatically after this many seconds (0 = only on button press).")]
    public float autoRestartAfter = 12f;
    public float textDistance = 2f;

    WeepingAngel angel;
    bool triggered;
    Transform fadeQuad;
    Material fadeMat;

    static readonly string[] LockKeywords = { "Locomotion", "Move", "Movement", "Turn", "Teleport", "Climb", "Walk", "Fly", "Controller", "Look" };

    void Awake()
    {
        angel = GetComponent<WeepingAngel>();
        angel.onCaught.AddListener(OnCaught);
    }

    void OnDestroy()
    {
        if (angel != null) angel.onCaught.RemoveListener(OnCaught);
    }

    void OnCaught()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        Camera cam = angel.playerCamera != null ? angel.playerCamera : Camera.main;
        if (cam == null) { Restart(); yield break; }

        LockPlayer(cam);
        BuildFadeQuad(cam);

        if (!snapEvent.IsNull) RuntimeManager.PlayOneShot(snapEvent, cam.transform.position);

        // ---- roll the rig around the head ----
        Transform rig = rigRoot != null ? rigRoot : (cam.transform.root != cam.transform ? cam.transform.root : null);
        float rolled = 0f;
        if (tiltView && rig != null)
        {
            float side = Vector3.Dot(cam.transform.right, transform.position - cam.transform.position) >= 0f ? 1f : -1f;
            float target = -side * snapAngle;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.001f, snapDuration));
                float want = target * t;
                rig.RotateAround(cam.transform.position, cam.transform.forward, want - rolled);
                rolled = want;
                yield return null;
            }
        }

        yield return new WaitForSeconds(blackoutDelay);

        // ---- fade to black ----
        float f = 0f;
        while (f < 1f)
        {
            f += Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
            SetFade(f);
            yield return null;
        }
        SetFade(1f);

        // Screen is black: put the world upright again (nobody sees it).
        if (rig != null && Mathf.Abs(rolled) > 0.01f)
            rig.RotateAround(cam.transform.position, cam.transform.forward, -rolled);

        // ---- world-space game over text ----
        BuildGameOverText(cam);

        // ---- wait for restart ----
        float waited = 0f;
        yield return new WaitForSeconds(0.8f); // don't let a panicked trigger squeeze skip it
        while (true)
        {
            waited += Time.deltaTime;
            if (AnyControllerButton()) break;
            if (autoRestartAfter > 0f && waited >= autoRestartAfter) break;
            yield return null;
        }
        Restart();
    }

    // ------------------------------------------------------------------ helpers

    void LockPlayer(Camera cam)
    {
        if (disableOnCatch != null)
            foreach (var b in disableOnCatch) if (b != null) b.enabled = false;

        var list = new List<Component>();
        if (angel.player != null) list.AddRange(angel.player.GetComponentsInChildren<Component>(false));
        list.AddRange(cam.GetComponentsInParent<Component>(false));
        if (cam.transform.root != null) list.AddRange(cam.transform.root.GetComponentsInChildren<Component>(false));

        foreach (var c in list)
        {
            if (c == null || c == this || c == angel) continue;

            if (c is CharacterController cc) { cc.enabled = false; continue; }
            if (c is Rigidbody rb) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; continue; }

            var mb = c as MonoBehaviour;
            if (mb == null) continue;

            string typeName = mb.GetType().Name;
            string ns = mb.GetType().Namespace ?? "";

            // Never touch head tracking, audio or FMOD.
            if (typeName.Contains("TrackedPoseDriver") || typeName.Contains("XROrigin") ||
                typeName.Contains("CameraOffset") || typeName.Contains("Tracking")) continue;
            if (ns.StartsWith("FMOD")) continue;

            bool isXr = ns.Contains("XR") || ns.Contains("InputSystem");
            if (isXr)
            {
                // XR package: only disable things that actually move/turn the player.
                foreach (var k in LockKeywords)
                    if (typeName.Contains(k)) { mb.enabled = false; break; }
            }
            else
            {
                mb.enabled = false; // your own movement scripts
            }
        }
    }

    void BuildFadeQuad(Camera cam)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        go.name = "AngelFade";
        fadeQuad = go.transform;
        fadeQuad.SetParent(cam.transform, false);
        fadeQuad.localPosition = new Vector3(0f, 0f, cam.nearClipPlane + 0.05f);
        fadeQuad.localScale = new Vector3(4f, 4f, 1f);

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        fadeMat = new Material(sh);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = fadeMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        SetFade(0f);
    }

    void SetFade(float a)
    {
        if (fadeMat == null) return;
        var c = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
        fadeMat.color = c;
        if (fadeMat.HasProperty("_Color")) fadeMat.SetColor("_Color", c);
    }

    void BuildGameOverText(Camera cam)
    {
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 pos = cam.transform.position + fwd * textDistance;
        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);

        // Render the text above the black quad? The quad is head-locked and covers the view, so fade it out
        // slowly to reveal the text floating in a dark void: hide the world with a big black sphere instead.
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(dome.GetComponent<Collider>());
        dome.transform.position = cam.transform.position;
        dome.transform.localScale = Vector3.one * (textDistance * 3f);
        var dr = dome.GetComponent<MeshRenderer>();
        Shader sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        dr.material = new Material(sh) { color = Color.black };
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // Draw inside of the sphere: flip it
        dome.transform.localScale = new Vector3(-dome.transform.localScale.x, dome.transform.localScale.y, dome.transform.localScale.z);

        if (fadeQuad != null) Destroy(fadeQuad.gameObject);

        MakeText(title, pos + Vector3.up * 0.25f, rot, 0.06f, new Color(0.8f, 0.05f, 0.05f), 120);
        MakeText(subtitle, pos - Vector3.up * 0.15f, rot, 0.02f, Color.white, 80);
    }

    static void MakeText(string text, Vector3 pos, Quaternion rot, float charSize, Color color, int fontSize)
    {
        var go = new GameObject("AngelText");
        go.transform.SetPositionAndRotation(pos, rot);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = charSize;
        tm.fontSize = fontSize;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;

        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    static bool AnyControllerButton()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(CommonUsages.triggerButton, out bool trig) && trig) return true;
            if (d.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip) return true;
            if (d.TryGetFeatureValue(CommonUsages.primaryButton, out bool a) && a) return true;
        }
        return false;
    }

    void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}