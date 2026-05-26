using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Stats aleatorii ale unei sabii, generate la spawn. Doar pentru afisare/log
    /// (indeplinesc cerinta de "stats diferite"), nefolosite in gameplay deocamdata.
    /// </summary>
    public class TemaSword : MonoBehaviour
    {
        public string swordType;
        public int damage;
        public float attackSpeed;
        public float range;
    }
}
