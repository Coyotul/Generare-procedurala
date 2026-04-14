using System;
using UnityEngine;

public class Lab3ConwayDungeonGenerator2D : MonoBehaviour
{
    private const int RequiredGenerationCount = 5;

    [Header("Grid")]
    [SerializeField] private int width = 80;
    [SerializeField] private int height = 50;
    [SerializeField, Range(0f, 1f)] private float initialAliveChance = 0.45f;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Build")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool centerGridAtOrigin = true;
    [SerializeField] private bool autoGenerateOnStart;

    private bool[,] _grid;

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateDungeon();
        }
    }

    [ContextMenu("Generate Dungeon (Gen 5 Conway)")]
    public void GenerateDungeon()
    {
        InitializeGrid();

        for (int i = 0; i < RequiredGenerationCount; i++)
        {
            _grid = StepGeneration(_grid);
        }

        BuildVisuals();
    }

    [ContextMenu("Clear Dungeon")]
    public void ClearDungeon()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void InitializeGrid()
    {
        int safeWidth = Mathf.Max(5, width);
        int safeHeight = Mathf.Max(5, height);

        _grid = new bool[safeWidth, safeHeight];

        int finalSeed = randomSeed ? Environment.TickCount : seed;
        System.Random rng = new System.Random(finalSeed);

        for (int y = 0; y < safeHeight; y++)
        {
            for (int x = 0; x < safeWidth; x++)
            {
                bool border = x == 0 || y == 0 || x == safeWidth - 1 || y == safeHeight - 1;
                if (border)
                {
                    _grid[x, y] = true;
                }
                else
                {
                    _grid[x, y] = rng.NextDouble() < initialAliveChance;
                }
            }
        }
    }

    private bool[,] StepGeneration(bool[,] current)
    {
        int w = current.GetLength(0);
        int h = current.GetLength(1);
        bool[,] next = new bool[w, h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int neighbors = CountAliveNeighbors(current, x, y);
                bool alive = current[x, y];

                if (alive)
                {
                    next[x, y] = neighbors == 2 || neighbors == 3;
                }
                else
                {
                    next[x, y] = neighbors == 3;
                }
            }
        }

        return next;
    }

    private int CountAliveNeighbors(bool[,] grid, int x, int y)
    {
        int count = 0;
        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                int nx = x + ox;
                int ny = y + oy;

                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                {
                    if (grid[nx, ny])
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private void BuildVisuals()
    {
        ClearDungeon();

        if (_grid == null)
        {
            return;
        }

        int w = _grid.GetLength(0);
        int h = _grid.GetLength(1);
        float safeSize = Mathf.Max(0.01f, cellSize);

        Vector3 offset = Vector3.zero;
        if (centerGridAtOrigin)
        {
            offset = new Vector3((w - 1) * safeSize * 0.5f, (h - 1) * safeSize * 0.5f, 0f);
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isWall = _grid[x, y];
                GameObject prefab = isWall ? wallPrefab : floorPrefab;

                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(x * safeSize, y * safeSize, 0f) - offset;
                GameObject cell = Instantiate(prefab, position, Quaternion.identity, transform);
                cell.name = isWall ? $"Wall_{x}_{y}" : $"Floor_{x}_{y}";
            }
        }
    }
}
