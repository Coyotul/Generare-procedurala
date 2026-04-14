using Lab1;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class Lab1PerlinTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Size")]
    [SerializeField] private int terrainWidth = 300;
    [SerializeField] private int terrainLength = 300;
    [SerializeField] private int terrainHeight = 80;
    [SerializeField] private int heightmapResolution = 513;

    [Header("Noise")]
    [SerializeField] private float noiseScale = 8f;
    [SerializeField] private float heightMultiplier = 30f;
    [SerializeField] private int octaves = 5;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private int seed = 2026;
    [SerializeField] private Vector2 offset;

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("Terrain component not found.", this);
            return;
        }

        if (terrain.terrainData == null)
        {
            terrain.terrainData = new TerrainData();
        }

        int safeResolution = Mathf.Max(33, heightmapResolution);
        if ((safeResolution - 1) % 2 != 0)
        {
            safeResolution += 1;
        }

        int safeTerrainHeight = Mathf.Max(1, terrainHeight);
        float safeNoiseScale = Mathf.Max(0.0001f, noiseScale);

        TerrainData data = terrain.terrainData;
        data.heightmapResolution = safeResolution;
        data.size = new Vector3(Mathf.Max(1, terrainWidth), safeTerrainHeight, Mathf.Max(1, terrainLength));

        float[,] heights = new float[safeResolution, safeResolution];

        for (int z = 0; z < safeResolution; z++)
        {
            for (int x = 0; x < safeResolution; x++)
            {
                float u = (float)x / (safeResolution - 1);
                float v = (float)z / (safeResolution - 1);

                float nx = offset.x + u * safeNoiseScale;
                float ny = offset.y + v * safeNoiseScale;

                float noise = Lab1PerlinNoise.FractalNoise2D(nx, ny, octaves, lacunarity, persistence, seed);
                float normalizedHeight = (noise * Mathf.Max(0f, heightMultiplier)) / safeTerrainHeight;

                heights[z, x] = Mathf.Clamp01(normalizedHeight);
            }
        }

        data.SetHeights(0, 0, heights);
        terrain.Flush();

        Debug.Log("Terrain generated with custom Perlin Noise.", this);
    }
}
