#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Weeping Angel > Pose Generator
/// Builds 8 creepy "broken puppet" pose clips for the SKM_Robot3 skeleton and assigns them to an AngelPoser.
/// Poses are defined in CHARACTER space (not bone-local axes), so they work no matter how the FBX was imported.
/// </summary>
public class AngelPoseGenerator : EditorWindow
{
    GameObject model;
    AngelPoser targetPoser;
    string outputFolder = "Assets/WeepingAngel/Poses";
    [Range(0.5f, 1.6f)] float exaggeration = 1f;
    bool invertFingerCurl;

    // ---- working state ----
    Transform root;
    Dictionary<string, Transform> bones;
    Dictionary<Transform, KeyValuePair<Vector3, Quaternion>> bind;
    Transform[] allTransforms;
    Quaternion charRot;
    Vector3 charUp;
    float bindFootLow, bodyH, ex;
    System.Random rng;

    static readonly string[] Required =
    {
        "pelvis","spine_01","spine_05","head","thigh_l","thigh_r","calf_l","calf_r","foot_l","foot_r","ball_l","ball_r",
        "upperarm_l","upperarm_r","lowerarm_l","lowerarm_r","hand_l","hand_r"
    };

    static readonly string[] PoseNames =
    {
        "00_Vigil","01_Lurch","02_Strung","03_Hunch","04_Snapped","05_Reach","06_Dangling","07_Lunge"
    };

    [MenuItem("Tools/Weeping Angel/Pose Generator")]
    static void Open() => GetWindow<AngelPoseGenerator>("Angel Poses");

    void OnEnable() => PickUpSelection();
    void OnSelectionChange() { PickUpSelection(); Repaint(); }

    void PickUpSelection()
    {
        if (Selection.activeGameObject == null) return;
        var p = Selection.activeGameObject.GetComponent<AngelPoser>();
        if (p != null) targetPoser = p;
    }

    // One click: add the angel components to the selected model root, then generate poses.
    void SetUpSelectedAndGenerate()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null || EditorUtility.IsPersistent(go))
        {
            EditorUtility.DisplayDialog("Angel Poses", "Select the model's ROOT object in the scene Hierarchy first.", "OK");
            return;
        }
        var angel = go.GetComponent<WeepingAngel>();
        if (angel == null) angel = Undo.AddComponent<WeepingAngel>(go); // also adds AngelVisibility + AngelPoser
        targetPoser = go.GetComponent<AngelPoser>();
        Generate();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Quick way: put the robot model in your scene, select its ROOT object in the Hierarchy and click " +
            "'Set Up Selected & Generate'. That adds the angel scripts and builds + assigns the poses.\n\n" +
            "Manual way: add WeepingAngel to the root yourself, drag it into 'Angel Poser' and click Generate.",
            MessageType.Info);

        if (GUILayout.Button("Set Up Selected & Generate", GUILayout.Height(32))) SetUpSelectedAndGenerate();
        GUILayout.Space(8);

        targetPoser = (AngelPoser)EditorGUILayout.ObjectField("Angel Poser", targetPoser, typeof(AngelPoser), true);
        model = (GameObject)EditorGUILayout.ObjectField("Model (optional)", model, typeof(GameObject), true);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        exaggeration = EditorGUILayout.Slider("Creepiness / Exaggeration", exaggeration, 0.5f, 1.6f);
        invertFingerCurl = EditorGUILayout.Toggle("Invert Finger Curl", invertFingerCurl);
        EditorGUILayout.LabelField("Fingers curling backwards? Tick 'Invert Finger Curl' and regenerate.", EditorStyles.miniLabel);

        GUILayout.Space(8);
        if (GUILayout.Button("Generate Creepy Poses", GUILayout.Height(32))) Generate();
    }

    // ------------------------------------------------------------------ generation

    void Generate()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Angel Poses", "Stop Play mode first. Poses must be generated from the un-posed model in Edit mode.", "OK");
            return;
        }
        GameObject src = model != null ? model : (targetPoser != null ? targetPoser.gameObject : null);
        if (src == null) { EditorUtility.DisplayDialog("Angel Poses", "Assign an Angel Poser or a Model first.", "OK"); return; }

        GameObject inst = Instantiate(src);
        inst.transform.SetParent(null);
        inst.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            root = inst.transform;
            bones = new Dictionary<string, Transform>();
            allTransforms = inst.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms) if (!bones.ContainsKey(t.name)) bones[t.name] = t;

            var missing = new List<string>();
            foreach (var n in Required) if (!bones.ContainsKey(n)) missing.Add(n);
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog("Angel Poses", "Missing bones: " + string.Join(", ", missing), "OK");
                return;
            }

            bind = new Dictionary<Transform, KeyValuePair<Vector3, Quaternion>>();
            foreach (var t in allTransforms)
                bind[t] = new KeyValuePair<Vector3, Quaternion>(t.localPosition, t.localRotation);

            ComputeFrame();
            bodyH = Vector3.Distance(T("head").position, T("pelvis").position);
            bindFootLow = LowestFoot();
            ex = exaggeration;

            EnsureFolder(outputFolder);
            var clips = new AnimationClip[PoseNames.Length];

            for (int i = 0; i < PoseNames.Length; i++)
            {
                Restore();
                rng = new System.Random(1000 + i * 17);
                BuildPose(i);
                Ground();

                AnimationClip clip = Record("AngelPose_" + PoseNames[i]);
                string path = outputFolder + "/" + clip.name + ".anim";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(clip, path);
                clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
            AssetDatabase.SaveAssets();

            if (targetPoser != null)
            {
                Undo.RecordObject(targetPoser, "Assign angel poses");
                targetPoser.mode = AngelPoser.PoseMode.AnimationClips;
                targetPoser.poseClips = clips;
                targetPoser.randomOrder = true;
                EditorUtility.SetDirty(targetPoser);
                if (targetPoser.gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetPoser.gameObject.scene);
            }
            Debug.Log("[WeepingAngel] Generated " + clips.Length + " poses in " + outputFolder);
        }
        finally
        {
            DestroyImmediate(inst);
        }
    }

    // ------------------------------------------------------------------ the poses

    void BuildPose(int i)
    {
        switch (i)
        {
            case 0: // VIGIL - upright, head cocked, hands limp. The "resting" pose.
                Rot(T("pelvis"), 0, 0, 2);
                Spine(-2, 0, 3);
                Neck(10, 0, 22);
                Arm(false, V(0.15f,-1,0),      V(0.10f,-1,0.10f),  V(0.10f,-1,0.30f), 0.5f);
                Arm(true,  V(0.35f,-1,-0.05f), V(0.30f,-1,0.15f),  V(0.20f,-0.8f,0.6f), 0.7f);
                Leg(false, V(0.04f,-1,0.02f),  V(0.02f,-1,0),      V(-0.10f,-0.2f,1));
                Leg(true,  V(0.03f,-1,0),      V(0.03f,-1,-0.02f), V(-0.12f,-0.2f,1));
                break;

            case 1: // LURCH - lunging forward, one arm clawing out, back foot dragging
                Rot(T("pelvis"), 0, 8, -4);
                Spine(14, -6, 6);
                Neck(-8, 0, -28);
                Arm(false, V(0.45f,-0.55f,0.55f), V(0.20f,-0.20f,0.95f), V(0.10f,-0.7f,0.7f), 0.9f);
                Arm(true,  V(0.20f,-0.95f,0.35f), V(0.10f,-0.80f,0.60f), V(0,-1,0.1f), 0.3f);
                Leg(true,  V(0.05f,-0.85f,0.50f), V(0.05f,-0.95f,-0.25f), V(-0.05f,-0.15f,1));
                Leg(false, V(0.08f,-0.95f,-0.25f), V(0.05f,-0.80f,-0.60f), V(0.10f,-0.7f,0.7f));
                Shift(0.04f);
                break;

            case 2: // STRUNG - marionette: shoulders hiked, wrists pulled up, hands dangling, head jerked back
                Spine(-4, 0, 0);
                Neck(-18, 10, 8);
                Shrug(false, 8); Shrug(true, 14);
                Arm(false, V(0.30f,-0.95f,0.10f), V(0.15f,-0.10f,1), V(0,-1,0.2f), 0.15f);
                Arm(true,  V(0.50f,-0.80f,0.05f), V(0.30f,0.35f,0.90f), V(0.10f,-0.9f,0.3f), 0.2f);
                Leg(false, V(0.20f,-0.95f,0.20f), V(0.10f,-1,-0.10f), V(0.50f,-0.2f,0.85f));
                Leg(true,  V(0.10f,-1,0.25f),     V(0.05f,-0.95f,-0.20f), V(-0.30f,-0.1f,1));
                break;

            case 3: // HUNCH - deep stoop, head craned up to stare, arms dangling long
                Rot(T("pelvis"), -6, 0, 3);
                Spine(30, 0, -5);
                Neck(-26, 0, 12);
                Arm(false, V(0.20f,-0.95f,0.30f), V(0.10f,-0.90f,0.45f), V(0,-1,0.1f), 0.8f);
                Arm(true,  V(0.25f,-0.92f,0.35f), V(0.12f,-0.85f,0.50f), V(0.05f,-1,0.05f), 0.95f);
                Leg(false, V(0.15f,-0.92f,0.35f), V(0.05f,-1,-0.10f), V(0,-0.2f,1));
                Leg(true,  V(0.10f,-0.90f,0.45f), V(0.05f,-1,-0.15f), V(0,-0.2f,1));
                break;

            case 4: // SNAPPED - neck bent at a wrong angle, one claw held to chest, one leg splayed
                Rot(T("pelvis"), 0, -6, 4);
                Spine(4, 0, -14);
                Neck(5, 20, 55);
                Shrug(true, 10);
                Arm(true,  V(0.35f,-0.70f,0.30f), V(-0.20f,0.35f,0.90f), V(-0.10f,0.40f,0.90f), 1.0f);
                Arm(false, V(0.10f,-1,-0.15f),    V(0.05f,-1,-0.10f),    V(0,-1,0), 0.2f);
                Leg(true,  V(0,-1,0),            V(0,-1,0),           V(0,-0.2f,1));
                Leg(false, V(0.45f,-0.90f,0.10f), V(0.20f,-1,-0.10f), V(0.60f,-0.2f,0.80f));
                Shift(-0.04f);
                break;

            case 5: // REACH - both arms grasping toward you
                Spine(16, 0, 0);
                Neck(-10, 0, -10);
                Arm(false, V(0.60f,-0.30f,0.70f), V(0.40f,0.05f,0.90f), V(0.30f,0,0.95f), 0.6f);
                Arm(true,  V(0.50f,-0.50f,0.60f), V(0.20f,-0.25f,0.95f), V(0.15f,-0.6f,0.8f), 1.0f);
                Leg(true,  V(0.10f,-0.88f,0.45f), V(0.05f,-0.97f,-0.20f), V(0,-0.15f,1));
                Leg(false, V(0.05f,-0.95f,-0.30f), V(0.03f,-0.90f,-0.45f), V(0,-0.5f,0.85f));
                break;

            case 6: // DANGLING - head dropped sideways, elbow bent the wrong way, foot turned inward
                Rot(T("pelvis"), 0, 0, -5);
                Spine(8, 12, 10);
                Neck(22, -15, 40);
                Arm(false, V(0.25f,-0.95f,-0.10f), V(0.50f,-0.70f,0.40f), V(0.40f,-0.90f,0.10f), 0.1f);
                Arm(true,  V(0.15f,-1,0.05f),       V(0.10f,-1,0.20f),    V(0,-1,0.1f), 0.6f);
                Leg(true,  V(-0.05f,-1,0.10f), V(-0.10f,-1,0), V(-0.70f,-0.15f,0.70f));
                Leg(false, V(0.20f,-0.95f,0.10f), V(0.15f,-1,0.05f), V(0.20f,-0.2f,1));
                Shift(0.05f);
                break;

            case 7: // LUNGE - wide stance, claws raised. The "you're about to die" pose.
                Spine(24, 0, 0);
                Neck(-22, 0, 8);
                Arm(false, V(0.80f,-0.20f,0.55f), V(0.50f,0.35f,0.80f), V(0.30f,0.50f,0.80f), 0.75f);
                Arm(true,  V(0.80f,-0.15f,0.50f), V(0.60f,0.30f,0.75f), V(0.40f,0.40f,0.80f), 0.75f);
                Leg(false, V(0.55f,-0.80f,0.30f), V(0.25f,-1,-0.10f), V(0.30f,-0.15f,1));
                Leg(true,  V(0.40f,-0.85f,0.45f), V(0.10f,-1,-0.10f), V(0.10f,-0.15f,1));
                break;
        }
    }

    // ------------------------------------------------------------------ pose helpers

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    Transform T(string n) { bones.TryGetValue(n, out var t); return t; }

    // Derive a character-space frame from the skeleton itself (works for any import axes).
    void ComputeFrame()
    {
        Vector3 right = (T("thigh_r").position - T("thigh_l").position).normalized;
        Vector3 up = Vector3.ProjectOnPlane(T("head").position - T("pelvis").position, right).normalized;
        Vector3 fwd = Vector3.Cross(right, up);
        charRot = Quaternion.LookRotation(fwd, up);
        charUp = charRot * Vector3.up;
    }

    // Rotate a bone by Euler angles expressed in character space.
    // +pitch leans an upright bone forward, +yaw turns right, +roll tilts toward character-left.
    void Rot(Transform t, float pitch, float yaw, float roll)
    {
        if (t == null) return;
        Quaternion d = charRot * Quaternion.Euler(pitch, yaw, roll) * Quaternion.Inverse(charRot);
        t.rotation = d * t.rotation;
    }

    void Chain(string[] names, float[] w, float p, float y, float r)
    {
        for (int i = 0; i < names.Length; i++)
            Rot(T(names[i]), p * w[i] * ex, y * w[i] * ex, r * w[i] * ex);
    }

    static readonly string[] SpineBones = { "spine_01", "spine_02", "spine_03", "spine_04", "spine_05" };
    static readonly float[] SpineW = { 0.15f, 0.20f, 0.25f, 0.20f, 0.20f };
    static readonly string[] NeckBones = { "neck_01", "neck_02", "head" };
    static readonly float[] NeckW = { 0.30f, 0.30f, 0.40f };

    void Spine(float p, float y, float r) => Chain(SpineBones, SpineW, p, y, r);
    void Neck(float p, float y, float r) => Chain(NeckBones, NeckW, p, y, r);

    // Raise a shoulder (positive degrees = up)
    void Shrug(bool left, float deg) => Rot(T(left ? "clavicle_l" : "clavicle_r"), 0, 0, (left ? -deg : deg) * ex);

    // Sideways weight shift of the hips, as a fraction of torso height (+ = toward character-right)
    void Shift(float frac)
    {
        Transform p = T("pelvis");
        p.position += charRot * Vector3.right * (frac * bodyH);
    }

    // Point a bone at a direction (character space). x is OUTWARD-positive for both sides.
    void Aim(Transform bone, Transform child, Vector3 dirChar, bool left)
    {
        if (bone == null || child == null) return;
        if (left) dirChar.x = -dirChar.x;
        Vector3 cur = (child.position - bone.position).normalized;
        Vector3 tgt = (charRot * dirChar).normalized;
        bone.rotation = Quaternion.FromToRotation(cur, tgt) * bone.rotation;
    }

    void Arm(bool left, Vector3 upper, Vector3 lower, Vector3 hand, float curl)
    {
        string s = left ? "_l" : "_r";
        Aim(T("upperarm" + s), T("lowerarm" + s), upper, left);
        Aim(T("lowerarm" + s), T("hand" + s), lower, left);
        Aim(T("hand" + s), T("middle_01" + s), hand, left);
        Curl(left, curl);
    }

    void Leg(bool left, Vector3 thigh, Vector3 calf, Vector3 foot)
    {
        string s = left ? "_l" : "_r";
        Aim(T("thigh" + s), T("calf" + s), thigh, left);
        Aim(T("calf" + s), T("foot" + s), calf, left);
        Aim(T("foot" + s), T("ball" + s), foot, left);
    }

    // Curl the fingers about the axis running across the knuckles. Each finger gets a little random
    // variation so no two fingers match - that's what makes the hands look wrong.
    void Curl(bool left, float curl)
    {
        string s = left ? "_l" : "_r";
        Transform idx = T("index_01" + s), pnk = T("pinky_01" + s);
        if (idx == null || pnk == null) return;

        Vector3 axis = (pnk.position - idx.position).normalized;
        float sign = (left ? -1f : 1f) * (invertFingerCurl ? -1f : 1f);

        string[] fingers = { "index", "middle", "ring", "pinky" };
        float[] baseAng = { 35f, 50f, 35f };
        foreach (var f in fingers)
        {
            float jitter = 0.65f + (float)rng.NextDouble() * 0.7f;
            for (int k = 1; k <= 3; k++)
            {
                Transform b = T(f + "_0" + k + s);
                if (b == null) continue;
                float ang = baseAng[k - 1] * curl * jitter * ex;
                b.rotation = Quaternion.AngleAxis(sign * ang, axis) * b.rotation;
            }
        }
        float[] thumbAng = { 8f, 20f, 25f };
        for (int k = 1; k <= 3; k++)
        {
            Transform b = T("thumb_0" + k + s);
            if (b == null) continue;
            b.rotation = Quaternion.AngleAxis(sign * thumbAng[k - 1] * curl * ex, axis) * b.rotation;
        }
    }

    float LowestFoot()
    {
        float low = float.MaxValue;
        foreach (var n in new[] { "foot_l", "foot_r", "ball_l", "ball_r" })
        {
            Transform t = T(n);
            if (t != null) low = Mathf.Min(low, Vector3.Dot(t.position, charUp));
        }
        return low;
    }

    // Keep the lowest foot at its original height so the angel never floats or sinks.
    void Ground()
    {
        float delta = bindFootLow - LowestFoot();
        T("pelvis").position += charUp * delta;
    }

    void Restore()
    {
        foreach (var kv in bind)
        {
            kv.Key.localPosition = kv.Value.Key;
            kv.Key.localRotation = kv.Value.Value;
        }
    }

    // ------------------------------------------------------------------ clip writing

    // Writes EVERY transform so each pose fully overwrites the previous one when sampled.
    AnimationClip Record(string name)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        foreach (var t in allTransforms)
        {
            if (t == root) continue;
            string path = AnimationUtility.CalculateTransformPath(t, root);
            Quaternion q = t.localRotation;
            Vector3 p = t.localPosition;
            Put(clip, path, "m_LocalRotation.x", q.x);
            Put(clip, path, "m_LocalRotation.y", q.y);
            Put(clip, path, "m_LocalRotation.z", q.z);
            Put(clip, path, "m_LocalRotation.w", q.w);
            Put(clip, path, "m_LocalPosition.x", p.x);
            Put(clip, path, "m_LocalPosition.y", p.y);
            Put(clip, path, "m_LocalPosition.z", p.z);
        }
        return clip;
    }

    static void Put(AnimationClip clip, string path, string prop, float v)
    {
        var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), prop);
        AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, 1f / 60f, v));
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
#endif
