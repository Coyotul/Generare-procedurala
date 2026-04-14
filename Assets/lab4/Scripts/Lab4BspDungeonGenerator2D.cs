using System;
using System.Collections.Generic;
using UnityEngine;

public class Lab4BspDungeonGenerator2D : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 70;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool centerGridAtOrigin = true;

    [Header("BSP")]
    [SerializeField] private int maxDepth = 5;
    [SerializeField] private int minLeafSize = 14;
    [SerializeField] private int minRoomSize = 5;
    [SerializeField] private int maxRoomPadding = 2;
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private int seed = 404;

    [Header("Build")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private bool autoGenerateOnStart;

    private bool[,] _walls;
    private System.Random _rng;

    private class BspNode
    {
        public RectInt Area;
        public BspNode Left;
        public BspNode Right;
        public RectInt Room;

        public bool IsLeaf => Left == null && Right == null;

        public BspNode(RectInt area)
        {
            Area = area;
            Room = new RectInt(-1, -1, 0, 0);
        }
    }

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateDungeon();
        }
    }

    [ContextMenu("Generate BSP Dungeon")]
    public void GenerateDungeon()
    {
        InitializeRng();
        InitializeWallGrid();

        int safeWidth = _walls.GetLength(0);
        int safeHeight = _walls.GetLength(1);

        BspNode root = new BspNode(new RectInt(1, 1, safeWidth - 2, safeHeight - 2));

        SplitRecursive(root, 0);
        CreateRooms(root);
        ConnectRooms(root);

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

    private void InitializeRng()
    {
        int finalSeed = randomSeed ? Environment.TickCount : seed;
        _rng = new System.Random(finalSeed);
    }

    private void InitializeWallGrid()
    {
        int safeWidth = Mathf.Max(20, width);
        int safeHeight = Mathf.Max(20, height);

        _walls = new bool[safeWidth, safeHeight];

        for (int y = 0; y < safeHeight; y++)
        {
            for (int x = 0; x < safeWidth; x++)
            {
                _walls[x, y] = true;
            }
        }
    }

    private void SplitRecursive(BspNode node, int depth)
    {
        if (node == null)
        {
            return;
        }

        if (depth >= Mathf.Max(1, maxDepth))
        {
            return;
        }

        int minSize = Mathf.Max(6, minLeafSize);
        bool canSplitHoriz = node.Area.height >= minSize * 2;
        bool canSplitVert = node.Area.width >= minSize * 2;

        if (!canSplitHoriz && !canSplitVert)
        {
            return;
        }

        bool splitVert;
        if (canSplitVert && canSplitHoriz)
        {
            float aspect = (float)node.Area.width / node.Area.height;
            if (aspect > 1.25f)
            {
                splitVert = true;
            }
            else if (aspect < 0.8f)
            {
                splitVert = false;
            }
            else
            {
                splitVert = _rng.NextDouble() > 0.5;
            }
        }
        else
        {
            splitVert = canSplitVert;
        }

        if (splitVert)
        {
            int splitMin = node.Area.xMin + minSize;
            int splitMax = node.Area.xMax - minSize;
            if (splitMax <= splitMin)
            {
                return;
            }

            int splitX = _rng.Next(splitMin, splitMax);
            RectInt leftArea = new RectInt(node.Area.xMin, node.Area.yMin, splitX - node.Area.xMin, node.Area.height);
            RectInt rightArea = new RectInt(splitX, node.Area.yMin, node.Area.xMax - splitX, node.Area.height);

            node.Left = new BspNode(leftArea);
            node.Right = new BspNode(rightArea);
        }
        else
        {
            int splitMin = node.Area.yMin + minSize;
            int splitMax = node.Area.yMax - minSize;
            if (splitMax <= splitMin)
            {
                return;
            }

            int splitY = _rng.Next(splitMin, splitMax);
            RectInt bottomArea = new RectInt(node.Area.xMin, node.Area.yMin, node.Area.width, splitY - node.Area.yMin);
            RectInt topArea = new RectInt(node.Area.xMin, splitY, node.Area.width, node.Area.yMax - splitY);

            node.Left = new BspNode(bottomArea);
            node.Right = new BspNode(topArea);
        }

        SplitRecursive(node.Left, depth + 1);
        SplitRecursive(node.Right, depth + 1);
    }

    private void CreateRooms(BspNode node)
    {
        if (node == null)
        {
            return;
        }

        if (!node.IsLeaf)
        {
            CreateRooms(node.Left);
            CreateRooms(node.Right);
            return;
        }

        int maxPadding = Mathf.Max(1, maxRoomPadding);
        int padX = _rng.Next(1, maxPadding + 1);
        int padY = _rng.Next(1, maxPadding + 1);

        int minSize = Mathf.Max(3, minRoomSize);
        int maxRoomWidth = node.Area.width - padX * 2;
        int maxRoomHeight = node.Area.height - padY * 2;

        if (maxRoomWidth < minSize || maxRoomHeight < minSize)
        {
            node.Room = new RectInt(node.Area.xMin + 1, node.Area.yMin + 1, Mathf.Max(2, node.Area.width - 2), Mathf.Max(2, node.Area.height - 2));
        }
        else
        {
            int roomWidth = _rng.Next(minSize, maxRoomWidth + 1);
            int roomHeight = _rng.Next(minSize, maxRoomHeight + 1);

            int roomXMin = node.Area.xMin + padX;
            int roomXMax = node.Area.xMax - padX - roomWidth;
            int roomYMin = node.Area.yMin + padY;
            int roomYMax = node.Area.yMax - padY - roomHeight;

            int roomX = roomXMax > roomXMin ? _rng.Next(roomXMin, roomXMax + 1) : roomXMin;
            int roomY = roomYMax > roomYMin ? _rng.Next(roomYMin, roomYMax + 1) : roomYMin;

            node.Room = new RectInt(roomX, roomY, roomWidth, roomHeight);
        }

        CarveRect(node.Room);
    }

    private void ConnectRooms(BspNode node)
    {
        if (node == null || node.IsLeaf)
        {
            return;
        }

        ConnectRooms(node.Left);
        ConnectRooms(node.Right);

        RectInt leftRoom = GetAnyRoom(node.Left);
        RectInt rightRoom = GetAnyRoom(node.Right);

        if (leftRoom.width <= 0 || rightRoom.width <= 0)
        {
            return;
        }

        Vector2Int a = RandomPointInRoom(leftRoom);
        Vector2Int b = RandomPointInRoom(rightRoom);

        CarveCorridor(a, b);
    }

    private RectInt GetAnyRoom(BspNode node)
    {
        if (node == null)
        {
            return new RectInt(-1, -1, 0, 0);
        }

        if (node.IsLeaf)
        {
            return node.Room;
        }

        RectInt leftRoom = GetAnyRoom(node.Left);
        if (leftRoom.width > 0)
        {
            return leftRoom;
        }

        return GetAnyRoom(node.Right);
    }

    private Vector2Int RandomPointInRoom(RectInt room)
    {
        int x = _rng.Next(room.xMin, room.xMax);
        int y = _rng.Next(room.yMin, room.yMax);
        return new Vector2Int(x, y);
    }

    private void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        bool horizontalFirst = _rng.NextDouble() > 0.5;

        if (horizontalFirst)
        {
            CarveHorizontal(a.x, b.x, a.y);
            CarveVertical(a.y, b.y, b.x);
        }
        else
        {
            CarveVertical(a.y, b.y, a.x);
            CarveHorizontal(a.x, b.x, b.y);
        }
    }

    private void CarveHorizontal(int x0, int x1, int y)
    {
        int min = Mathf.Min(x0, x1);
        int max = Mathf.Max(x0, x1);
        for (int x = min; x <= max; x++)
        {
            SetFloor(x, y);
            SetFloor(x, y + 1);
        }
    }

    private void CarveVertical(int y0, int y1, int x)
    {
        int min = Mathf.Min(y0, y1);
        int max = Mathf.Max(y0, y1);
        for (int y = min; y <= max; y++)
        {
            SetFloor(x, y);
            SetFloor(x + 1, y);
        }
    }

    private void CarveRect(RectInt rect)
    {
        for (int y = rect.yMin; y < rect.yMax; y++)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                SetFloor(x, y);
            }
        }
    }

    private void SetFloor(int x, int y)
    {
        if (_walls == null)
        {
            return;
        }

        int w = _walls.GetLength(0);
        int h = _walls.GetLength(1);

        if (x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1)
        {
            return;
        }

        _walls[x, y] = false;
    }

    private void BuildVisuals()
    {
        ClearDungeon();

        if (_walls == null)
        {
            return;
        }

        int w = _walls.GetLength(0);
        int h = _walls.GetLength(1);
        float safeSize = Mathf.Max(0.01f, cellSize);

        Vector3 originOffset = Vector3.zero;
        if (centerGridAtOrigin)
        {
            originOffset = new Vector3((w - 1) * safeSize * 0.5f, (h - 1) * safeSize * 0.5f, 0f);
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isWall = _walls[x, y];
                GameObject prefab = isWall ? wallPrefab : floorPrefab;
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(x * safeSize, y * safeSize, 0f) - originOffset;
                GameObject cell = Instantiate(prefab, position, Quaternion.identity, transform);
                cell.name = isWall ? $"Wall_{x}_{y}" : $"Floor_{x}_{y}";
            }
        }
    }
}
