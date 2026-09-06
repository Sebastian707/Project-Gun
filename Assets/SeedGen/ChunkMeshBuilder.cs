using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BackroomsStyle
{
    [Tooltip("Wall thickness in meters. 0.2-0.3 reads as a real interior wall.")]
    public float wallThickness;

    [Tooltip("Height of a small protruding trim strip at the wall base.")]
    public float baseboardHeight;
    [Tooltip("How far the baseboard sticks out past the wall face.")]
    public float baseboardProtrusion;

    [Tooltip("Side length of a pillar's square cross-section.")]
    public float pillarSize;
    [Range(0f, 1f), Tooltip("Chance a grid corner inside an open room gets a support pillar.")]
    public float roomPillarChance;
    [Range(0f, 1f), Tooltip("Chance a grid corner inside the hallway maze gets a pillar. Keep low/zero - a pillar can block a 1-wide hallway.")]
    public float hallwayPillarChance;

    [Tooltip("Thickness of the box collider placed under the floor (visual floor stays a thin plane at y=0, the collider gives it some depth to stand on).")]
    public float floorColliderThickness;
    [Tooltip("Thickness of the box collider placed above the ceiling.")]
    public float ceilingColliderThickness;

    public static BackroomsStyle Default => new BackroomsStyle
    {
        wallThickness = 0.25f,
        baseboardHeight = 0.15f,
        baseboardProtrusion = 0.03f,
        pillarSize = 0.4f,
        roomPillarChance = 0.22f,
        hallwayPillarChance = 0.0f,
        floorColliderThickness = 0.2f,
        ceilingColliderThickness = 0.2f
    };
}

public static class ChunkMeshBuilder
{
    public struct ColliderBox
    {
        public Vector3 center;
        public Vector3 size;
    }

    public static void Build(
        Mesh mesh,
        BackroomsLayout layout,
        Vector2Int chunkCoord,
        int chunkSize,
        float cellSize,
        float wallHeight,
        BackroomsStyle style,
        List<ColliderBox> colliderBoxes = null)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var floorTris = new List<int>();
        var wallTris = new List<int>();
        var ceilTris = new List<int>();

        colliderBoxes?.Clear();

        int baseCellX = chunkCoord.x * chunkSize;
        int baseCellZ = chunkCoord.y * chunkSize;
        float t = style.wallThickness;

        float originX = baseCellX * cellSize;
        float originZ = baseCellZ * cellSize;

        for (int lx = 0; lx < chunkSize; lx++)
        {
            for (int lz = 0; lz < chunkSize; lz++)
            {
                int cx = baseCellX + lx;
                int cz = baseCellZ + lz;

                float wx = cx * cellSize - originX;
                float wz = cz * cellSize - originZ;

                Vector3 p00 = new Vector3(wx, 0f, wz);
                Vector3 p10 = new Vector3(wx + cellSize, 0f, wz);
                Vector3 p01 = new Vector3(wx, 0f, wz + cellSize);
                Vector3 p11 = new Vector3(wx + cellSize, 0f, wz + cellSize);

                AddQuad(verts, uvs, floorTris, p00, p01, p11, p10, cellSize);

                Vector3 up = Vector3.up * wallHeight;
                AddQuad(verts, uvs, ceilTris, p00 + up, p10 + up, p11 + up, p01 + up, cellSize);

                if (!layout.IsEdgeOpen(cx, cz, EdgeDir.North))
                {
                    AddBox(verts, uvs, wallTris,
                        wx + t * 0.5f, wx + cellSize - t * 0.5f,
                        wz + cellSize - t * 0.5f, wz + cellSize + t * 0.5f,
                        0f, wallHeight, colliderBoxes);

                    AddBaseboard(verts, uvs, wallTris, style,
                        wx + t * 0.5f, wx + cellSize - t * 0.5f,
                        wz + cellSize - t * 0.5f, wz + cellSize + t * 0.5f);
                }
                if (!layout.IsEdgeOpen(cx, cz, EdgeDir.East))
                {
                    AddBox(verts, uvs, wallTris,
                        wx + cellSize - t * 0.5f, wx + cellSize + t * 0.5f,
                        wz + t * 0.5f, wz + cellSize - t * 0.5f,
                        0f, wallHeight, colliderBoxes);
                    AddBaseboard(verts, uvs, wallTris, style,
                        wx + cellSize - t * 0.5f, wx + cellSize + t * 0.5f,
                        wz + t * 0.5f, wz + cellSize - t * 0.5f);
                }

                {
                    int px = cx + 1;
                    int pz = cz + 1;
                    bool cellIsRoom = layout.IsOpenRoomZone(cx, cz);
                    float pillarChance = cellIsRoom ? style.roomPillarChance : style.hallwayPillarChance;
                    bool builtPillar = false;

                    if (pillarChance > 0f && DHash.HashChance(layout.seed, pillarChance, px, pz, 555))
                    {
                        float cxw = px * cellSize - originX;
                        float czw = pz * cellSize - originZ;
                        float h = style.pillarSize * 0.5f;
                        AddBox(verts, uvs, wallTris, cxw - h, cxw + h, czw - h, czw + h, 0f, wallHeight, colliderBoxes);
                        builtPillar = true;
                    }

                    if (!builtPillar)
                    {
                        bool needsFill =
                            !layout.IsEdgeOpen(cx, cz, EdgeDir.North) ||
                            !layout.IsEdgeOpen(cx + 1, cz, EdgeDir.North) ||
                            !layout.IsEdgeOpen(cx, cz, EdgeDir.East) ||
                            !layout.IsEdgeOpen(cx, cz + 1, EdgeDir.East);

                        if (needsFill)
                        {
                            float cxw = px * cellSize - originX;
                            float czw = pz * cellSize - originZ;
                            float h = t * 0.5f;
                            AddBox(verts, uvs, wallTris, cxw - h, cxw + h, czw - h, czw + h, 0f, wallHeight, colliderBoxes);
                        }
                    }
                }
            }
        }

        if (colliderBoxes != null)
        {
            float chunkWorldSize = chunkSize * cellSize;

            colliderBoxes.Add(new ColliderBox
            {
                center = new Vector3(chunkWorldSize * 0.5f, -style.floorColliderThickness * 0.5f, chunkWorldSize * 0.5f),
                size = new Vector3(chunkWorldSize, style.floorColliderThickness, chunkWorldSize)
            });

            colliderBoxes.Add(new ColliderBox
            {
                center = new Vector3(chunkWorldSize * 0.5f, wallHeight + style.ceilingColliderThickness * 0.5f, chunkWorldSize * 0.5f),
                size = new Vector3(chunkWorldSize, style.ceilingColliderThickness, chunkWorldSize)
            });
        }

        mesh.Clear();
        mesh.subMeshCount = 3;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(floorTris, 0);
        mesh.SetTriangles(wallTris, 1);
        mesh.SetTriangles(ceilTris, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
    }

    private static void AddQuad(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, float cellSize)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, cellSize));
        uvs.Add(new Vector2(cellSize, cellSize));
        uvs.Add(new Vector2(cellSize, 0));
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
    }

    private static void AddBox(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
        float minX, float maxX, float minZ, float maxZ, float minY, float maxY,
        List<ColliderBox> colliderBoxes = null)
    {
        Vector3 a = new Vector3(minX, minY, minZ);
        Vector3 b = new Vector3(maxX, minY, minZ);
        Vector3 c = new Vector3(maxX, minY, maxZ);
        Vector3 d = new Vector3(minX, minY, maxZ);
        Vector3 a2 = new Vector3(minX, maxY, minZ);
        Vector3 b2 = new Vector3(maxX, maxY, minZ);
        Vector3 c2 = new Vector3(maxX, maxY, maxZ);
        Vector3 d2 = new Vector3(minX, maxY, maxZ);

        float h = maxY - minY;
        float wSide1 = maxX - minX;
        float wSide2 = maxZ - minZ;

        AddQuad(verts, uvs, tris, a, a2, b2, b, wSide1 > 0 ? wSide1 : h);
        AddQuad(verts, uvs, tris, c, c2, d2, d, wSide1 > 0 ? wSide1 : h); 
        AddQuad(verts, uvs, tris, b, b2, c2, c, wSide2 > 0 ? wSide2 : h); 
        AddQuad(verts, uvs, tris, d, d2, a2, a, wSide2 > 0 ? wSide2 : h); 
        AddQuad(verts, uvs, tris, a2, d2, c2, b2, h);                    

        if (colliderBoxes != null)
        {
            colliderBoxes.Add(new ColliderBox
            {
                center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f),
                size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ)
            });
        }
    }

    private static void AddBaseboard(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
        BackroomsStyle style, float minX, float maxX, float minZ, float maxZ)
    {
        float p = style.baseboardProtrusion;
        AddBox(verts, uvs, tris,
            minX - p, maxX + p, minZ - p, maxZ + p,
            0f, style.baseboardHeight);
    }
}