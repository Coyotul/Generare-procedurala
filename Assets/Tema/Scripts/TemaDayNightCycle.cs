using UnityEngine;

namespace Tema
{
    [RequireComponent(typeof(Light))]
    public class TemaDayNightCycle : MonoBehaviour
    {
        [SerializeField] private float dayLengthSeconds = 120f;
        [SerializeField, Range(0f, 1f)] private float startTime01 = 0.3f;
        [SerializeField] private float sunYaw = 170f;

        [SerializeField] private Color dayColor = new Color(1f, 0.96f, 0.84f);
        [SerializeField] private Color duskColor = new Color(1f, 0.55f, 0.30f);
        [SerializeField] private Color nightColor = new Color(0.25f, 0.32f, 0.55f);
        [SerializeField] private float maxIntensity = 1.2f;
        [SerializeField] private float nightIntensity = 0.05f;

        [SerializeField] private bool affectAmbient = true;
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.57f, 0.60f);
        [SerializeField] private Color nightAmbient = new Color(0.10f, 0.12f, 0.20f);

        private Light _light;
        private float _time01;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _time01 = startTime01;
            if (affectAmbient) RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        private void Update()
        {
            if (dayLengthSeconds > 0.01f)
                _time01 = Mathf.Repeat(_time01 + Time.deltaTime / dayLengthSeconds, 1f);

            float sunAngle = _time01 * 360f - 90f;
            transform.rotation = Quaternion.Euler(sunAngle, sunYaw, 0f);

            float elevation = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            float dayFactor = Mathf.Clamp01(elevation);

            Color sunColor = elevation < 0f
                ? nightColor
                : Color.Lerp(duskColor, dayColor, dayFactor);
            _light.color = sunColor;
            _light.intensity = Mathf.Lerp(nightIntensity, maxIntensity, dayFactor);

            if (affectAmbient)
                RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, dayFactor);
        }
    }
}
