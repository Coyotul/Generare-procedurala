using UnityEngine;
using UnityEngine.InputSystem;

namespace Tema
{
    /// <summary>
    /// Unealta de sapat (lopata): cat timp se tine apasat click stanga, trage un ray din camera
    /// prin pozitia mouse-ului si coboara vertecsii terenului in punctul de impact, in timp real.
    /// Acopera bonusurile de "actualizare procedurala in timp real" + "input care influenteaza generarea".
    /// Foloseste New Input System (proiectul e setat pe Input System Package).
    /// </summary>
    public class TemaDiggingTool : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TemaTerrain terrain;
        [SerializeField] private Camera cam;

        [Header("Dig")]
        [SerializeField] private float digRadius = 3f;
        [SerializeField] private float digStrength = 8f;       // unitati/secunda
        [SerializeField] private float maxRayDistance = 1000f;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (terrain == null || cam == null) return;
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                // sapam doar daca am lovit terenul (are componenta TemaTerrain)
                if (hit.collider.GetComponent<TemaTerrain>() != null)
                    terrain.Dig(hit.point, digRadius, digStrength * Time.deltaTime);
            }
        }
    }
}
