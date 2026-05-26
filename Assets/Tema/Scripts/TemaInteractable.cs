using UnityEngine;

namespace Tema
{
    public class TemaInteractable : MonoBehaviour
    {
        public string message = "";
        public float displayDuration = 2.5f;

        private float _showUntil = -1f;
        private GUIStyle _style;

        public void Setup(string msg)
        {
            message = msg;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<TemaPlayerController>() == null) return;
            _showUntil = Time.time + displayDuration;
        }

        private void OnGUI()
        {
            if (Time.time > _showUntil) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 32;
                _style.fontStyle = FontStyle.Bold;
                _style.alignment = TextAnchor.MiddleCenter;
                _style.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(0f, Screen.height * 0.25f, Screen.width, 60f), message, _style);
        }
    }
}
