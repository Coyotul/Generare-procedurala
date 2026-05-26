using UnityEngine;

namespace Tema
{
    public class TemaEnemySpawner : MonoBehaviour
    {
        [SerializeField] private TemaDungeon3D dungeon;
        [SerializeField] private GameObject enemyBodyPrefab;
        [SerializeField] private RuntimeAnimatorController animatorController;

        [SerializeField] private int enemyCount = 5;
        [SerializeField] private int seed = 2024;

        private static readonly string[] Types = { "Grunt", "Brute", "Assassin" };

        private System.Random _rng;

        private void Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<TemaDungeon3D>();
            if (dungeon == null || enemyBodyPrefab == null)
            {
                Debug.LogWarning("[TemaEnemySpawner] Lipseste dungeon sau enemyBodyPrefab.", this);
                return;
            }
            dungeon.EnsureGenerated();

            _rng = new System.Random(seed);
            for (int i = 0; i < enemyCount; i++)
            {
                if (dungeon.TryGetRandomFloorPosition(_rng, out Vector3 pos))
                    CreateEnemy(i, pos);
            }
        }

        private void CreateEnemy(int index, Vector3 position)
        {
            Quaternion rot = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);
            GameObject body = Instantiate(enemyBodyPrefab, position, rot, transform);
            body.name = $"Enemy_{index}";

            Animator anim = body.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (animatorController != null) anim.runtimeAnimatorController = animatorController;
                anim.applyRootMotion = false;
            }

            TemaEnemy enemy = body.AddComponent<TemaEnemy>();
            enemy.enemyType = Types[_rng.Next(Types.Length)];
            enemy.hp = _rng.Next(50, 201);
            enemy.damage = _rng.Next(8, 31);
            enemy.armor = _rng.Next(0, 16);

            Debug.Log($"[EnemySpawner] Spawned enemy #{index}: type={enemy.enemyType}, " +
                      $"HP={enemy.hp}, DMG={enemy.damage}, ARMOR={enemy.armor}", body);
        }
    }
}
