using UnityEngine;
using UnityEngine.InputSystem;

namespace Tema
{
    /// <summary>
    /// Camera third-person care urmareste playerul si orbiteaza cu mouse-ul.
    /// Se ataseaza pe Main Camera. Foloseste New Input System.
    /// Expune directiile planare (forward/right) pentru miscarea camera-relative a playerului.
    /// </summary>
    public class TemaFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float targetHeight = 1.6f;

        [Header("Orbit")]
        [SerializeField] private float distance = 8f;
        [SerializeField] private float yawSensitivity = 0.15f;
        [SerializeField] private float pitchSensitivity = 0.12f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 70f;
        [Tooltip("Daca e bifat, orbita doar cat tii apasat click dreapta (recomandat in editor).")]
        [SerializeField] private bool requireRightMouseToOrbit = true;

        [Header("Follow")]
        [SerializeField] private float followLerp = 12f;

        private float _yaw;
        private float _pitch = 20f;

        public Transform Target => target;
        public void SetTarget(Transform t) => target = t;

        private void Start()
        {
            if (target == null)
            {
                TemaPlayerController player = FindFirstObjectByType<TemaPlayerController>();
                if (player != null) target = player.transform;
            }
            _yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            bool canOrbit = !requireRightMouseToOrbit ||
                            (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (canOrbit && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _yaw += delta.x * yawSensitivity;
                _pitch -= delta.y * pitchSensitivity;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * targetHeight;
            Vector3 desired = focus - rot * Vector3.forward * distance;

            transform.position = Vector3.Lerp(transform.position, desired,
                1f - Mathf.Exp(-followLerp * Time.deltaTime));
            transform.rotation = rot;
        }

        /// <summary>Directia "inainte" a camerei proiectata pe planul orizontal.</summary>
        public Vector3 PlanarForward()
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        /// <summary>Directia "dreapta" a camerei proiectata pe planul orizontal.</summary>
        public Vector3 PlanarRight()
        {
            Vector3 r = transform.right;
            r.y = 0f;
            return r.sqrMagnitude > 0.0001f ? r.normalized : Vector3.right;
        }
    }
}
