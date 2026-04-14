using System;
using System.Collections.Generic;

namespace Lab1
{
    public static class Lab1PerlinNoise
    {
        private static readonly Dictionary<int, int[]> PermutationCache = new Dictionary<int, int[]>();

        public static float Noise(float x, float y, float z, int seed = 0)
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

        public static float FractalNoise2D(
            float x,
            float y,
            int octaves = 4,
            float lacunarity = 2f,
            float persistence = 0.5f,
            int seed = 0)
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

            if (maxSum <= 0f)
            {
                return 0f;
            }

            return sum / maxSum;
        }

        private static int[] GetPermutation(int seed)
        {
            if (PermutationCache.TryGetValue(seed, out int[] cached))
            {
                return cached;
            }

            int[] baseValues = new int[256];
            for (int i = 0; i < baseValues.Length; i++)
            {
                baseValues[i] = i;
            }

            Random random = new Random(seed);
            for (int i = baseValues.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                int tmp = baseValues[i];
                baseValues[i] = baseValues[swapIndex];
                baseValues[swapIndex] = tmp;
            }

            int[] p = new int[512];
            for (int i = 0; i < 512; i++)
            {
                p[i] = baseValues[i & 255];
            }

            PermutationCache[seed] = p;
            return p;
        }

        private static int Inc(int num)
        {
            return (num + 1) & 255;
        }

        private static int FloorToInt(float value)
        {
            return value >= 0f ? (int)value : (int)value - 1;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

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
