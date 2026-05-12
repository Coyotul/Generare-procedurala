using System;
using UnityEngine;

namespace Lab6
{
    public static class Lab6NpcGenerator
    {
        private static readonly string[] FirstNames =
        {
            "Aria", "Borin", "Cael", "Dara", "Elwin", "Falen", "Goren", "Hilda",
            "Ithra", "Joren", "Kael", "Lyra", "Morn", "Nyssa", "Orin", "Pyra",
            "Quil", "Ravi", "Sora", "Theron", "Ulfar", "Vera", "Wren", "Xena",
            "Yari", "Zog", "Elar", "Mira", "Tobin", "Selka"
        };

        private static readonly string[] Surnames =
        {
            "Stormblade", "the Quick", "Ironhand", "the Wise", "Nightshade",
            "Goldleaf", "Stoneborn", "the Cruel", "Frostwalker", "Emberheart",
            "of Ashen Vale", "the Pale", "Brightspear", "Hollowstep"
        };

        private static readonly (NpcClass cls, float weight)[] ClassWeights =
        {
            (NpcClass.Warrior, 0.30f),
            (NpcClass.Mage,    0.20f),
            (NpcClass.Rogue,   0.25f),
            (NpcClass.Archer,  0.15f),
            (NpcClass.Paladin, 0.10f),
        };

        public static Npc Generate(int id)
        {
            NpcClass cls = PickWeightedClass();
            (float minHp, float maxHp, float minDmg, float maxDmg, float minArm, float maxArm) = StatRanges(cls);

            float hp = UnityEngine.Random.Range(minHp, maxHp);
            float dmg = UnityEngine.Random.Range(minDmg, maxDmg);
            float arm = UnityEngine.Random.Range(minArm, maxArm);

            Trait trait = (Trait)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Trait)).Length);
            ApplyTrait(trait, ref hp, ref dmg, ref arm);

            string name = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)];
            if (UnityEngine.Random.value < 0.55f)
            {
                name += " " + Surnames[UnityEngine.Random.Range(0, Surnames.Length)];
            }

            float roundedHp = Mathf.Round(hp);

            return new Npc
            {
                id = id,
                name = name,
                cls = cls,
                trait = trait,
                hp = roundedHp,
                maxHp = roundedHp,
                damage = Mathf.Round(dmg),
                armor = Mathf.Round(arm),
                locationIndex = 0,
                alive = true
            };
        }

        private static NpcClass PickWeightedClass()
        {
            float total = 0f;
            foreach (var pair in ClassWeights) total += pair.weight;

            float roll = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var pair in ClassWeights)
            {
                acc += pair.weight;
                if (roll <= acc) return pair.cls;
            }
            return ClassWeights[ClassWeights.Length - 1].cls;
        }

        public static (float minHp, float maxHp, float minDmg, float maxDmg, float minArm, float maxArm) StatRanges(NpcClass cls)
        {
            switch (cls)
            {
                case NpcClass.Warrior: return ( 80, 120, 15, 25, 10, 20);
                case NpcClass.Mage:    return ( 40,  65, 30, 50,  2,  5);
                case NpcClass.Rogue:   return ( 55,  80, 20, 35,  5, 10);
                case NpcClass.Archer:  return ( 60,  85, 18, 30,  4,  8);
                case NpcClass.Paladin: return ( 90, 130, 12, 20, 15, 25);
            }
            return (50, 100, 10, 20, 5, 10);
        }

        public static void ApplyTrait(Trait t, ref float hp, ref float dmg, ref float armor)
        {
            switch (t)
            {
                case Trait.Aggressive: hp *= 0.9f;  dmg *= 1.3f;                     break;
                case Trait.Coward:     hp *= 0.8f;                  armor *= 0.7f;   break;
                case Trait.Brave:      hp *= 1.1f;  dmg *= 1.1f;                     break;
                case Trait.Cunning:                 dmg *= 1.2f;    armor *= 1.1f;   break;
                case Trait.Peaceful:   hp *= 1.15f; dmg *= 0.7f;                     break;
            }
        }

        public static string TraitDescription(Trait t)
        {
            switch (t)
            {
                case Trait.Aggressive: return "Aggressive (HPx0.9, DMGx1.3) - attacks first";
                case Trait.Coward:     return "Coward (HPx0.8, ARMx0.7) - flees below 30% HP";
                case Trait.Brave:      return "Brave (HPx1.1, DMGx1.1) - never flees";
                case Trait.Cunning:    return "Cunning (DMGx1.2, ARMx1.1) - targets weakest";
                case Trait.Peaceful:   return "Peaceful (HPx1.15, DMGx0.7) - only counterattacks";
            }
            return t.ToString();
        }
    }
}
