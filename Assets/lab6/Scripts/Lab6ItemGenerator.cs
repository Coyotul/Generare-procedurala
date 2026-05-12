using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab6
{
    public static class Lab6ItemGenerator
    {
        private const float BaseBudget = 60f;

        private static readonly (Rarity r, float weight)[] RarityWeights =
        {
            (Rarity.Common,    0.55f),
            (Rarity.Uncommon,  0.25f),
            (Rarity.Rare,      0.12f),
            (Rarity.Epic,      0.06f),
            (Rarity.Legendary, 0.02f),
        };

        private static readonly string[] CommonPrefixes    = { "Plain", "Crude", "Rusty", "Worn", "Battered" };
        private static readonly string[] UncommonPrefixes  = { "Sturdy", "Sharp", "Polished", "Fine", "Honed" };
        private static readonly string[] RarePrefixes      = { "Enchanted", "Master", "Glorious", "Hallowed", "Runed" };
        private static readonly string[] EpicPrefixes      = { "Demon's", "Dragon", "Mythical", "Ancient", "Voidforged" };
        private static readonly string[] LegendaryPrefixes = { "Godslayer", "Worldforged", "Eternal", "Soulbound", "Starborne" };

        public static Item Generate(int id)
        {
            Rarity rarity = PickWeightedRarity();
            ItemType type = (ItemType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ItemType)).Length);

            float budget = BaseBudget * StatMultiplier(rarity);
            float dmg = UnityEngine.Random.Range(0.4f, 0.7f) * budget;
            float dur = budget - dmg + budget * 0.3f;

            List<Ability> abilities = new List<Ability>();
            if (rarity == Rarity.Epic)
            {
                abilities.Add(RandomAbility(null));
            }
            else if (rarity == Rarity.Legendary)
            {
                Ability a = RandomAbility(null);
                abilities.Add(a);
                abilities.Add(RandomAbility(a.type));
            }

            string prefix = PrefixForRarity(rarity);
            string name = $"{prefix} {type}";

            return new Item
            {
                id = id,
                name = name,
                type = type,
                rarity = rarity,
                damage = Mathf.Round(dmg),
                durability = Mathf.Round(dur),
                abilities = abilities
            };
        }

        public static float StatMultiplier(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common:    return 1.0f;
                case Rarity.Uncommon:  return 1.3f;
                case Rarity.Rare:      return 1.7f;
                case Rarity.Epic:      return 2.2f;
                case Rarity.Legendary: return 3.0f;
            }
            return 1f;
        }

        private static Rarity PickWeightedRarity()
        {
            float total = 0f;
            foreach (var pair in RarityWeights) total += pair.weight;

            float roll = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var pair in RarityWeights)
            {
                acc += pair.weight;
                if (roll <= acc) return pair.r;
            }
            return RarityWeights[RarityWeights.Length - 1].r;
        }

        private static string PrefixForRarity(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common:    return CommonPrefixes[UnityEngine.Random.Range(0, CommonPrefixes.Length)];
                case Rarity.Uncommon:  return UncommonPrefixes[UnityEngine.Random.Range(0, UncommonPrefixes.Length)];
                case Rarity.Rare:      return RarePrefixes[UnityEngine.Random.Range(0, RarePrefixes.Length)];
                case Rarity.Epic:      return EpicPrefixes[UnityEngine.Random.Range(0, EpicPrefixes.Length)];
                case Rarity.Legendary: return LegendaryPrefixes[UnityEngine.Random.Range(0, LegendaryPrefixes.Length)];
            }
            return "";
        }

        private static Ability RandomAbility(AbilityType? exclude)
        {
            AbilityType[] all = (AbilityType[])Enum.GetValues(typeof(AbilityType));
            AbilityType picked;
            int safety = 0;
            do
            {
                picked = all[UnityEngine.Random.Range(0, all.Length)];
                safety++;
            } while (exclude.HasValue && picked == exclude.Value && safety < 16);

            Ability a = new Ability { type = picked };
            switch (picked)
            {
                case AbilityType.Poison:
                    a.paramA = UnityEngine.Random.Range(3f, 8f);
                    a.paramB = UnityEngine.Random.Range(3f, 5f);
                    break;
                case AbilityType.Lifesteal:
                    a.paramA = UnityEngine.Random.Range(0.15f, 0.30f);
                    break;
                case AbilityType.FireDamage:
                    a.paramA = UnityEngine.Random.Range(5f, 15f);
                    a.paramB = 2f;
                    break;
                case AbilityType.IceSlow:
                    a.paramA = UnityEngine.Random.Range(0.20f, 0.50f);
                    a.paramB = UnityEngine.Random.Range(2f, 4f);
                    break;
                case AbilityType.Thunder:
                    a.paramA = UnityEngine.Random.Range(0.15f, 0.35f);
                    a.paramB = 1f;
                    break;
            }
            return a;
        }

        public static string Stars(Rarity r)
        {
            int count = (int)r + 1;
            return new string('*', count);
        }
    }
}
