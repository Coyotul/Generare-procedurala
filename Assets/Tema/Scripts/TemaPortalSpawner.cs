using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Spawneaza mai multe portaluri (default 20) la pozitii XZ aleatorii, ingropate sub
    /// suprafata terenului. Nu exista niciun indiciu la suprafata - playerul trebuie sa sape
    /// cu lopata (tine L) ca sa dea de unul. La intrare in portal se incarca scena de dungeon.
    /// Fiecare instanta primeste un collider-trigger + TemaPortalTrigger la runtime
    /// (prefabul ramane neatins).
    /// </summary>
    public class TemaPortalSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TemaTerrain terrain;
        [SerializeField] private GameObject portalPrefab;

        [Header("Spawn")]
        [SerializeField] private int portalCount = 20;
        [SerializeField] private int seed = 777;
        [Tooltip("Cat de adanc sub suprafata e centrul portalului (unitati world).")]
        [SerializeField] private float buryDepth = 4f;
        [Tooltip("Raza trigger-ului daca prefabul nu are collider propriu.")]
        [SerializeField] private float fallbackTriggerRadius = 1.2f;

        [Header("Scene")]
        [SerializeField] private string dungeonSceneName = "Scena2";

        private System.Random _rng;

        private void Start()
        {
            if (terrain == null) terrain = FindFirstObjectByType<TemaTerrain>();
            if (terrain == null || portalPrefab == null)
            {
                Debug.LogWarning("[TemaPortalSpawner] Lipseste terrain sau portalPrefab.", this);
                return;
            }
            if (!terrain.IsGenerated) terrain.Generate();

            _rng = new System.Random(seed);
            for (int i = 0; i < portalCount; i++) SpawnPortal(i);
        }

        private void SpawnPortal(int index)
        {
            float worldX = Mathf.Lerp(terrain.MinWorldX, terrain.MaxWorldX, (float)_rng.NextDouble());
            float worldZ = Mathf.Lerp(terrain.MinWorldZ, terrain.MaxWorldZ, (float)_rng.NextDouble());
            float surfaceY = terrain.SampleHeight(worldX, worldZ);

            Vector3 pos = new Vector3(worldX, surfaceY - buryDepth, worldZ);
            float yaw = (float)_rng.NextDouble() * 360f;

            GameObject portal = Instantiate(portalPrefab, pos, Quaternion.Euler(0f, yaw, 0f), transform);
            portal.name = $"Portal_{index}";

            // colliderul devine trigger ca playerul sa poata "intra" in portal
            Collider col = portal.GetComponentInChildren<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                SphereCollider sc = portal.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = fallbackTriggerRadius;
            }

            TemaPortalTrigger trigger = portal.GetComponent<TemaPortalTrigger>();
            if (trigger == null) trigger = portal.AddComponent<TemaPortalTrigger>();
            trigger.SetScene(dungeonSceneName);
        }
    }
}
