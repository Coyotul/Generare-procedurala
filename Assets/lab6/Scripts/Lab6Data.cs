using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab6
{
    public enum NpcClass { Warrior, Mage, Rogue, Archer, Paladin }
    public enum Trait { Aggressive, Coward, Brave, Cunning, Peaceful }
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum ItemType { Sword, Axe, Bow, Staff, Dagger, Shield, Helmet, Armor }
    public enum AbilityType { Poison, Lifesteal, FireDamage, IceSlow, Thunder }
    public enum LocationType { Dungeon, Tavern, Market, Forest, Plain }

    [Serializable]
    public class Ability
    {
        public AbilityType type;
        public float paramA;
        public float paramB;

        public string Format()
        {
            switch (type)
            {
                case AbilityType.Poison:
                    return $"Poison ({Mathf.RoundToInt(paramA)} dmg/tick x {Mathf.RoundToInt(paramB)} ticks)";
                case AbilityType.Lifesteal:
                    return $"Lifesteal ({Mathf.RoundToInt(paramA * 100f)}%)";
                case AbilityType.FireDamage:
                    return $"Fire (+{Mathf.RoundToInt(paramA)} dmg, {Mathf.RoundToInt(paramB)} tick burn)";
                case AbilityType.IceSlow:
                    return $"Ice Slow ({Mathf.RoundToInt(paramA * 100f)}%, {paramB:F1}s)";
                case AbilityType.Thunder:
                    return $"Thunder ({Mathf.RoundToInt(paramA * 100f)}% stun, 1s)";
            }
            return type.ToString();
        }
    }

    [Serializable]
    public class Npc
    {
        public int id;
        public string name;
        public NpcClass cls;
        public Trait trait;
        public float hp;
        public float maxHp;
        public float damage;
        public float armor;
        public int locationIndex;
        public bool alive = true;
    }

    [Serializable]
    public class Item
    {
        public int id;
        public string name;
        public ItemType type;
        public Rarity rarity;
        public float damage;
        public float durability;
        public List<Ability> abilities = new List<Ability>();
    }

    [Serializable]
    public class RelationshipEntry
    {
        public int from;
        public int to;
        public int value;
    }

    [Serializable]
    public class SaveData
    {
        public List<Npc> npcs = new List<Npc>();
        public List<Item> items = new List<Item>();
        public int nextNpcId = 1;
        public int nextItemId = 1;
    }

    public static class Lab6Colors
    {
        public static readonly Color Common     = new Color(0.85f, 0.85f, 0.85f);
        public static readonly Color Uncommon   = new Color(0.30f, 0.85f, 0.30f);
        public static readonly Color Rare       = new Color(0.30f, 0.55f, 1.00f);
        public static readonly Color Epic       = new Color(0.75f, 0.30f, 0.95f);
        public static readonly Color Legendary  = new Color(1.00f, 0.55f, 0.10f);

        public static Color For(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon:  return Uncommon;
                case Rarity.Rare:      return Rare;
                case Rarity.Epic:      return Epic;
                case Rarity.Legendary: return Legendary;
                default:               return Common;
            }
        }

        public static Color ForClass(NpcClass c)
        {
            switch (c)
            {
                case NpcClass.Warrior: return new Color(0.78f, 0.20f, 0.20f);
                case NpcClass.Mage:    return new Color(0.25f, 0.45f, 0.90f);
                case NpcClass.Rogue:   return new Color(0.20f, 0.55f, 0.30f);
                case NpcClass.Archer:  return new Color(0.85f, 0.70f, 0.20f);
                case NpcClass.Paladin: return new Color(0.90f, 0.88f, 0.78f);
            }
            return Color.gray;
        }

        public static Color ForLocation(LocationType loc)
        {
            switch (loc)
            {
                case LocationType.Dungeon: return new Color(0.35f, 0.20f, 0.30f);
                case LocationType.Tavern:  return new Color(0.55f, 0.35f, 0.15f);
                case LocationType.Market:  return new Color(0.85f, 0.65f, 0.30f);
                case LocationType.Forest:  return new Color(0.20f, 0.45f, 0.25f);
                case LocationType.Plain:   return new Color(0.55f, 0.70f, 0.35f);
            }
            return Color.gray;
        }
    }
}
