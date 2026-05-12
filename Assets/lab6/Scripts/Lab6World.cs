using System;
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
            Npcs.Clear();
            Journal.Clear();
            _relations.Clear();
            Day = 0;
            Finished = false;
            FinalSummary = null;

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
            {
                EndSimulation("the 10-day count was reached");
            }
        }

        private void ExecuteTurn(Npc actor)
        {
            LocationType loc = AllLocations[actor.locationIndex];
            List<Npc> sameLocation = NpcsAt(actor.locationIndex, exclude: actor);

            float hpPercent = actor.maxHp <= 0f ? 0f : actor.hp / actor.maxHp;

            // Priority 1: Coward survival
            if (actor.trait == Trait.Coward && hpPercent < 0.30f)
            {
                Flee(actor, loc);
                return;
            }

            // Priority 2: Attack
            bool wantsToAttack =
                (actor.trait == Trait.Aggressive || actor.trait == Trait.Cunning) && sameLocation.Count > 0;

            if (wantsToAttack)
            {
                Npc target = (actor.trait == Trait.Cunning)
                    ? FindLowestHpEnemy(actor, sameLocation)
                    : sameLocation[UnityEngine.Random.Range(0, sameLocation.Count)];

                Attack(actor, target, loc);
                return;
            }

            // Priority 3: Trade (allies = same location, relation >= 0)
            if (sameLocation.Count > 0 && UnityEngine.Random.value < TradeChance(loc))
            {
                Npc partner = PickAlly(actor, sameLocation);
                if (partner != null)
                {
                    Trade(actor, partner, loc);
                    return;
                }
            }

            // Priority 4: Explore
            float exploreChance = 0.20f + MovementBonus(loc);
            if (UnityEngine.Random.value < exploreChance)
            {
                Explore(actor, loc);
                return;
            }

            // Priority 5: Idle
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
                target.alive = false;
                target.hp = 0f;
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
            int newLoc;
            int safety = 0;
            do
            {
                newLoc = UnityEngine.Random.Range(0, AllLocations.Length);
                safety++;
            } while (newLoc == actor.locationIndex && safety < 8);

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
            int newLoc;
            int safety = 0;
            do
            {
                newLoc = UnityEngine.Random.Range(0, AllLocations.Length);
                safety++;
            } while (newLoc == actor.locationIndex && safety < 8);

            actor.locationIndex = newLoc;
            Journal.Add(Format(ExploreTemplates, actor, null, loc, 0f, ""));
        }

        private void Idle(Npc actor, LocationType loc)
        {
            if (UnityEngine.Random.value < 0.20f)
            {
                Journal.Add(Format(IdleTemplates, actor, null, loc, 0f, ""));
            }
        }

        private void ApplyCopresenceBonuses()
        {
            for (int i = 0; i < Npcs.Count; i++)
            {
                Npc a = Npcs[i];
                if (!a.alive) continue;
                for (int j = i + 1; j < Npcs.Count; j++)
                {
                    Npc b = Npcs[j];
                    if (!b.alive) continue;
                    if (a.locationIndex != b.locationIndex) continue;

                    ChangeRelation(a.id, b.id, +2);
                    ChangeRelation(b.id, a.id, +2);
                }
            }
        }

        private bool CheckEnd()
        {
            int alive = 0;
            Npc lastAlive = null;
            foreach (Npc n in Npcs)
            {
                if (n.alive) { alive++; lastAlive = n; }
            }

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
            int alive = 0, dead = 0;
            Npc strongest = null;
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
                   $"  Survivors: {alive}\n" +
                   $"  Fallen:    {dead}\n" +
                   $"{strongLine}\n" +
                   $"  Journal entries: {Journal.Count}";
        }

        public int GetRelation(int fromId, int toId)
        {
            return _relations.TryGetValue((fromId, toId), out int v) ? v : 0;
        }

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
            Npc best = null;
            float lowest = float.MaxValue;
            foreach (Npc n in options)
            {
                if (n.hp < lowest)
                {
                    lowest = n.hp;
                    best = n;
                }
            }
            return best;
        }

        private Npc PickAlly(Npc actor, List<Npc> options)
        {
            List<Npc> friendly = new List<Npc>();
            foreach (Npc n in options)
            {
                if (GetRelation(actor.id, n.id) >= 0) friendly.Add(n);
            }
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

        // ---------- Narrative templates (>= 3 per type) ----------
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
