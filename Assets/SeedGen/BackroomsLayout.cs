using UnityEngine;


public enum EdgeDir { North, East, South, West }

public class BackroomsLayout
{
    public readonly int seed;
    public readonly int zoneSize;         
    public readonly float roomZoneChance;  
    public readonly float hallwayOpenChance; 

    public BackroomsLayout(int seed, int zoneSize, float roomZoneChance, float hallwayOpenChance)
    {
        this.seed = seed;
        this.zoneSize = Mathf.Max(2, zoneSize);
        this.roomZoneChance = roomZoneChance;
        this.hallwayOpenChance = hallwayOpenChance;
    }

    public Vector2Int ZoneOf(int cellX, int cellZ)
    {
        return new Vector2Int(
            Mathf.FloorToInt(cellX / (float)zoneSize),
            Mathf.FloorToInt(cellZ / (float)zoneSize));
    }

    public bool IsOpenRoomZone(Vector2Int zone)
    {
        return DHash.HashChance(seed, roomZoneChance, zone.x, zone.y, 777);
    }

    public bool IsOpenRoomZone(int cellX, int cellZ) => IsOpenRoomZone(ZoneOf(cellX, cellZ));

    public bool IsEdgeOpen(int x, int z, EdgeDir dir)
    {
        int nx = x, nz = z;
        switch (dir)
        {
            case EdgeDir.North: nz = z + 1; break;
            case EdgeDir.South: nz = z - 1; break;
            case EdgeDir.East: nx = x + 1; break;
            case EdgeDir.West: nx = x - 1; break;
        }

        int ax = x, az = z, bx = nx, bz = nz;
        if (bx < ax || (bx == ax && bz < az))
        {
            ax = nx; az = nz; bx = x; bz = z;
        }

        Vector2Int zoneA = ZoneOf(ax, az);
        Vector2Int zoneB = ZoneOf(bx, bz);
        bool aRoom = IsOpenRoomZone(zoneA);
        bool bRoom = IsOpenRoomZone(zoneB);

        if (zoneA == zoneB && aRoom)
            return true;

        float chance = (aRoom || bRoom) ? 0.85f : hallwayOpenChance;

        return DHash.HashChance(seed, chance, ax, az, bx, bz, 42);
    }
}
