using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tema
{
    /// <summary>
    /// Pus pe fiecare instanta de portal. Cand playerul intra in trigger,
    /// incarca scena de dungeon. CharacterController-ul playerului declanseaza
    /// OnTriggerEnter pe colliderele setate ca trigger (fara Rigidbody).
    /// </summary>
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
