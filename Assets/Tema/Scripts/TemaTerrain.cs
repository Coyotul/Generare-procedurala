using Lab5;
using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Terenul lumii deschise: mesh procedural cu biomi (elevatie + umiditate, ca la Lab5),
    /// collider pentru raycast, plus suport pentru sampling de inaltime (plasare obiecte) si
    /// pentru deformare in timp real (lopata). Refoloseste noise-ul din Lab5BiomeNoise.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TemaTerrain : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int mapWidth = 160;
        [SerializeField] private int mapHeight = 160;
        [SerializeField] private float meshScale = 1f;
        [SerializeField] private float heightMultiplier = 22f;
        [SerializeField] private float waterLevel = 0.35f;
        [SerializeField] private bool centerAtOrigin = true;

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

        [Header("Digging")]
        [SerializeField] private float minDigY = -15f;

        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private int _w;
        private int _h;
        private Vector3 _originOffset;
        private Vector3[] _vertices;
        private float[,] _elevation;

        public bool IsGenerated { get; private set; }

        // Limitele lumii pe XZ (world space) - folosite de spawnere.
        public float MinWorldX => 0f - _originOffset.x;
        public float MaxWorldX => (_w - 1) * meshScale - _originOffset.x;
        public float MinWorldZ => 0f - _originOffset.z;
        public float MaxWorldZ => (_h - 1) * meshScale - _originOffset.z;

        private void Awake()
        {
            Generate();
        }

        [ContextMenu("Generate Terrain")]
        public void Generate()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();

            _w = Mathf.Max(2, mapWidth);
            _h = Mathf.Max(2, mapHeight);

            _originOffset = centerAtOrigin
                ? new Vector3((_w - 1) * meshScale * 0.5f, 0f, (_h - 1) * meshScale * 0.5f)
                : Vector3.zero;

            float[,] moisture;
            _elevation = GenerateNoiseMap(_w, _h, elevationScale, elevationOctaves,
                elevationLacunarity, elevationPersistence, elevationSeed);
            moisture = GenerateNoiseMap(_w, _h, moistureScale, moistureOctaves,
                moistureLacunarity, moisturePersistence, moistureSeed);

            BuildMesh(_elevation, moisture);
            ApplyMesh();
            IsGenerated = true;
        }

        private float[,] GenerateNoiseMap(int w, int h, float scale, int octaves,
            float lacunarity, float persistence, int seed)
        {
            float[,] map = new float[w, h];
            float safeScale = Mathf.Max(0.0001f, scale);
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1) * safeScale;
                    float v = (float)z / (h - 1) * safeScale;
                    map[x, z] = Lab5BiomeNoise.FractalNoise2D(
                        u, v,
                        Mathf.Max(1, octaves),
                        Mathf.Max(1f, lacunarity),
                        Mathf.Clamp01(persistence),
                        seed);
                }
            return map;
        }

        private void BuildMesh(float[,] elevation, float[,] moisture)
        {
            _vertices = new Vector3[_w * _h];
            Color[] colors = new Color[_w * _h];
            Vector2[] uvs = new Vector2[_w * _h];
            int[] triangles = new int[(_w - 1) * (_h - 1) * 6];

            for (int z = 0; z < _h; z++)
                for (int x = 0; x < _w; x++)
                {
                    int idx = z * _w + x;
                    float e = elevation[x, z];
                    float displayElevation = e < waterLevel ? waterLevel : e;
                    float worldY = displayElevation * heightMultiplier;

                    _vertices[idx] = new Vector3(
                        x * meshScale - _originOffset.x,
                        worldY,
                        z * meshScale - _originOffset.z);
                    colors[idx] = GetBiomeColor(e, moisture[x, z]);
                    uvs[idx] = new Vector2((float)x / (_w - 1), (float)z / (_h - 1));
                }

            int t = 0;
            for (int z = 0; z < _h - 1; z++)
                for (int x = 0; x < _w - 1; x++)
                {
                    int a = z * _w + x;
                    int b = a + 1;
                    int c = a + _w;
                    int d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }

            _mesh = new Mesh
            {
                name = "TemaTerrainMesh",
                indexFormat = _vertices.Length > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };
            _mesh.vertices = _vertices;
            _mesh.triangles = triangles;
            _mesh.colors = colors;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        private void ApplyMesh()
        {
            if (Application.isPlaying) _meshFilter.mesh = _mesh;
            else _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
        }

        private Color GetBiomeColor(float elevation, float moisture)
        {
            if (elevation < waterLevel) return new Color(0.10f, 0.35f, 0.70f); // Ocean
            if (elevation < 0.40f) return new Color(0.93f, 0.88f, 0.66f);      // Plaja
            if (elevation < 0.75f)
            {
                if (moisture < 0.20f) return new Color(0.93f, 0.83f, 0.52f); // Desert
                if (moisture < 0.40f) return new Color(0.78f, 0.78f, 0.45f); // Savana
                if (moisture < 0.65f) return new Color(0.42f, 0.65f, 0.32f); // Padure
                if (moisture < 0.85f) return new Color(0.18f, 0.50f, 0.22f); // Jungla
                return new Color(0.28f, 0.45f, 0.40f);                       // Mlastina
            }
            if (elevation < 0.85f)
                return moisture < 0.40f
                    ? new Color(0.80f, 0.85f, 0.85f)  // Tundra
                    : new Color(0.20f, 0.40f, 0.30f); // Taiga
            return Color.white; // Zapada
        }

        // ---------- API public pentru spawnere si lopata ----------

        /// <summary>Inaltimea (world Y) a suprafetei la o pozitie XZ in world space (bilinear).</summary>
        public float SampleHeight(float worldX, float worldZ)
        {
            if (_vertices == null) return 0f;

            float gx = (worldX + _originOffset.x) / meshScale;
            float gz = (worldZ + _originOffset.z) / meshScale;
            gx = Mathf.Clamp(gx, 0f, _w - 1.001f);
            gz = Mathf.Clamp(gz, 0f, _h - 1.001f);

            int x0 = (int)gx;
            int z0 = (int)gz;
            float tx = gx - x0;
            float tz = gz - z0;

            float y00 = _vertices[z0 * _w + x0].y;
            float y10 = _vertices[z0 * _w + (x0 + 1)].y;
            float y01 = _vertices[(z0 + 1) * _w + x0].y;
            float y11 = _vertices[(z0 + 1) * _w + (x0 + 1)].y;

            float a = Mathf.Lerp(y00, y10, tx);
            float b = Mathf.Lerp(y01, y11, tx);
            return Mathf.Lerp(a, b, tz);
        }

        /// <summary>Elevatia normalizata [0,1] (pentru filtrare pe biom la spawn).</summary>
        public float SampleElevation01(float worldX, float worldZ)
        {
            if (_elevation == null) return 0f;
            float gx = (worldX + _originOffset.x) / meshScale;
            float gz = (worldZ + _originOffset.z) / meshScale;
            int x = Mathf.Clamp(Mathf.RoundToInt(gx), 0, _w - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(gz), 0, _h - 1);
            return _elevation[x, z];
        }

        /// <summary>
        /// Coboara vertecsii din raza data in jurul punctului (world space) cu un falloff neted.
        /// Reconstruieste normalele si collider-ul. Apelat continuu cat timp se sapa.
        /// </summary>
        public void Dig(Vector3 worldPoint, float radius, float strength)
        {
            if (_vertices == null) return;

            float gx = (worldPoint.x + _originOffset.x) / meshScale;
            float gz = (worldPoint.z + _originOffset.z) / meshScale;
            int gridRadius = Mathf.CeilToInt(radius / meshScale) + 1;

            int xMin = Mathf.Max(0, (int)gx - gridRadius);
            int xMax = Mathf.Min(_w - 1, (int)gx + gridRadius);
            int zMin = Mathf.Max(0, (int)gz - gridRadius);
            int zMax = Mathf.Min(_h - 1, (int)gz + gridRadius);

            bool changed = false;
            float r2 = radius * radius;

            for (int z = zMin; z <= zMax; z++)
                for (int x = xMin; x <= xMax; x++)
                {
                    int idx = z * _w + x;
                    Vector3 v = _vertices[idx];
                    float dx = v.x - worldPoint.x;
                    float dz = v.z - worldPoint.z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 > r2) continue;

                    float falloff = 1f - Mathf.Sqrt(d2) / radius; // 1 in centru -> 0 la margine
                    falloff = falloff * falloff * (3f - 2f * falloff); // smoothstep
                    float newY = v.y - strength * falloff;
                    if (newY < minDigY) newY = minDigY;
                    if (!Mathf.Approximately(newY, v.y))
                    {
                        v.y = newY;
                        _vertices[idx] = v;
                        changed = true;
                    }
                }

            if (!changed) return;

            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
        }
    }
}
