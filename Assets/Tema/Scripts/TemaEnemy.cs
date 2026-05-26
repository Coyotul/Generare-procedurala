using UnityEngine;

namespace Tema
{
    /// <summary>
    /// Stats aleatorii ale unui inamic, generate la spawn. Sunt doar pentru afisare/log
    /// (indeplinesc cerinta de "caracteristici diferite"), nu sunt folosite in gameplay deocamdata.
    /// </summary>
    public class TemaEnemy : MonoBehaviour
    {
        public string enemyType;
        public int hp;
        public int damage;
        public int armor;
    }
}
