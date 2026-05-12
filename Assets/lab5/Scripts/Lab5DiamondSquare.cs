using UnityEngine;

namespace Lab5
{
    public static class Lab5DiamondSquare
    {
        public static float[,] Generate(
            int iterations,
            float roughness,
            float initialMin,
            float initialMax,
            int seed)
        {
            int n = Mathf.Clamp(iterations, 1, 12);
            int size = (1 << n) + 1;

            float[,] grid = new float[size, size];
            System.Random rng = new System.Random(seed);

            float min = Mathf.Min(initialMin, initialMax);
            float max = Mathf.Max(initialMin, initialMax);
            grid[0, 0] = RandomRange(rng, min, max);
            grid[size - 1, 0] = RandomRange(rng, min, max);
            grid[0, size - 1] = RandomRange(rng, min, max);
            grid[size - 1, size - 1] = RandomRange(rng, min, max);

            int step = size - 1;
            float range = Mathf.Max(0.05f, max - min);
            float decay = Mathf.Clamp(roughness, 0.05f, 0.95f);

            while (step > 1)
            {
                int half = step / 2;

                for (int z = 0; z + step < size; z += step)
                {
                    for (int x = 0; x + step < size; x += step)
                    {
                        float avg = (
                            grid[x, z] +
                            grid[x + step, z] +
                            grid[x, z + step] +
                            grid[x + step, z + step]) * 0.25f;

                        grid[x + half, z + half] = Mathf.Clamp01(avg + RandomRange(rng, -range, range));
                    }
                }

                for (int z = 0; z < size; z += half)
                {
                    int xStart = ((z / half) % 2 == 0) ? half : 0;
                    for (int x = xStart; x < size; x += step)
                    {
                        float sum = 0f;
                        int count = 0;

                        if (x - half >= 0) { sum += grid[x - half, z]; count++; }
                        if (x + half < size) { sum += grid[x + half, z]; count++; }
                        if (z - half >= 0) { sum += grid[x, z - half]; count++; }
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
