using Lab5;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Lab5BiomeGenerator : MonoBehaviour
{
    public enum HeightSource
    {
        FractalNoise,
        DiamondSquare
    }

    [Header("Grid")]
    [SerializeField] private int mapWidth = 200;
    [SerializeField] private int mapHeight = 200;
    [SerializeField] private float meshScale = 0.5f;
    [SerializeField] private float heightMultiplier = 20f;
    [SerializeField] private float waterLevel = 0.35f;

    [Header("Height source")]
    [SerializeField] private HeightSource heightSource = HeightSource.FractalNoise;

    [Header("Elevation noise")]
    [SerializeField] private int elevationSeed = 2026;
    [SerializeField] private float elevationScale = 4f;
    [SerializeField] private int elevationOctaves = 7;
    [SerializeField] private float elevationLacunarity = 2f;
    [SerializeField, Range(0f, 1f)] private float elevationPersistence = 0.5f;

    [Header("Moisture noise")]
    [SerializeField] private int moistureSeed = 9173;
    [SerializeField] private float moistureScale = 3f;
    [SerializeField] private int moistureOctaves = 6;
    [SerializeField] private float moistureLacunarity = 2f;
    [SerializeField, Range(0f, 1f)] private float moisturePersistence = 0.5f;

    [Header("Diamond Square (bonus)")]
    [SerializeField] private int diamondSquareSeed = 1234;
    [SerializeField, Range(2, 10)] private int diamondSquareIterations = 8;
    [SerializeField] private float diamondSquareRoughness = 0.55f;
    [SerializeField] private float diamondSquareInitialMin = 0.2f;
    [SerializeField] private float diamondSquareInitialMax = 0.8f;

    [Header("Auto generate")]
    [SerializeField] private bool autoGenerateOnStart = true;

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Biomes")]
    public void Generate()
    {
        int w = Mathf.Max(2, mapWidth);
        int h = Mathf.Max(2, mapHeight);

        float[,] elevation = heightSource == HeightSource.DiamondSquare
            ? GenerateDiamondSquareElevation(w, h)
            : GenerateFractalElevation(w, h);

        float[,] moisture = GenerateMoistureMap(w, h);

        Mesh mesh = BuildMesh(elevation, moisture, w, h);
        MeshFilter mf = GetComponent<MeshFilter>();
        if (Application.isPlaying)
        {
            mf.mesh = mesh;
        }
        else
        {
            mf.sharedMesh = mesh;
        }
    }

    private float[,] GenerateFractalElevation(int w, int h)
    {
        float[,] map = new float[w, h];
        float safeScale = Mathf.Max(0.0001f, elevationScale);

        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1) * safeScale;
                float v = (float)z / (h - 1) * safeScale;
                map[x, z] = Lab5BiomeNoise.FractalNoise2D(
                    u, v,
                    Mathf.Max(1, elevationOctaves),
                    Mathf.Max(1f, elevationLacunarity),
                    Mathf.Clamp01(elevationPersistence),
                    elevationSeed);
            }
        }

        return map;
    }

    private float[,] GenerateMoistureMap(int w, int h)
    {
        float[,] map = new float[w, h];
        float safeScale = Mathf.Max(0.0001f, moistureScale);

        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1) * safeScale;
                float v = (float)z / (h - 1) * safeScale;
                map[x, z] = Lab5BiomeNoise.FractalNoise2D(
                    u, v,
                    Mathf.Max(1, moistureOctaves),
                    Mathf.Max(1f, moistureLacunarity),
                    Mathf.Clamp01(moisturePersistence),
                    moistureSeed);
            }
        }

        return map;
    }

    private float[,] GenerateDiamondSquareElevation(int w, int h)
    {
        float[,] full = Lab5DiamondSquare.Generate(
            diamondSquareIterations,
            diamondSquareRoughness,
            diamondSquareInitialMin,
            diamondSquareInitialMax,
            diamondSquareSeed);

        int side = full.GetLength(0);
        float[,] map = new float[w, h];
        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float fx = (float)x / (w - 1) * (side - 1);
                float fz = (float)z / (h - 1) * (side - 1);
                int ix = Mathf.Clamp((int)fx, 0, side - 2);
                int iz = Mathf.Clamp((int)fz, 0, side - 2);
                float tx = fx - ix;
                float tz = fz - iz;

                float a = full[ix, iz];
                float b = full[ix + 1, iz];
                float c = full[ix, iz + 1];
                float d = full[ix + 1, iz + 1];

                float ab = Mathf.Lerp(a, b, tx);
                float cd = Mathf.Lerp(c, d, tx);
                map[x, z] = Mathf.Lerp(ab, cd, tz);
            }
        }

        return map;
    }

    private Mesh BuildMesh(float[,] elevation, float[,] moisture, int w, int h)
    {
        Vector3[] vertices = new Vector3[w * h];
        Color[] colors = new Color[w * h];
        Vector2[] uvs = new Vector2[w * h];
        int[] triangles = new int[(w - 1) * (h - 1) * 6];

        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = z * w + x;
                float e = elevation[x, z];
                float displayElevation = e < waterLevel ? waterLevel : e;
                float worldY = displayElevation * heightMultiplier;

                vertices[idx] = new Vector3(x * meshScale, worldY, z * meshScale);
                colors[idx] = GetBiomeColor(e, moisture[x, z]);
                uvs[idx] = new Vector2((float)x / (w - 1), (float)z / (h - 1));
            }
        }

        int triIdx = 0;
        for (int z = 0; z < h - 1; z++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                int a = z * w + x;
                int b = a + 1;
                int c = a + w;
                int d = c + 1;

                triangles[triIdx++] = a;
                triangles[triIdx++] = c;
                triangles[triIdx++] = b;

                triangles[triIdx++] = b;
                triangles[triIdx++] = c;
                triangles[triIdx++] = d;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Lab5BiomeMesh",
            indexFormat = vertices.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Color GetBiomeColor(float elevation, float moisture)
    {
        if (elevation < waterLevel)
        {
            return new Color(0.10f, 0.35f, 0.70f);
        }

        if (elevation < 0.40f)
        {
            return new Color(0.93f, 0.88f, 0.66f);
        }

        if (elevation < 0.75f)
        {
            if (moisture < 0.20f) return new Color(0.93f, 0.83f, 0.52f);
            if (moisture < 0.40f) return new Color(0.78f, 0.78f, 0.45f);
            if (moisture < 0.65f) return new Color(0.42f, 0.65f, 0.32f);
            if (moisture < 0.85f) return new Color(0.18f, 0.50f, 0.22f);
            return new Color(0.28f, 0.45f, 0.40f);
        }

        if (elevation < 0.85f)
        {
            return moisture < 0.40f
                ? new Color(0.80f, 0.85f, 0.85f)
                : new Color(0.20f, 0.40f, 0.30f);
        }

        return Color.white;
    }
}
