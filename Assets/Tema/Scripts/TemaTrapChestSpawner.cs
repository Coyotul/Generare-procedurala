using UnityEngine;

namespace Tema
{
    public class TemaTrapChestSpawner : MonoBehaviour
    {
        [SerializeField] private TemaDungeon3D dungeon;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private GameObject chestPrefab;

        [SerializeField] private int trapCount = 6;
        [SerializeField] private int chestCount = 4;
        [SerializeField] private int seed = 909;
        [SerializeField] private float triggerRadius = 1.2f;

        [SerializeField] private string trapMessage = "Ai calcat pe o capcana!";
        [SerializeField] private string chestMessage = "Ai gasit un cufar!";

        private System.Random _rng;

        private void Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<TemaDungeon3D>();
            if (dungeon == null)
            {
                Debug.LogWarning("[TemaTrapChestSpawner] Lipseste dungeon.", this);
                return;
            }
            dungeon.EnsureGenerated();
            _rng = new System.Random(seed);

            for (int i = 0; i < trapCount; i++) Spawn(trapPrefab, trapMessage, "Trap", i);
            for (int i = 0; i < chestCount; i++) Spawn(chestPrefab, chestMessage, "Chest", i);
        }

        private void Spawn(GameObject prefab, string message, string label, int index)
        {
            if (prefab == null) return;
            if (!dungeon.TryGetRandomFloorPosition(_rng, out Vector3 pos)) return;

            Quaternion rot = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);
            GameObject obj = Instantiate(prefab, pos, rot, transform);
            obj.name = $"{label}_{index}";

            Collider col = obj.GetComponentInChildren<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                SphereCollider sc = obj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = triggerRadius;
            }

            TemaInteractable interactable = obj.GetComponent<TemaInteractable>();
            if (interactable == null) interactable = obj.AddComponent<TemaInteractable>();
            interactable.Setup(message);
        }
    }
}
