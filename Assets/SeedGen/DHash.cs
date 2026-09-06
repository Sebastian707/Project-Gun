using UnityEngine;

public static class DHash
{
    public static uint Hash(int seed, params int[] coords)
    {
        unchecked
        {
            uint h = (uint)seed * 374761393u + 0x9E3779B9u;
            for (int i = 0; i < coords.Length; i++)
            {
                h = (h ^ (uint)coords[i]) * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
            }
            h ^= h >> 16;
            h *= 3266489917u;
            h ^= h >> 13;
            return h;
        }
    }

    public static float Hash01(int seed, params int[] coords)
    {
        return (Hash(seed, coords) & 0x00FFFFFFu) / (float)0x01000000u;
    }

    public static bool HashChance(int seed, float probability, params int[] coords)
    {
        return Hash01(seed, coords) < probability;
    }
}
