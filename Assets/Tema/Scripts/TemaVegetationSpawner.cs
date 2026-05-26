using UnityEngine;

namespace Tema
{
    public class TemaVegetationSpawner : MonoBehaviour
    {
        [SerializeField] private TemaTerrain terrain;

        [SerializeField] private int treeCount = 40;
        [SerializeField] private int bushCount = 60;
        [SerializeField] private int seed = 12345;
        [SerializeField] private int maxPlacementTries = 12;

        [SerializeField] private float minElevation = 0.40f;
        [SerializeField] private float maxElevation = 0.85f;

        private System.Random _rng;

        private void Start()
        {
            if (terrain == null)
            {
                Debug.LogWarning("[TemaVegetationSpawner] Terrain reference missing.", this);
                return;
            }
            if (!terrain.IsGenerated) terrain.Generate();

            _rng = new System.Random(seed);

            for (int i = 0; i < treeCount; i++) SpawnPlant(true, i);
            for (int i = 0; i < bushCount; i++) SpawnPlant(false, i);
        }

        private void SpawnPlant(bool isTree, int index)
        {
            for (int attempt = 0; attempt < Mathf.Max(1, maxPlacementTries); attempt++)
            {
                float worldX = Mathf.Lerp(terrain.MinWorldX, terrain.MaxWorldX, (float)_rng.NextDouble());
                float worldZ = Mathf.Lerp(terrain.MinWorldZ, terrain.MaxWorldZ, (float)_rng.NextDouble());

                float e = terrain.SampleElevation01(worldX, worldZ);
                if (e < minElevation || e > maxElevation) continue;

                float y = terrain.SampleHeight(worldX, worldZ);
                CreatePlant(isTree, index, new Vector3(worldX, y, worldZ));
                return;
            }
        }

        private void CreatePlant(bool isTree, int index, Vector3 position)
        {
            GameObject go = new GameObject(isTree ? $"Tree_{index}" : $"Bush_{index}");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);

            TemaLSystemPlant plant = go.AddComponent<TemaLSystemPlant>();

            if (isTree)
            {
                plant.Configure(
                    "X",
                    new[] { "X=F-[[X]+X]+F[+FX]-X", "F=FF" },
                    4,
                    22.5f,
                    0.35f,
                    new Color(0.25f, 0.50f, 0.22f));
            }
            else
            {
                plant.Configure(
                    "X",
                    new[] { "X=F[+X]F[-X]+X", "F=FF" },
                    3,
                    20f,
                    0.22f,
                    new Color(0.45f, 0.65f, 0.30f));
            }

            plant.Build();
        }
    }
}
