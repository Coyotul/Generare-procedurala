using System.Collections.Generic;
using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Dungeon 3D generat procedural cu BSP (impartire recursiva a spatiului), pe care
    /// playerul third-person il poate parcurge. Podeaua e o placa mare, iar peretii sunt
    /// cuburi 3D ridicate doar pe celulele de zid invecinate cu podea (conturul camerelor +
    /// coridoarelor). Toate camerele sunt conectate prin coridoare (ca la Lab4).
    /// Dupa generare, playerul e asezat in prima camera (intrarea).
    /// </summary>
    public class TemaDungeon3D : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int width = 48;
        [SerializeField] private int height = 36;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float wallHeight = 3f;

        [Header("BSP")]
        [SerializeField] private int maxDepth = 5;
        [SerializeField] private int minLeafSize = 8;
        [SerializeField] private int minRoomSize = 4;
        [SerializeField] private int maxRoomPadding = 2;
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 1234;

        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;

        private bool[,] _walls;
        private System.Random _rng;
        private readonly List<RectInt> _rooms = new List<RectInt>();
        private Vector3 _origin;

        public bool IsGenerated { get; private set; }
        public int RoomCount => _rooms.Count;

        private class BspNode
        {
            public RectInt Area;
            public BspNode Left;
            public BspNode Right;
            public RectInt Room;
            public bool IsLeaf => Left == null && Right == null;
            public BspNode(RectInt area) { Area = area; Room = new RectInt(-1, -1, 0, 0); }
        }

        private void Start()
        {
            EnsureGenerated();
        }

        public void EnsureGenerated()
        {
            if (!IsGenerated) Generate();
        }

        /// <summary>Pozitie aleatoare pe podea (centrul unei celule dintr-o camera), in world space.</summary>
        public bool TryGetRandomFloorPosition(System.Random rng, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (_rooms == null || _rooms.Count == 0) return false;
            RectInt room = _rooms[rng.Next(_rooms.Count)];
            int cx = rng.Next(room.xMin, room.xMax);
            int cy = rng.Next(room.yMin, room.yMax);
            pos = CellToWorld(cx, cy);
            return true;
        }

        [ContextMenu("Generate Dungeon")]
        public void Generate()
        {
            int s = randomSeed ? System.Environment.TickCount : seed;
            _rng = new System.Random(s);

            int w = Mathf.Max(20, width);
            int h = Mathf.Max(20, height);
            _walls = new bool[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _walls[x, y] = true;
            _rooms.Clear();

            BspNode root = new BspNode(new RectInt(1, 1, w - 2, h - 2));
            Split(root, 0);
            CreateRooms(root);
            Connect(root);

            _origin = new Vector3((w - 1) * cellSize * 0.5f, 0f, (h - 1) * cellSize * 0.5f);
            EnsureMaterials();
            Build(w, h);
            PlacePlayer();
            IsGenerated = true;
        }

        // ---------- BSP (portat din Lab4) ----------

        private void Split(BspNode node, int depth)
        {
            if (node == null || depth >= Mathf.Max(1, maxDepth)) return;

            int minSize = Mathf.Max(6, minLeafSize);
            bool canSplitHoriz = node.Area.height >= minSize * 2;
            bool canSplitVert = node.Area.width >= minSize * 2;
            if (!canSplitHoriz && !canSplitVert) return;

            bool splitVert;
            if (canSplitVert && canSplitHoriz)
            {
                float aspect = (float)node.Area.width / node.Area.height;
                if (aspect > 1.25f) splitVert = true;
                else if (aspect < 0.8f) splitVert = false;
                else splitVert = _rng.NextDouble() > 0.5;
            }
            else splitVert = canSplitVert;

            if (splitVert)
            {
                int splitMin = node.Area.xMin + minSize;
                int splitMax = node.Area.xMax - minSize;
                if (splitMax <= splitMin) return;
                int splitX = _rng.Next(splitMin, splitMax);
                node.Left = new BspNode(new RectInt(node.Area.xMin, node.Area.yMin, splitX - node.Area.xMin, node.Area.height));
                node.Right = new BspNode(new RectInt(splitX, node.Area.yMin, node.Area.xMax - splitX, node.Area.height));
            }
            else
            {
                int splitMin = node.Area.yMin + minSize;
                int splitMax = node.Area.yMax - minSize;
                if (splitMax <= splitMin) return;
                int splitY = _rng.Next(splitMin, splitMax);
                node.Left = new BspNode(new RectInt(node.Area.xMin, node.Area.yMin, node.Area.width, splitY - node.Area.yMin));
                node.Right = new BspNode(new RectInt(node.Area.xMin, splitY, node.Area.width, node.Area.yMax - splitY));
            }

            Split(node.Left, depth + 1);
            Split(node.Right, depth + 1);
        }

        private void CreateRooms(BspNode node)
        {
            if (node == null) return;
            if (!node.IsLeaf) { CreateRooms(node.Left); CreateRooms(node.Right); return; }

            int maxPadding = Mathf.Max(1, maxRoomPadding);
            int padX = _rng.Next(1, maxPadding + 1);
            int padY = _rng.Next(1, maxPadding + 1);
            int minSize = Mathf.Max(3, minRoomSize);

            int maxRoomWidth = node.Area.width - padX * 2;
            int maxRoomHeight = node.Area.height - padY * 2;
            if (maxRoomWidth < minSize || maxRoomHeight < minSize)
            {
                node.Room = new RectInt(node.Area.xMin + 1, node.Area.yMin + 1,
                    Mathf.Max(2, node.Area.width - 2), Mathf.Max(2, node.Area.height - 2));
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

            _rooms.Add(node.Room);
            CarveRect(node.Room);
        }

        private void Connect(BspNode node)
        {
            if (node == null || node.IsLeaf) return;
            Connect(node.Left);
            Connect(node.Right);

            RectInt a = GetAnyRoom(node.Left);
            RectInt b = GetAnyRoom(node.Right);
            if (a.width <= 0 || b.width <= 0) return;

            Vector2Int pa = new Vector2Int(_rng.Next(a.xMin, a.xMax), _rng.Next(a.yMin, a.yMax));
            Vector2Int pb = new Vector2Int(_rng.Next(b.xMin, b.xMax), _rng.Next(b.yMin, b.yMax));
            CarveCorridor(pa, pb);
        }

        private RectInt GetAnyRoom(BspNode node)
        {
            if (node == null) return new RectInt(-1, -1, 0, 0);
            if (node.IsLeaf) return node.Room;
            RectInt left = GetAnyRoom(node.Left);
            return left.width > 0 ? left : GetAnyRoom(node.Right);
        }

        private void CarveCorridor(Vector2Int a, Vector2Int b)
        {
            if (_rng.NextDouble() > 0.5)
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
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            {
                SetFloor(x, y);
                SetFloor(x, y + 1);
            }
        }

        private void CarveVertical(int y0, int y1, int x)
        {
            for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
            {
                SetFloor(x, y);
                SetFloor(x + 1, y);
            }
        }

        private void CarveRect(RectInt rect)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                    SetFloor(x, y);
        }

        private void SetFloor(int x, int y)
        {
            int w = _walls.GetLength(0);
            int h = _walls.GetLength(1);
            if (x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1) return;
            _walls[x, y] = false;
        }

        // ---------- Build 3D ----------

        private void Build(int w, int h)
        {
            // podea: o placa mare sub tot grid-ul
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DungeonFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.localScale = new Vector3(w * cellSize, 0.4f, h * cellSize);
            floor.transform.position = new Vector3(0f, -0.2f, 0f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            // ziduri: doar celulele de zid invecinate cu podea (conturul camerelor/coridoarelor)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!_walls[x, y]) continue;
                    if (!HasFloorNeighbor(x, y, w, h)) continue;

                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.SetParent(transform, false);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
                    wall.transform.position = CellToWorld(x, y) + Vector3.up * (wallHeight * 0.5f);
                    wall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
                }
        }

        private bool HasFloorNeighbor(int x, int y, int w, int h)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (!_walls[nx, ny]) return true;
                }
            return false;
        }

        private Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x * cellSize, 0f, y * cellSize) - _origin;
        }

        private void PlacePlayer()
        {
            if (player == null || _rooms.Count == 0) return;
            RectInt entrance = _rooms[0];
            Vector3 center = CellToWorld(entrance.x + entrance.width / 2, entrance.y + entrance.height / 2);
            player.position = center + Vector3.up * 2f;
        }

        private void EnsureMaterials()
        {
            if (floorMaterial == null) floorMaterial = MakeMaterial(new Color(0.50f, 0.45f, 0.40f));
            if (wallMaterial == null) wallMaterial = MakeMaterial(new Color(0.30f, 0.30f, 0.36f));
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            return m;
        }
    }
}
