# Laboratorul 6 — NPC / Item / World Simulation (Unity / C#)

## Cerinta laboratorului

Task 1 - Generator NPC:
- NPC = combinatie aleatoare de nume, clasa, viata, damage
- Clasa selectata prin distributie ponderata (Warrior 30%, Mage 20%, Rogue 25%,
  Archer 15%, Paladin 10%)
- Fiecare NPC are si o trasatura de personalitate care modifica atributele
  (Aggressive, Coward, Brave, Cunning, Peaceful)
- Portret afisat pe baza clasei
- Generare la apasarea unui buton

Task 2 - Generator Item:
- Item = tip + raritate + damage + durability din stat budget
- Itemele Epic si Legendary au abilitati speciale cu parametri proprii
  (Poison, Lifesteal, FireDamage, IceSlow, Thunder)
- Nume procedural (prefix + tip)
- Culoare bazata pe raritate
- Generare la buton

Task 3 - World Simulation:
- 4-6 NPC-uri distribuite in 5 locatii predefinite
- Buton Advance Day -> fiecare NPC executa actiune dupa personalitate, HP, relatii
- Sistem de relatii [-100, +100] cu update asimetric
- Narativ generat din template-uri (min 3 variante per tip)
- Sfarsit: 1 NPC ramas viu SAU 10 zile + sumar procedural

Bonus 1: Galerie scrolabila + rename NPCs/items + persistenta intre rulari
Bonus 2: Rarity stars + animatie pentru cel mai rar tip
Bonus 3: Harta vizuala cu locatia NPC-urilor, update la fiecare tick

---

## Arhitectura solutiei

Tot UI-ul e construit procedural in cod la runtime (fara prefab-uri de UI):
scena are doar Main Camera + un GameObject "Lab6Bootstrap" cu scriptul Lab6Main.
La Play, scriptul construieste Canvas, EventSystem si toate panelurile pentru
cele 4 tab-uri (NPC, Item, World, Gallery).

Fisiere:
- Lab6Data.cs           - enums (NpcClass, Trait, Rarity, etc.) + clase
                          serializabile (Npc, Item, Ability, SaveData) + paleta de
                          culori (Lab6Colors)
- Lab6NpcGenerator.cs   - roluire NPC: clasa ponderata, stat-uri in range,
                          trasatura random + multiplicatori
- Lab6ItemGenerator.cs  - raritate ponderata, stat budget, abilitati Epic/Legendary,
                          prefix-uri procedurale
- Lab6World.cs          - simularea: decision tree, relatii asimetrice, narativ
- Lab6SaveSystem.cs     - JSON save/load la Application.persistentDataPath
- Lab6Ui.cs             - helper pentru a construi UI in cod (Panel, Label,
                          Button, Input, ScrollView)
- Lab6Main.cs           - bootstrap MonoBehaviour: Canvas, EventSystem, tab-uri,
                          handler-e

---

## Cum functioneaza, pe scurt

### Selectie ponderata (roulette wheel)

Pentru clase (NPC) si rarităti (Item):
```
total = suma ponderilor
roll = Random.value * total
acumulator = 0
foreach (optiune, pondere):
    acumulator += pondere
    if (roll <= acumulator) return optiune
```
Probabilitatea unei optiuni = pondere / total. Ponderile nu trebuie sa sumeze la 1.

### NPC - aplicare trasatura

Dupa ce extrag stat-urile in intervalul clasei (ex. Warrior HP 80-120), aplic
multiplicatorii trasaturii:
```
Aggressive: HP*0.9,  DMG*1.3
Coward:     HP*0.8,  ARM*0.7
Brave:      HP*1.1,  DMG*1.1
Cunning:    DMG*1.2, ARM*1.1
Peaceful:   HP*1.15, DMG*0.7
```
Trasatura ramane ca atribut al NPC-ului si influenteaza decizia in simulare.

### Item - stat budget

```
budget    = 60 * StatMultiplier(rarity)        # 1.0/1.3/1.7/2.2/3.0
damage    = U(0.4, 0.7) * budget               # 40-70% din buget
durability = budget - damage + 0.3*budget
```
Raritatea controleaza puterea totala. Distributia damage vs durability e random
in interval -> doua iteme de aceeasi raritate sunt comparabile dar nu identice.

Pentru Epic adaug 1 abilitate, pentru Legendary 2 distincte (a doua e re-rolled
daca pica acelasi type ca prima).

### World - decision tree pe prioritati

Pentru fiecare NPC, in ordine:
```
1. Coward + HP<30%  -> Flee (muta in alta locatie)
2. Aggressive/Cunning + tinta -> Attack
   (Cunning = cel mai slab; Aggressive = random)
3. 25% sansa (+bonus Market/Tavern) -> Trade
4. 20% sansa (+bonus Forest/Plain) -> Explore
5. Idle (loggat in jurnal 20% din timp)
```
Stop la prima conditie care se aplica.

### Relatii asimetrice

```
ChangeRelation(from, to, delta):
    rel[from->to] += delta              # efect direct
    rel[to->from] += delta * 0.7        # reactie inversa, mai slaba
    clamp ambele la [-100, +100]
```
Eveniment:
- atac:                -20 victima->agresor
- trade:               +10 ambele directii
- martor crima:        -30 toti martorii -> ucigas
- co-prezenta (zi):    +2 pentru fiecare pereche in aceeasi locatie

Folosesc `Dictionary<(int,int), int>` cu cheie tuplu - eficient pentru relatii
rare (majoritatea perechilor au 0).

### Locatii - modificatori

```
Dungeon: x1.5 agresiune
Tavern:  x0.6 agresiune, +40% trade
Market:  x0.4 agresiune, +60% trade
Forest:  x1.2 agresiune, +20% movement
Plain:   x1.0 agresiune, +10% movement
```
In Attack: `dmg = attacker.damage * AggressionMultiplier(loc) - target.armor*0.5`

### Narativ din template-uri

Minim 3 templates per tip de actiune (atack, flee, trade, explore, idle, death).
Substituirile: `{d}`, `{attacker}`, `{target}`, `{cls}`, `{loc}`, `{dmg}`, `{style}`.
Style depinde de trasatura: Aggressive="brutally", Cunning="calculatedly", etc.

### Bonus 1 - Galerie + persistenta + rename

`Lab6SaveSystem` salveaza JsonUtility.ToJson(SaveData) intr-un fisier la
`Application.persistentDataPath/lab6_save.json` la fiecare generare/rename.
Galeria afiseaza listele cu input field + buton Rename pentru fiecare.

### Bonus 2 - Stele + animatie Legendary

Numarul de stele = `(int)rarity + 1` (de la 1 la 5).
In `Lab6Main.Update()`, daca itemul curent e Legendary:
```
t = Mathf.PingPong(Time.unscaledTime * 1.5, 1)
color = Lerp(orangeBase, yellowBright, t)
scale = 1 + 0.04 * sin(Time.unscaledTime * 3.2)
```
Pulseaza culoarea si scaleaza ±4% in jurul lui 1.

### Bonus 3 - Harta

GridLayoutGroup 3x2 cu cele 5 locatii (a 6-a celula goala). Fiecare celula listeaza
NPC-urile prezente colorate dupa clasa, cu HP. Se reconstruieste la fiecare
RefreshWorldUi() dupa Advance Day.

---

## Cod sursa

### Assets/lab6/Scripts/Lab6Data.cs

```csharp
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
    public class SaveData
    {
        public List<Npc> npcs = new List<Npc>();
        public List<Item> items = new List<Item>();
        public int nextNpcId = 1;
        public int nextItemId = 1;
    }

    public static class Lab6Colors
    {
        public static readonly Color Common    = new Color(0.85f, 0.85f, 0.85f);
        public static readonly Color Uncommon  = new Color(0.30f, 0.85f, 0.30f);
        public static readonly Color Rare      = new Color(0.30f, 0.55f, 1.00f);
        public static readonly Color Epic      = new Color(0.75f, 0.30f, 0.95f);
        public static readonly Color Legendary = new Color(1.00f, 0.55f, 0.10f);

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
```

### Assets/lab6/Scripts/Lab6NpcGenerator.cs

```csharp
using System;
using UnityEngine;

namespace Lab6
{
    public static class Lab6NpcGenerator
    {
        private static readonly string[] FirstNames =
        {
            "Aria","Borin","Cael","Dara","Elwin","Falen","Goren","Hilda",
            "Ithra","Joren","Kael","Lyra","Morn","Nyssa","Orin","Pyra",
            "Quil","Ravi","Sora","Theron","Ulfar","Vera","Wren","Xena",
            "Yari","Zog","Elar","Mira","Tobin","Selka"
        };
        private static readonly string[] Surnames =
        {
            "Stormblade","the Quick","Ironhand","the Wise","Nightshade",
            "Goldleaf","Stoneborn","the Cruel","Frostwalker","Emberheart",
            "of Ashen Vale","the Pale","Brightspear","Hollowstep"
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
                name += " " + Surnames[UnityEngine.Random.Range(0, Surnames.Length)];

            float roundedHp = Mathf.Round(hp);
            return new Npc {
                id = id, name = name, cls = cls, trait = trait,
                hp = roundedHp, maxHp = roundedHp,
                damage = Mathf.Round(dmg), armor = Mathf.Round(arm),
                locationIndex = 0, alive = true
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
                case Trait.Aggressive: hp *= 0.9f;  dmg *= 1.3f;                  break;
                case Trait.Coward:     hp *= 0.8f;                  armor *= 0.7f; break;
                case Trait.Brave:      hp *= 1.1f;  dmg *= 1.1f;                  break;
                case Trait.Cunning:                 dmg *= 1.2f;    armor *= 1.1f; break;
                case Trait.Peaceful:   hp *= 1.15f; dmg *= 0.7f;                  break;
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
```

### Assets/lab6/Scripts/Lab6ItemGenerator.cs

```csharp
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

        private static readonly string[] CommonPrefixes    = { "Plain","Crude","Rusty","Worn","Battered" };
        private static readonly string[] UncommonPrefixes  = { "Sturdy","Sharp","Polished","Fine","Honed" };
        private static readonly string[] RarePrefixes      = { "Enchanted","Master","Glorious","Hallowed","Runed" };
        private static readonly string[] EpicPrefixes      = { "Demon's","Dragon","Mythical","Ancient","Voidforged" };
        private static readonly string[] LegendaryPrefixes = { "Godslayer","Worldforged","Eternal","Soulbound","Starborne" };

        public static Item Generate(int id)
        {
            Rarity rarity = PickWeightedRarity();
            ItemType type = (ItemType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ItemType)).Length);

            float budget = BaseBudget * StatMultiplier(rarity);
            float dmg = UnityEngine.Random.Range(0.4f, 0.7f) * budget;
            float dur = budget - dmg + budget * 0.3f;

            List<Ability> abilities = new List<Ability>();
            if (rarity == Rarity.Epic) abilities.Add(RandomAbility(null));
            else if (rarity == Rarity.Legendary)
            {
                Ability a = RandomAbility(null);
                abilities.Add(a);
                abilities.Add(RandomAbility(a.type));
            }

            string prefix = PrefixForRarity(rarity);
            string name = $"{prefix} {type}";

            return new Item {
                id = id, name = name, type = type, rarity = rarity,
                damage = Mathf.Round(dmg), durability = Mathf.Round(dur),
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
                    a.paramB = UnityEngine.Random.Range(3f, 5f); break;
                case AbilityType.Lifesteal:
                    a.paramA = UnityEngine.Random.Range(0.15f, 0.30f); break;
                case AbilityType.FireDamage:
                    a.paramA = UnityEngine.Random.Range(5f, 15f);
                    a.paramB = 2f; break;
                case AbilityType.IceSlow:
                    a.paramA = UnityEngine.Random.Range(0.20f, 0.50f);
                    a.paramB = UnityEngine.Random.Range(2f, 4f); break;
                case AbilityType.Thunder:
                    a.paramA = UnityEngine.Random.Range(0.15f, 0.35f);
                    a.paramB = 1f; break;
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
```

### Assets/lab6/Scripts/Lab6World.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lab6
{
    public class Lab6World
    {
        public const int MaxDays = 10;
        public List<Npc> Npcs { get; private set; } = new List<Npc>();
        public List<string> Journal { get; private set; } = new List<string>();
        public int Day { get; private set; }
        public bool Finished { get; private set; }
        public string FinalSummary { get; private set; }

        private readonly Dictionary<(int, int), int> _relations = new Dictionary<(int, int), int>();
        private static readonly LocationType[] AllLocations =
            { LocationType.Dungeon, LocationType.Tavern, LocationType.Market, LocationType.Forest, LocationType.Plain };

        public IReadOnlyList<LocationType> Locations => AllLocations;

        public void StartNew(int npcCount, int seed)
        {
            UnityEngine.Random.InitState(seed);
            Npcs.Clear(); Journal.Clear(); _relations.Clear();
            Day = 0; Finished = false; FinalSummary = null;

            int count = Mathf.Clamp(npcCount, 4, 6);
            for (int i = 0; i < count; i++)
            {
                Npc npc = Lab6NpcGenerator.Generate(i + 1);
                npc.locationIndex = UnityEngine.Random.Range(0, AllLocations.Length);
                Npcs.Add(npc);
            }
            Journal.Add($"[Day 0] {count} souls gather across the realm. The tale begins.");
        }

        public void AdvanceDay()
        {
            if (Finished) return;
            Day++;

            List<Npc> ordered = new List<Npc>(Npcs);
            Shuffle(ordered);

            foreach (Npc actor in ordered)
            {
                if (!actor.alive) continue;
                ExecuteTurn(actor);
                if (CheckEnd()) return;
            }

            ApplyCopresenceBonuses();

            if (Day >= MaxDays)
                EndSimulation("the 10-day count was reached");
        }

        private void ExecuteTurn(Npc actor)
        {
            LocationType loc = AllLocations[actor.locationIndex];
            List<Npc> sameLocation = NpcsAt(actor.locationIndex, exclude: actor);
            float hpPercent = actor.maxHp <= 0f ? 0f : actor.hp / actor.maxHp;

            // P1: Coward survival
            if (actor.trait == Trait.Coward && hpPercent < 0.30f)
            { Flee(actor, loc); return; }

            // P2: Attack
            bool wantsToAttack = (actor.trait == Trait.Aggressive || actor.trait == Trait.Cunning)
                                 && sameLocation.Count > 0;
            if (wantsToAttack)
            {
                Npc target = (actor.trait == Trait.Cunning)
                    ? FindLowestHpEnemy(actor, sameLocation)
                    : sameLocation[UnityEngine.Random.Range(0, sameLocation.Count)];
                Attack(actor, target, loc); return;
            }

            // P3: Trade
            if (sameLocation.Count > 0 && UnityEngine.Random.value < TradeChance(loc))
            {
                Npc partner = PickAlly(actor, sameLocation);
                if (partner != null) { Trade(actor, partner, loc); return; }
            }

            // P4: Explore
            float exploreChance = 0.20f + MovementBonus(loc);
            if (UnityEngine.Random.value < exploreChance)
            { Explore(actor, loc); return; }

            // P5: Idle
            Idle(actor, loc);
        }

        private void Attack(Npc attacker, Npc target, LocationType loc)
        {
            float baseDmg = attacker.damage * AggressionMultiplier(loc);
            float dmg = Mathf.Max(1f, baseDmg - target.armor * 0.5f);
            target.hp -= dmg;

            ChangeRelation(target.id, attacker.id, -20);

            string style = StyleOf(attacker.trait);
            Journal.Add(Format(AttackTemplates, attacker, target, loc, dmg, style));

            if (target.hp <= 0f)
            {
                target.alive = false; target.hp = 0f;
                Journal.Add(Format(DeathTemplates, attacker, target, loc, dmg, style));

                foreach (Npc witness in NpcsAt(target.locationIndex, exclude: null))
                {
                    if (witness.id == attacker.id || witness.id == target.id || !witness.alive) continue;
                    ChangeRelation(witness.id, attacker.id, -30);
                }
            }
        }

        private void Flee(Npc actor, LocationType from)
        {
            int newLoc; int safety = 0;
            do { newLoc = UnityEngine.Random.Range(0, AllLocations.Length); safety++; }
            while (newLoc == actor.locationIndex && safety < 8);

            actor.locationIndex = newLoc;
            Journal.Add(Format(FleeTemplates, actor, null, from, 0f, ""));
        }

        private void Trade(Npc actor, Npc partner, LocationType loc)
        {
            ChangeRelation(actor.id, partner.id, +10);
            ChangeRelation(partner.id, actor.id, +10);
            Journal.Add(Format(TradeTemplates, actor, partner, loc, 0f, ""));
        }

        private void Explore(Npc actor, LocationType loc)
        {
            int newLoc; int safety = 0;
            do { newLoc = UnityEngine.Random.Range(0, AllLocations.Length); safety++; }
            while (newLoc == actor.locationIndex && safety < 8);

            actor.locationIndex = newLoc;
            Journal.Add(Format(ExploreTemplates, actor, null, loc, 0f, ""));
        }

        private void Idle(Npc actor, LocationType loc)
        {
            if (UnityEngine.Random.value < 0.20f)
                Journal.Add(Format(IdleTemplates, actor, null, loc, 0f, ""));
        }

        private void ApplyCopresenceBonuses()
        {
            for (int i = 0; i < Npcs.Count; i++)
            {
                Npc a = Npcs[i]; if (!a.alive) continue;
                for (int j = i + 1; j < Npcs.Count; j++)
                {
                    Npc b = Npcs[j];
                    if (!b.alive || a.locationIndex != b.locationIndex) continue;
                    ChangeRelation(a.id, b.id, +2);
                    ChangeRelation(b.id, a.id, +2);
                }
            }
        }

        private bool CheckEnd()
        {
            int alive = 0; Npc lastAlive = null;
            foreach (Npc n in Npcs) if (n.alive) { alive++; lastAlive = n; }
            if (alive <= 1)
            {
                string survivor = lastAlive != null ? lastAlive.name : "no one";
                EndSimulation($"{survivor} stands alone");
                return true;
            }
            return false;
        }

        private void EndSimulation(string reason)
        {
            Finished = true;
            FinalSummary = BuildSummary(reason);
            Journal.Add($"[Day {Day}] {reason}.");
        }

        private string BuildSummary(string reason)
        {
            int alive = 0, dead = 0; Npc strongest = null;
            foreach (Npc n in Npcs)
            {
                if (n.alive) { alive++; if (strongest == null || n.hp > strongest.hp) strongest = n; }
                else dead++;
            }
            string strongLine = strongest != null
                ? $"  Strongest survivor: {strongest.name} ({strongest.cls}, HP {Mathf.RoundToInt(strongest.hp)}/{Mathf.RoundToInt(strongest.maxHp)})"
                : "  No survivors remain.";
            return $"=== Chronicle ends on Day {Day} ===\n" +
                   $"Reason: {reason}.\n" +
                   $"  Survivors: {alive}\n  Fallen:    {dead}\n{strongLine}\n" +
                   $"  Journal entries: {Journal.Count}";
        }

        public int GetRelation(int fromId, int toId) =>
            _relations.TryGetValue((fromId, toId), out int v) ? v : 0;

        private void ChangeRelation(int fromId, int toId, int delta)
        {
            int cur = GetRelation(fromId, toId);
            _relations[(fromId, toId)] = Mathf.Clamp(cur + delta, -100, 100);
            int inverseDelta = Mathf.RoundToInt(delta * 0.7f);
            int curInv = GetRelation(toId, fromId);
            _relations[(toId, fromId)] = Mathf.Clamp(curInv + inverseDelta, -100, 100);
        }

        private List<Npc> NpcsAt(int locIndex, Npc exclude)
        {
            List<Npc> list = new List<Npc>();
            foreach (Npc n in Npcs)
            {
                if (!n.alive) continue;
                if (exclude != null && n.id == exclude.id) continue;
                if (n.locationIndex == locIndex) list.Add(n);
            }
            return list;
        }

        private Npc FindLowestHpEnemy(Npc actor, List<Npc> options)
        {
            Npc best = null; float lowest = float.MaxValue;
            foreach (Npc n in options) if (n.hp < lowest) { lowest = n.hp; best = n; }
            return best;
        }

        private Npc PickAlly(Npc actor, List<Npc> options)
        {
            List<Npc> friendly = new List<Npc>();
            foreach (Npc n in options)
                if (GetRelation(actor.id, n.id) >= 0) friendly.Add(n);
            if (friendly.Count == 0) return null;
            return friendly[UnityEngine.Random.Range(0, friendly.Count)];
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static float AggressionMultiplier(LocationType l)
        {
            switch (l)
            {
                case LocationType.Dungeon: return 1.5f;
                case LocationType.Tavern:  return 0.6f;
                case LocationType.Market:  return 0.4f;
                case LocationType.Forest:  return 1.2f;
                case LocationType.Plain:   return 1.0f;
            }
            return 1f;
        }

        private static float TradeChance(LocationType l)
        {
            float baseChance = 0.25f;
            switch (l)
            {
                case LocationType.Tavern: return baseChance * 1.40f;
                case LocationType.Market: return baseChance * 1.60f;
            }
            return baseChance;
        }

        private static float MovementBonus(LocationType l)
        {
            switch (l)
            {
                case LocationType.Forest: return 0.20f;
                case LocationType.Plain:  return 0.10f;
            }
            return 0f;
        }

        private static string StyleOf(Trait t)
        {
            switch (t)
            {
                case Trait.Aggressive: return "brutally";
                case Trait.Cunning:    return "calculatedly";
                case Trait.Brave:      return "boldly";
                case Trait.Peaceful:   return "reluctantly";
                case Trait.Coward:     return "desperately";
            }
            return "";
        }

        // ---------- Narrative templates (>=3 per type) ----------
        private static readonly string[] AttackTemplates =
        {
            "[Day {d}] {attacker} the {cls} struck {target} {style} in the {loc} for {dmg} damage.",
            "[Day {d}] In the {loc}, {attacker} unleashed a {style} attack on {target}, dealing {dmg}.",
            "[Day {d}] {target} reeled as {attacker} hit {style} for {dmg} damage.",
            "[Day {d}] Blades clashed in the {loc} - {attacker} carved {dmg} from {target}."
        };
        private static readonly string[] FleeTemplates =
        {
            "[Day {d}] {attacker}, terrified, fled the {loc}.",
            "[Day {d}] Wounded and afraid, {attacker} escaped from {loc}.",
            "[Day {d}] {attacker} ran from the {loc} to save their skin."
        };
        private static readonly string[] TradeTemplates =
        {
            "[Day {d}] {attacker} shared supplies with {target} in the {loc}.",
            "[Day {d}] A gift passed between {attacker} and {target} at the {loc}.",
            "[Day {d}] {attacker} handed {target} something useful - a moment of kindness in the {loc}."
        };
        private static readonly string[] ExploreTemplates =
        {
            "[Day {d}] {attacker} wandered off into the {loc}.",
            "[Day {d}] Restless, {attacker} sought a new place beyond the {loc}.",
            "[Day {d}] {attacker} moved on, leaving the {loc} behind."
        };
        private static readonly string[] IdleTemplates =
        {
            "[Day {d}] {attacker} rested in the {loc}.",
            "[Day {d}] Nothing notable from {attacker} today.",
            "[Day {d}] {attacker} sat quietly in the {loc}."
        };
        private static readonly string[] DeathTemplates =
        {
            "[Day {d}] {target} fell at the hands of {attacker} in the {loc}.",
            "[Day {d}] The {loc} claimed {target}'s life - {attacker} dealt the killing blow.",
            "[Day {d}] {target} drew their last breath in the {loc}."
        };

        private string Format(string[] pool, Npc attacker, Npc target, LocationType loc, float dmg, string style)
        {
            string template = pool[UnityEngine.Random.Range(0, pool.Length)];
            return template
                .Replace("{d}", Day.ToString())
                .Replace("{attacker}", attacker != null ? attacker.name : "Someone")
                .Replace("{target}", target != null ? target.name : "no one")
                .Replace("{cls}", attacker != null ? attacker.cls.ToString() : "")
                .Replace("{loc}", loc.ToString())
                .Replace("{dmg}", Mathf.RoundToInt(dmg).ToString())
                .Replace("{style}", style);
        }
    }
}
```

### Assets/lab6/Scripts/Lab6SaveSystem.cs

```csharp
using System.IO;
using UnityEngine;

namespace Lab6
{
    public static class Lab6SaveSystem
    {
        private const string FileName = "lab6_save.json";
        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(Path)) return new SaveData();
                string json = File.ReadAllText(Path);
                if (string.IsNullOrWhiteSpace(json)) return new SaveData();
                return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Lab6] Failed to load save: {e.Message}");
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(Path, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Lab6] Failed to save: {e.Message}");
            }
        }

        public static string GetSavePath() => Path;
    }
}
```

### Assets/lab6/Scripts/Lab6Ui.cs (UI helper)

Helper care construieste Panel, Label, Button, InputField, ScrollView din cod.
Foloseste Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") pentru font-ul
default. ScrollView e construit complet manual cu Viewport (Mask), Content
(VerticalLayoutGroup + ContentSizeFitter), si scrollbar vertical.

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lab6
{
    public static class Lab6Ui
    {
        private static Font _defaultFont;
        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont != null) return _defaultFont;
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _defaultFont;
            }
        }

        public static GameObject Panel(Transform parent, string name, Color bg)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        public static Text Label(Transform parent, string text, int size = 14,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text; t.font = DefaultFont; t.fontSize = size;
            t.alignment = anchor; t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontStyle = style; t.supportRichText = true;
            return t;
        }

        public static Button Btn(Transform parent, string label, Action onClick, Color? color = null)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color ?? new Color(0.25f, 0.28f, 0.40f, 1f);
            Button b = go.GetComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(0.35f, 0.40f, 0.55f);
            cb.pressedColor = new Color(0.20f, 0.22f, 0.32f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());

            Text t = Label(go.transform, label, 14, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform rtt = t.GetComponent<RectTransform>();
            rtt.anchorMin = Vector2.zero; rtt.anchorMax = Vector2.one;
            rtt.offsetMin = Vector2.zero; rtt.offsetMax = Vector2.zero;
            return b;
        }

        // InputField, ScrollView, Anchor, Stretch, SetSize - vezi sursa completa
        // pentru implementarea utilitarelor de layout.

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max,
                                  Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }

        public static void Stretch(GameObject go, float pad = 0f)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
```

### Assets/lab6/Scripts/Lab6Main.cs (bootstrap + orchestrator UI)

Construieste tot UI-ul la Start(). Functii principale:

- `Start()` -> EnsureEventSystem(), BuildCanvas(), Load(), BuildHeader(), BuildPanels(), ShowTab(Npc)
- `BuildHeader()` - tabs Npc/Item/World/Gallery + titlu
- `BuildNpcPanel()` -> buton "Generate NPC" + display cu portret colorat (initiala clasei) + stat block
- `BuildItemPanel()` -> buton "Generate Item" + card centrat cu nume colorat raritate + stele + stats + abilitati
- `BuildWorldPanel()` -> butoane "New Simulation" / "Advance Day" + harta GridLayout + scroll-view jurnal
- `BuildGalleryPanel()` -> tabs NPCs/Items + ScrollView cu randuri + Rename + Clear Save
- `Update()` - animatie Legendary (Mathf.PingPong + Sin pe scale)

Cod cheie:

```csharp
public class Lab6Main : MonoBehaviour
{
    public enum Tab { Npc, Item, World, Gallery }

    private SaveData _save;
    private Lab6World _world;
    // ... fields

    private void Start()
    {
        EnsureEventSystem();
        BuildCanvas();
        _save = Lab6SaveSystem.Load();
        BuildHeader();
        BuildPanels();
        ShowTab(Tab.Npc);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();  // proiectul foloseste New Input System
    }

    private void GenerateNpcAndSave()
    {
        Npc n = Lab6NpcGenerator.Generate(_save.nextNpcId++);
        _save.npcs.Add(n);
        Lab6SaveSystem.Save(_save);
        _currentNpc = n;
        RenderNpcDisplay();
    }

    private void GenerateItemAndSave()
    {
        Item it = Lab6ItemGenerator.Generate(_save.nextItemId++);
        _save.items.Add(it);
        Lab6SaveSystem.Save(_save);
        _currentItem = it;
        RenderItemDisplay();
    }

    private void StartWorld(int npcCount)
    {
        _world = new Lab6World();
        _world.StartNew(npcCount, System.Environment.TickCount);
        RefreshWorldUi();
    }

    private void AdvanceWorldDay()
    {
        if (_world == null) StartWorld(5);
        if (_world.Finished) return;
        _world.AdvanceDay();
        RefreshWorldUi();
    }

    private void Update()
    {
        // Legendary animation (Bonus 2)
        if (_itemNameText != null && _currentItem != null && _currentItem.rarity == Rarity.Legendary)
        {
            float t = Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
            Color c = Color.Lerp(Lab6Colors.Legendary, new Color(1f, 0.95f, 0.55f), t);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            string stars = Lab6ItemGenerator.Stars(Rarity.Legendary);
            _itemNameText.text = $"<color=#{hex}><b>{_currentItem.name}</b></color>  <color=#{hex}>{stars}</color>";
            float scale = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3.2f);
            _itemNameText.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
```

Restul Lab6Main.cs construieste UI-ul detaliat (peste 600 linii). Pattern-ul e
acelasi: `Lab6Ui.Panel`/`Label`/`Btn`/`ScrollView` cu `Lab6Ui.Anchor` pentru
pozitionare. Render-ul tab-urilor: cleared + rebuilt cu noile date dupa fiecare
generare sau Advance Day.

---

## Rulare in Unity

1. Deschide Assets/lab6/lab6.unity (sau scena este deja deschisa).
2. Apasa Play.
3. Tab-urile sus: NPC / Item / World / Gallery.
4. La generare/rename, datele se salveaza automat in
   Application.persistentDataPath/lab6_save.json (calea exacta e afisata in
   tab-ul Gallery).
