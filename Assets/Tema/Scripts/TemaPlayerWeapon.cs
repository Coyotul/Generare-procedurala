using UnityEngine;
using UnityEngine.InputSystem;

namespace Tema
{
    /// <summary>
    /// Arma curenta a playerului. Cand ridica o sabie, vechea arma e inlocuita cu noua
    /// (instanta din prefabul sabiei, atasata la mana). Atacul (click stanga) doar
    /// porneste particle system-urile sabiei.
    /// </summary>
    public class TemaPlayerWeapon : MonoBehaviour
    {
        [Header("Hold")]
        [Tooltip("Unde se tine arma. Daca e gol, se cauta osul mainii; altfel se ataseaza la player.")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Vector3 holdLocalPosition = new Vector3(0.2f, 1f, 0.3f);
        [SerializeField] private Vector3 holdLocalEuler = Vector3.zero;
        [SerializeField] private float holdScale = 1f;

        private GameObject _current;

        private void Awake()
        {
            if (weaponHolder == null) weaponHolder = FindHandBone();
            if (weaponHolder == null) weaponHolder = transform;
        }

        private Transform FindHandBone()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("hand") && (n.Contains("right") || n.EndsWith("_r") || n.EndsWith(".r") || n.EndsWith("r")))
                    return t;
            }
            return null;
        }

        public void Equip(GameObject swordPrefab)
        {
            if (swordPrefab == null) return;
            if (_current != null) Destroy(_current);

            _current = Instantiate(swordPrefab, weaponHolder);
            _current.transform.localPosition = holdLocalPosition;
            _current.transform.localRotation = Quaternion.Euler(holdLocalEuler);
            _current.transform.localScale = Vector3.one * holdScale;
        }

        private void Update()
        {
            if (_current == null) return;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                Attack();
        }

        private void Attack()
        {
            foreach (ParticleSystem ps in _current.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }
    }
}
