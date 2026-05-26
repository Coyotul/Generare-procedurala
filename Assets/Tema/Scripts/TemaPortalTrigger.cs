using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tema
{
    public class TemaPortalTrigger : MonoBehaviour
    {
        [SerializeField] private string dungeonSceneName = "Scena2";
        private bool _triggered;

        public void SetScene(string sceneName) => dungeonSceneName = sceneName;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (other.GetComponentInParent<TemaPlayerController>() == null) return;

            _triggered = true;
            SceneManager.LoadScene(dungeonSceneName);
        }
    }
}
