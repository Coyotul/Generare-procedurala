using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Imprastie sabii in dungeon (Scena 2) la pozitii aleatorii pe podea, plutind deasupra ei.
    /// Fiecare sabie e aleasa aleator din lista din Inspector si primeste un collider-trigger +
    /// TemaSwordPickup. Codul doar alege random din lista expusa in Inspector.
    /// </summary>
    public class TemaSwordSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TemaDungeon3D dungeon;
        [Tooltip("Prefab-uri de sabii. Trage-le aici din Hovl Studio/MoonSword/Prefabs.")]
        [SerializeField] private GameObject[] swordPrefabs;

        [Header("Spawn")]
        [SerializeField] private int swordCount = 8;
        [SerializeField] private int seed = 555;
        [SerializeField] private float hoverHeight = 1.5f;
        [SerializeField] private float swordScale = 1f;
        [SerializeField] private float triggerRadius = 1.2f;

        private static readonly string[] SwordTypes = { "Light", "Balanced", "Heavy" };

        private System.Random _rng;

        private void Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<TemaDungeon3D>();
            if (dungeon == null || swordPrefabs == null || swordPrefabs.Length == 0)
            {
                Debug.LogWarning("[TemaSwordSpawner] Lipseste dungeon sau swordPrefabs.", this);
                return;
            }
            dungeon.EnsureGenerated();

            _rng = new System.Random(seed);
            for (int i = 0; i < swordCount; i++)
            {
                if (dungeon.TryGetRandomFloorPosition(_rng, out Vector3 pos))
                    CreateSword(i, pos + Vector3.up * hoverHeight);
            }
        }

        private void CreateSword(int index, Vector3 position)
        {
            GameObject prefab = swordPrefabs[_rng.Next(swordPrefabs.Length)];
            if (prefab == null) return;

            GameObject sword = Instantiate(prefab, position, Quaternion.identity, transform);
            sword.name = $"SwordPickup_{index}";
            sword.transform.localScale = Vector3.one * swordScale;

            SphereCollider col = sword.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = triggerRadius;

            TemaSwordPickup pickup = sword.AddComponent<TemaSwordPickup>();
            pickup.Setup(prefab);

            // stats random (doar pentru log)
            TemaSword stats = sword.AddComponent<TemaSword>();
            stats.swordType = SwordTypes[_rng.Next(SwordTypes.Length)];
            stats.damage = _rng.Next(10, 56);
            stats.attackSpeed = Mathf.Round((0.5f + (float)_rng.NextDouble() * 1.7f) * 10f) / 10f;
            stats.range = Mathf.Round((1.2f + (float)_rng.NextDouble() * 1.6f) * 10f) / 10f;

            Debug.Log($"[SwordSpawner] Spawned sword #{index}: {prefab.name}, type={stats.swordType}, " +
                      $"DMG={stats.damage}, ATK_SPD={stats.attackSpeed}, RANGE={stats.range}", sword);
        }
    }
}
