# Laboratorul 5 — Biome Generator (Unity / C#)

## Cerinta laboratorului

Generare procedurala de biomi folosind doua harti de zgomot (elevatie + umiditate)
si clasificarea Whittaker. Mesh-ul 3D este construit din grid, fiecare vertex
primind inaltimea din harta de elevatie si culoarea din clasificarea biomilor.

Tasks:
1. Genereaza harta de elevatie si harta de umiditate cu seed-uri diferite,
   folosind in ambele cazuri multiple octave (6-8).
2. Genereaza culori diferite pentru fiecare biom, folosind tabelul Whittaker.
3. Construieste mesh-ul 3D atribuind fiecarui vertex inaltimea si culoarea corecta.
4. Bonus: implementeaza algoritmul Diamond Square pentru a genera un heightmap.

Tabel Whittaker simplificat:

| Biom                  | Elevatie    | Umiditate  |
|-----------------------|-------------|------------|
| Ocean                 | < 0.35      | -          |
| Plaja                 | 0.35 - 0.40 | -          |
| Desert                | 0.40 - 0.75 | < 0.20     |
| Savana                | 0.40 - 0.75 | 0.20-0.40  |
| Padure temperata      | 0.40 - 0.75 | 0.40-0.65  |
| Jungla                | 0.40 - 0.75 | 0.65-0.85  |
| Mlastina              | 0.40 - 0.75 | > 0.85     |
| Tundra                | 0.75 - 0.85 | < 0.40     |
| Taiga                 | 0.75 - 0.85 | > 0.40     |
| Zapada / Munte inalt  | > 0.85      | -          |

---

## Cum functioneaza, pe scurt

### Perlin Noise (Lab5BiomeNoise.cs)

La fiecare colt intreg al grilei (xi, yi, zi) hash-uiesc un gradient pseudo-aleator
prin tabelul de permutare p[]. Pentru un punct (x, y, z) intre colturi:
- calculez offset-ul de la fiecare colt la punct
- fac dot product intre offset si gradient -> "influenta" colt-ului
- interpolez trilinear cele 8 valori folosind functia Fade(t) = 6t^5 - 15t^4 + 10t^3
  (continuitate C^2, fara linii vizibile la marginile celulelor)

Seed -> permutation table (Fisher-Yates shuffle pe 0..255) -> hash la colturi ->
gradienti -> noise. Acelasi seed = aceeasi harta determinist.

### Fractal Noise (octave)

```
for i in [0..octaves):
    sum += Noise(x*freq, y*freq, seed + i*17) * amplitude
    amplitude *= persistence    // 0.5 default
    frequency *= lacunarity     // 2.0 default
return sum / total_amplitude
```

Octava 1 = forma generala (munti, vai). Octave 2-3 = dealuri. Octave 3+ = stanci.
`seed + i*17` ofera permutari diferite pe fiecare octava ca sa nu se repete identic.

### Doua harti, seed-uri diferite

- elevationSeed = 2026, scale = 4, 7 octave
- moistureSeed  = 9173, scale = 3, 6 octave

Daca seed-urile ar fi identice, moisture(x,z) == elevation(x,z) la fiecare punct =>
spatiul 2D Whittaker degenereaza intr-o dreapta diagonala, ai pierde deserturi,
mlastini, tundra. Seed-uri diferite = harti necorelate = tot tabelul Whittaker se umple.

### Diamond Square (bonus)

Grila (2^n)+1 x (2^n)+1. Pas cu pas:

1. Init: cele 4 colturi primesc valori random in [initialMin, initialMax].
2. SQUARE step: mijlocul fiecarui patrat = media celor 4 colturi + random(-range, +range).
3. DIAMOND step: mijloacele laturilor = media a maxim 4 colturi de romb + random.
   Pe margini, rombul are doar 3 colturi -> mediez doar pe cele existente.
4. step /= 2, range *= roughness (default 0.55), repeat pana step <= 1.

Roughness controleaza cat de zgrunturos e relieful. Range scade exponential cu
fiecare iteratie -> punctele de detaliu se abat doar putin de la media vecinilor.

### Construirea mesh-ului

```
for fiecare (x, z) in grila:
    idx = z * W + x
    height = elevation < waterLevel ? waterLevel : elevation   // aplatizare ocean
    vertices[idx] = (x*scale, height*heightMultiplier, z*scale)
    colors[idx]   = GetBiomeColor(elevation, moisture)
    uvs[idx]      = (x/(W-1), z/(H-1))

for fiecare quad pe grila:
    a=jos-stanga, b=jos-dreapta, c=sus-stanga, d=sus-dreapta
    triangle 1: a -> c -> b
    triangle 2: b -> c -> d
```

`mesh.RecalculateNormals()` mediaza normalele triunghiurilor pe vertecsi comuni ->
shading neted. Pentru >65k vertecsi e nevoie de `IndexFormat.UInt32`.

### Shader-ul cu vertex colors

URP-ul default nu foloseste direct culorile pe vertex. Am scris un shader minimal
HLSL care:
- citeste COLOR din vertex (Attributes)
- aplica Lambert simplu cu lumina principala URP
- multiplica cu _Tint si adauga _Ambient

Materialul `biomeMat.mat` foloseste acest shader.

---

## Cod sursa

### Assets/lab5/Scripts/Lab5BiomeNoise.cs

```csharp
using System;
using System.Collections.Generic;

namespace Lab5
{
    public static class Lab5BiomeNoise
    {
        private static readonly Dictionary<int, int[]> PermutationCache = new Dictionary<int, int[]>();

        public static float FractalNoise2D(
            float x, float y, int octaves, float lacunarity, float persistence, int seed)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float maxSum = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float n = Noise(x * frequency, y * frequency, 0f, seed + i * 17);
                sum += n * amplitude;
                maxSum += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (maxSum <= 0f) return 0f;
            return sum / maxSum;
        }

        public static float Noise(float x, float y, float z, int seed)
        {
            int[] p = GetPermutation(seed);

            int xi = FloorToInt(x) & 255;
            int yi = FloorToInt(y) & 255;
            int zi = FloorToInt(z) & 255;

            float xf = x - FloorToInt(x);
            float yf = y - FloorToInt(y);
            float zf = z - FloorToInt(z);

            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            int aaa = p[p[p[xi] + yi] + zi];
            int aba = p[p[p[xi] + Inc(yi)] + zi];
            int aab = p[p[p[xi] + yi] + Inc(zi)];
            int abb = p[p[p[xi] + Inc(yi)] + Inc(zi)];
            int baa = p[p[p[Inc(xi)] + yi] + zi];
            int bba = p[p[p[Inc(xi)] + Inc(yi)] + zi];
            int bab = p[p[p[Inc(xi)] + yi] + Inc(zi)];
            int bbb = p[p[p[Inc(xi)] + Inc(yi)] + Inc(zi)];

            float x1 = Lerp(Grad(aaa, xf, yf, zf), Grad(baa, xf - 1f, yf, zf), u);
            float x2 = Lerp(Grad(aba, xf, yf - 1f, zf), Grad(bba, xf - 1f, yf - 1f, zf), u);
            float y1 = Lerp(x1, x2, v);

            float x3 = Lerp(Grad(aab, xf, yf, zf - 1f), Grad(bab, xf - 1f, yf, zf - 1f), u);
            float x4 = Lerp(Grad(abb, xf, yf - 1f, zf - 1f), Grad(bbb, xf - 1f, yf - 1f, zf - 1f), u);
            float y2 = Lerp(x3, x4, v);

            float value = Lerp(y1, y2, w);
            return (value + 1f) * 0.5f;
        }

        private static int[] GetPermutation(int seed)
        {
            if (PermutationCache.TryGetValue(seed, out int[] cached)) return cached;

            int[] baseValues = new int[256];
            for (int i = 0; i < baseValues.Length; i++) baseValues[i] = i;

            Random random = new Random(seed);
            for (int i = baseValues.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                int tmp = baseValues[i];
                baseValues[i] = baseValues[swapIndex];
                baseValues[swapIndex] = tmp;
            }

            int[] p = new int[512];
            for (int i = 0; i < 512; i++) p[i] = baseValues[i & 255];

            PermutationCache[seed] = p;
            return p;
        }

        private static int Inc(int num) => (num + 1) & 255;
        private static int FloorToInt(float value) => value >= 0f ? (int)value : (int)value - 1;
        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            float first = (h & 1) == 0 ? u : -u;
            float second = (h & 2) == 0 ? v : -v;
            return first + second;
        }
    }
}
```

### Assets/lab5/Scripts/Lab5BiomeGenerator.cs

```csharp
using Lab5;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Lab5BiomeGenerator : MonoBehaviour
{
    public enum HeightSource { FractalNoise, DiamondSquare }

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
        if (autoGenerateOnStart) Generate();
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
        if (Application.isPlaying) mf.mesh = mesh;
        else mf.sharedMesh = mesh;
    }

    private float[,] GenerateFractalElevation(int w, int h)
    {
        float[,] map = new float[w, h];
        float safeScale = Mathf.Max(0.0001f, elevationScale);
        for (int z = 0; z < h; z++)
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
        return map;
    }

    private float[,] GenerateMoistureMap(int w, int h)
    {
        float[,] map = new float[w, h];
        float safeScale = Mathf.Max(0.0001f, moistureScale);
        for (int z = 0; z < h; z++)
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
        return map;
    }

    private float[,] GenerateDiamondSquareElevation(int w, int h)
    {
        float[,] full = Lab5DiamondSquare.Generate(
            diamondSquareIterations, diamondSquareRoughness,
            diamondSquareInitialMin, diamondSquareInitialMax, diamondSquareSeed);

        int side = full.GetLength(0);
        float[,] map = new float[w, h];
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                float fx = (float)x / (w - 1) * (side - 1);
                float fz = (float)z / (h - 1) * (side - 1);
                int ix = Mathf.Clamp((int)fx, 0, side - 2);
                int iz = Mathf.Clamp((int)fz, 0, side - 2);
                float tx = fx - ix, tz = fz - iz;
                float a = full[ix, iz], b = full[ix + 1, iz];
                float c = full[ix, iz + 1], d = full[ix + 1, iz + 1];
                float ab = Mathf.Lerp(a, b, tx);
                float cd = Mathf.Lerp(c, d, tx);
                map[x, z] = Mathf.Lerp(ab, cd, tz);
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
            for (int x = 0; x < w; x++)
            {
                int idx = z * w + x;
                float e = elevation[x, z];
                float displayElevation = e < waterLevel ? waterLevel : e;
                float worldY = displayElevation * heightMultiplier;

                vertices[idx] = new Vector3(x * meshScale, worldY, z * meshScale);
                colors[idx]   = GetBiomeColor(e, moisture[x, z]);
                uvs[idx]      = new Vector2((float)x / (w - 1), (float)z / (h - 1));
            }

        int triIdx = 0;
        for (int z = 0; z < h - 1; z++)
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
        if (elevation < waterLevel)  return new Color(0.10f, 0.35f, 0.70f); // Ocean
        if (elevation < 0.40f)       return new Color(0.93f, 0.88f, 0.66f); // Plaja

        if (elevation < 0.75f)
        {
            if (moisture < 0.20f) return new Color(0.93f, 0.83f, 0.52f); // Desert
            if (moisture < 0.40f) return new Color(0.78f, 0.78f, 0.45f); // Savana
            if (moisture < 0.65f) return new Color(0.42f, 0.65f, 0.32f); // Padure
            if (moisture < 0.85f) return new Color(0.18f, 0.50f, 0.22f); // Jungla
            return new Color(0.28f, 0.45f, 0.40f);                       // Mlastina
        }

        if (elevation < 0.85f)
        {
            return moisture < 0.40f
                ? new Color(0.80f, 0.85f, 0.85f)  // Tundra
                : new Color(0.20f, 0.40f, 0.30f); // Taiga
        }

        return Color.white; // Zapada
    }
}
```

### Assets/lab5/Scripts/Lab5DiamondSquare.cs

```csharp
using UnityEngine;

namespace Lab5
{
    public static class Lab5DiamondSquare
    {
        public static float[,] Generate(
            int iterations, float roughness, float initialMin, float initialMax, int seed)
        {
            int n = Mathf.Clamp(iterations, 1, 12);
            int size = (1 << n) + 1;

            float[,] grid = new float[size, size];
            System.Random rng = new System.Random(seed);

            float min = Mathf.Min(initialMin, initialMax);
            float max = Mathf.Max(initialMin, initialMax);
            grid[0, 0]               = RandomRange(rng, min, max);
            grid[size - 1, 0]        = RandomRange(rng, min, max);
            grid[0, size - 1]        = RandomRange(rng, min, max);
            grid[size - 1, size - 1] = RandomRange(rng, min, max);

            int step = size - 1;
            float range = Mathf.Max(0.05f, max - min);
            float decay = Mathf.Clamp(roughness, 0.05f, 0.95f);

            while (step > 1)
            {
                int half = step / 2;

                // SQUARE step: centrul fiecarui patrat
                for (int z = 0; z + step < size; z += step)
                    for (int x = 0; x + step < size; x += step)
                    {
                        float avg = (
                            grid[x, z] + grid[x + step, z] +
                            grid[x, z + step] + grid[x + step, z + step]) * 0.25f;
                        grid[x + half, z + half] = Mathf.Clamp01(avg + RandomRange(rng, -range, range));
                    }

                // DIAMOND step: mijloacele laturilor (cu fallback pentru romburi incomplete pe margini)
                for (int z = 0; z < size; z += half)
                {
                    int xStart = ((z / half) % 2 == 0) ? half : 0;
                    for (int x = xStart; x < size; x += step)
                    {
                        float sum = 0f;
                        int count = 0;

                        if (x - half >= 0)   { sum += grid[x - half, z]; count++; }
                        if (x + half < size) { sum += grid[x + half, z]; count++; }
                        if (z - half >= 0)   { sum += grid[x, z - half]; count++; }
                        if (z + half < size) { sum += grid[x, z + half]; count++; }

                        if (count > 0)
                        {
                            float avg = sum / count;
                            grid[x, z] = Mathf.Clamp01(avg + RandomRange(rng, -range, range));
                        }
                    }
                }

                step = half;
                range *= decay;
            }

            return grid;
        }

        private static float RandomRange(System.Random rng, float minInclusive, float maxInclusive)
        {
            return (float)(rng.NextDouble() * (maxInclusive - minInclusive) + minInclusive);
        }
    }
}
```

### Assets/lab5/BiomeVertexColor.shader (URP)

```hlsl
Shader "Lab5/BiomeVertexColor"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Ambient ("Ambient", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float  _Ambient;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(n, mainLight.direction));
                float3 lighting = mainLight.color.rgb * NdotL + _Ambient.xxx;
                float3 baseColor = IN.color.rgb * _Tint.rgb;
                return half4(baseColor * lighting, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
```

---

## Rulare in Unity

1. Deschide scena Assets/lab5/lab5.unity.
2. GameObject-ul BiomeGenerator are componenta Lab5BiomeGenerator. La Play genereaza
   automat mesh-ul (autoGenerateOnStart = true).
3. Pentru a folosi Diamond Square in loc de fractal noise, schimba "Height Source"
   din inspector.
4. Apasa context menu "Generate Biomes" pentru regenerare manuala.
