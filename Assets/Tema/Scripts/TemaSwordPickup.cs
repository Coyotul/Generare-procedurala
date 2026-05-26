using UnityEngine;

namespace Tema
{
    public class TemaSwordPickup : MonoBehaviour
    {
        [SerializeField] private GameObject swordPrefab;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float hoverAmplitude = 0.35f;
        [SerializeField] private float hoverFrequency = 0.8f;

        private Vector3 _baseLocalPos;
        private float _phase;
        private bool _picked;

        public void Setup(GameObject prefab) => swordPrefab = prefab;

        private void Start()
        {
            _baseLocalPos = transform.localPosition;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
            Vector3 p = _baseLocalPos;
            p.y += Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f + _phase) * hoverAmplitude;
            transform.localPosition = p;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_picked) return;
            TemaPlayerWeapon weapon = other.GetComponentInParent<TemaPlayerWeapon>();
            if (weapon == null) return;

            _picked = true;
            weapon.Equip(swordPrefab != null ? swordPrefab : gameObject);
            Destroy(gameObject);
        }
    }
}
