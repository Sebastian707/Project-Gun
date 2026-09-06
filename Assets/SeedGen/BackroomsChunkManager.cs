using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BackroomsChunkManager : MonoBehaviour
{
    [Header("World Seed")]
    [Tooltip("Same seed = same world, forever. Change it for a different layout.")]
    public int seed = 12345;

    [Header("Player")]
    public Transform player;

    [Header("Grid Settings")]
    [Tooltip("Cells per chunk side.")]
    public int chunkSize = 8;
    [Tooltip("World-space size of one hallway cell.")]
    public float cellSize = 4f;
    public float wallHeight = 3.2f;

    [Header("Layout Tuning")]
    [Tooltip("Cells per side that make up one potential open-room zone.")]
    public int zoneSize = 4;
    [Range(0f, 1f)] public float roomZoneChance = 0.12f;
    [Range(0f, 1f), Tooltip("Chance a normal hallway edge is a passage rather than a wall.")]
    public float hallwayOpenChance = 0.55f;

    [Header("Streaming")]
    [Tooltip("Chunks are loaded within this many chunks of the player.")]
    public int viewDistanceInChunks = 4;
    [Tooltip("Chunks are unloaded once they exceed this many chunks away (should be >= view distance).")]
    public int unloadDistanceInChunks = 5;
    [Tooltip("Max chunks generated per frame from the throttled queue (chunks farther than guaranteedImmediateRadius away). Raise this if you still see brief floor gaps at high movement speed; lower it if streaming causes visible hitches.")]
    public int maxGeneratedPerFrame = 3;
    [Tooltip("Chunks within this many chunks of the player (Chebyshev distance) are always spawned immediately/synchronously, never left to the throttled queue - this is what actually prevents falling through the floor. 1 covers the player's chunk plus its 8 neighbors.")]
    public int guaranteedImmediateRadius = 1;

    [Header("Fall-Through Safety Net")]
    [Tooltip("If the player ever ends up below the floor anyway (e.g. a huge single-frame teleport), snap them back up above their current chunk instead of falling forever.")]
    public bool enableFallThroughSafetyNet = true;
    [Tooltip("If the player's Y position drops below this, the safety net kicks in.")]
    public float fallThroughYThreshold = -10f;
    [Tooltip("Y position the player is placed at when the safety net triggers.")]
    public float safetyNetRecoveryY = 1f;

    [Header("Floating Origin")]
    [Tooltip("When the player's distance from the current render origin exceeds this many world units, everything currently in the scene (player + spawned chunks) is shifted back toward (0,0,0) to avoid float-precision jitter. Keep this well below ~50,000 (a safe margin under float32's precision ceiling).")]
    public float originRebaseThreshold = 2000f;

    [Header("Wall / Pillar Style")]
    public BackroomsStyle style;

    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;

    [Header("Collision")]
    public bool addColliders = true;

    private BackroomsLayout layout;
    private readonly Dictionary<Vector2Int, ChunkInstance> active = new Dictionary<Vector2Int, ChunkInstance>();
    private readonly Stack<ChunkInstance> pool = new Stack<ChunkInstance>();

    private readonly List<Vector2Int> generationQueue = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> queuedSet = new HashSet<Vector2Int>();

    private Vector2Int lastPlayerChunk;
    private bool initialized;

    private Vector3 originOffset = Vector3.zero;

    private class ChunkInstance
    {
        public GameObject go;
        public Mesh mesh;
        public MeshFilter filter;
        public MeshRenderer renderer;

        public List<ChunkMeshBuilder.ColliderBox> colliderBoxes = new List<ChunkMeshBuilder.ColliderBox>();

        public List<BoxCollider> boxColliders = new List<BoxCollider>();
    }

    private void Awake()
    {
        layout = new BackroomsLayout(seed, zoneSize, roomZoneChance, hallwayOpenChance);
        if (style.wallThickness <= 0f) style = BackroomsStyle.Default;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("BackroomsChunkManager: no player assigned, using this transform.");
            player = transform;
        }
        lastPlayerChunk = WorldToChunk(player.position);
        RefreshRequiredChunks(force: true);
        StartCoroutine(GenerationLoop());
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        CheckOriginRebase();

        Vector2Int currentChunk = WorldToChunk(player.position);
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            RefreshRequiredChunks(force: false);
        }

        CheckFallThroughSafetyNet();
    }

    private void CheckFallThroughSafetyNet()
    {
        if (!enableFallThroughSafetyNet) return;
        if (player.position.y >= fallThroughYThreshold) return;

        Vector2Int chunk = WorldToChunk(player.position);
        if (!active.ContainsKey(chunk))
        {
            Spawn(chunk);
        }

        Vector3 pos = player.position;
        pos.y = safetyNetRecoveryY;
        player.position = pos;
    }

    private Vector2Int WorldToChunk(Vector3 renderedWorldPos)
    {
        float chunkWorldSize = chunkSize * cellSize;
        Vector3 trueWorldPos = renderedWorldPos + originOffset;
        return new Vector2Int(
            Mathf.FloorToInt(trueWorldPos.x / chunkWorldSize),
            Mathf.FloorToInt(trueWorldPos.z / chunkWorldSize));
    }

    private void CheckOriginRebase()
    {
        Vector3 pos = player.position;
        float distSqrFromOrigin = pos.x * pos.x + pos.z * pos.z;
        if (distSqrFromOrigin < originRebaseThreshold * originRebaseThreshold) return;

        float chunkWorldSize = chunkSize * cellSize;

        Vector3 shift = new Vector3(
            Mathf.Round(pos.x / chunkWorldSize) * chunkWorldSize,
            0f,
            Mathf.Round(pos.z / chunkWorldSize) * chunkWorldSize);

        if (shift.sqrMagnitude < 0.001f) return;

        RebaseOrigin(shift);
    }

    private void RebaseOrigin(Vector3 shift)
    {

        player.position -= shift;

        foreach (var kv in active)
        {
            kv.Value.go.transform.position -= shift;
        }

        originOffset += shift;
    }

    private void RefreshRequiredChunks(bool force)
    {
        Vector2Int center = lastPlayerChunk;

        for (int dx = -guaranteedImmediateRadius; dx <= guaranteedImmediateRadius; dx++)
        {
            for (int dz = -guaranteedImmediateRadius; dz <= guaranteedImmediateRadius; dz++)
            {
                Vector2Int coord = new Vector2Int(center.x + dx, center.y + dz);
                if (active.ContainsKey(coord)) continue;
                queuedSet.Remove(coord); 
                Spawn(coord);
            }
        }

        for (int dx = -viewDistanceInChunks; dx <= viewDistanceInChunks; dx++)
        {
            for (int dz = -viewDistanceInChunks; dz <= viewDistanceInChunks; dz++)
            {
                Vector2Int coord = new Vector2Int(center.x + dx, center.y + dz);
                if (active.ContainsKey(coord) || queuedSet.Contains(coord)) continue;
                generationQueue.Add(coord);
                queuedSet.Add(coord);
            }
        }

        List<Vector2Int> toRemove = null;
        foreach (var kv in active)
        {
            Vector2Int c = kv.Key;
            int dist = Mathf.Max(Mathf.Abs(c.x - center.x), Mathf.Abs(c.y - center.y));
            if (dist > unloadDistanceInChunks)
            {
                (toRemove ??= new List<Vector2Int>()).Add(c);
            }
        }
        if (toRemove != null)
        {
            foreach (var c in toRemove)
                Despawn(c);
        }
    }

    private IEnumerator GenerationLoop()
    {
        while (true)
        {
            if (generationQueue.Count > 0)
            {

                generationQueue.Sort((a, b) =>
                    ChebyshevDistance(a, lastPlayerChunk).CompareTo(ChebyshevDistance(b, lastPlayerChunk)));

                int generatedThisFrame = 0;
                int i = 0;
                while (i < generationQueue.Count && generatedThisFrame < maxGeneratedPerFrame)
                {
                    Vector2Int coord = generationQueue[i];
                    generationQueue.RemoveAt(i);
                    queuedSet.Remove(coord);

                    int dist = ChebyshevDistance(coord, lastPlayerChunk);
                    if (dist > viewDistanceInChunks) continue;
                    if (active.ContainsKey(coord)) continue;

                    Spawn(coord);
                    generatedThisFrame++;
                }
            }
            yield return null;
        }
    }

    private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    private void Spawn(Vector2Int coord)
    {
        ChunkInstance inst = pool.Count > 0 ? pool.Pop() : CreateChunkInstance();

        float chunkWorldSize = chunkSize * cellSize;
        Vector3 trueWorldPos = new Vector3(coord.x * chunkWorldSize, 0f, coord.y * chunkWorldSize);
        inst.go.transform.position = trueWorldPos - originOffset;
        inst.go.name = $"Chunk_{coord.x}_{coord.y}";
        inst.go.SetActive(true);

        ChunkMeshBuilder.Build(inst.mesh, layout, coord, chunkSize, cellSize, wallHeight, style,
            addColliders ? inst.colliderBoxes : null);

        if (addColliders)
        {
            ApplyBoxColliders(inst);
        }

        active[coord] = inst;
    }

    private void ApplyBoxColliders(ChunkInstance inst)
    {
        var specs = inst.colliderBoxes;

        while (inst.boxColliders.Count < specs.Count)
        {
            inst.boxColliders.Add(inst.go.AddComponent<BoxCollider>());
        }

        for (int i = 0; i < specs.Count; i++)
        {
            BoxCollider bc = inst.boxColliders[i];
            bc.center = specs[i].center;
            bc.size = specs[i].size;
            bc.enabled = true;
        }

        for (int i = specs.Count; i < inst.boxColliders.Count; i++)
        {
            inst.boxColliders[i].enabled = false;
        }
    }

    private void Despawn(Vector2Int coord)
    {
        if (!active.TryGetValue(coord, out var inst)) return;
        active.Remove(coord);

        inst.go.SetActive(false);
        pool.Push(inst);
    }

    private ChunkInstance CreateChunkInstance()
    {
        var go = new GameObject("Chunk");
        go.transform.SetParent(transform, false);

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[] { floorMaterial, wallMaterial, ceilingMaterial };

        return new ChunkInstance
        {
            go = go,
            mesh = mesh,
            filter = filter,
            renderer = renderer
        };
    }

    public Vector3 GetTrueWorldPosition(Vector3 renderedWorldPos)
    {
        return renderedWorldPos + originOffset;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        float chunkWorldSize = chunkSize * cellSize;
        Vector2Int center = WorldToChunk(player.position);
        Gizmos.color = Color.yellow;
        Vector3 size = new Vector3(chunkWorldSize, 0.1f, chunkWorldSize);
        for (int dx = -viewDistanceInChunks; dx <= viewDistanceInChunks; dx++)
        {
            for (int dz = -viewDistanceInChunks; dz <= viewDistanceInChunks; dz++)
            {
                Vector3 c = new Vector3(
                    (center.x + dx + 0.5f) * chunkWorldSize,
                    0f,
                    (center.y + dz + 0.5f) * chunkWorldSize) - originOffset;
                Gizmos.DrawWireCube(c, size);
            }
        }
    }
#endif
}