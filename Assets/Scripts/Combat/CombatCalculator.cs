using UnityEngine;
using RagnaRune.Core;
using RagnaRune.Cards;

namespace RagnaRune.Combat
{
    public enum HitResult { Miss, Hit, Critical, PerfectDodge }

    public struct DamageResult
    {
        public HitResult Hit;
        public int RawDamage;
        public int FinalDamage;
        public bool IsCritical;
        public Element AttackElement;
        public float ElementMultiplier;
    }

    /// <summary>
    /// Pure-static combat math. No MonoBehaviour, no state.
    /// Mirror RO's published formulas as closely as sensible.
    /// </summary>
    public static class CombatCalculator
    {
        // ── Hit / Miss ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns whether an attack lands and if it's a critical.
        /// RO formula: chance to hit = (attacker HIT - defender FLEE) + 80%
        /// </summary>
        public static HitResult RollHit(CharacterStats attacker, CharacterStats defender)
        {
            // Critical check first (bypasses FLEE entirely)
            int critChance = Mathf.Clamp(attacker.CRIT, 1, 100);
            if (Random.Range(0, 100) < critChance)
                return HitResult.Critical;

            // Perfect dodge (LUK-based for defender)
            int perfectDodge = Mathf.Max(1, defender.LUK / 10);
            if (Random.Range(0, 100) < perfectDodge)
                return HitResult.PerfectDodge;

            // Standard hit roll
            int hitChance = Mathf.Clamp((attacker.HIT - defender.FLEE) + 80, 5, 95);
            return Random.Range(0, 100) < hitChance ? HitResult.Hit : HitResult.Miss;
        }

        // ── Physical Damage ───────────────────────────────────────────────────

        /// <summary>
        /// Full melee physical damage calculation.
        /// </summary>
        public static DamageResult PhysicalDamage(
            CharacterStats attacker,
            CharacterStats defender,
            CardSystem attackerCards = null)
        {
            var result = new DamageResult();

            result.Hit = RollHit(attacker, defender);
            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge)
                return result;

            result.IsCritical    = result.Hit == HitResult.Critical;
            result.AttackElement = attacker.WeaponElement;

            // ── Base damage ───────────────────────────────────────────────────
            // RO: damage = (ATK - DEF) * element_mult, randomized ±10%
            int baseATK = attacker.ATK;
            int cardSizeBonus = attackerCards != null
                ? attackerCards.GetBonusDamageVsSize(defender.Size) : 0;
            baseATK += cardSizeBonus;

            // Ignore DEF from cards
            int ignorePct = attackerCards != null ? attackerCards.GetIgnoreDefPercent() : 0;
            int effectiveDEF = defender.DEF * (100 - ignorePct) / 100;

            int rawDmg = Mathf.Max(1, baseATK - effectiveDEF);

            // Random variance ±10%
            float variance = Random.Range(0.9f, 1.1f);
            rawDmg = Mathf.RoundToInt(rawDmg * variance);

            // ── Element multiplier ────────────────────────────────────────────
            result.ElementMultiplier = ElementChart.GetMultiplier(
                attacker.WeaponElement, defender.BodyElement);
            int dmg = Mathf.RoundToInt(rawDmg * result.ElementMultiplier);

            // ── Critical bonus: ignores DEF, +40% damage ─────────────────────
            if (result.IsCritical)
            {
                dmg = Mathf.RoundToInt((baseATK * 1.4f) * result.ElementMultiplier);
            }

            result.RawDamage   = rawDmg;
            result.FinalDamage = Mathf.Max(1, dmg);
            return result;
        }

        // ── Magic Damage ──────────────────────────────────────────────────────

        public static DamageResult MagicDamage(
            CharacterStats attacker,
            CharacterStats defender,
            Element spellElement = Element.Neutral)
        {
            var result = new DamageResult();
            result.AttackElement = spellElement;

            // Magic always hits (cast completes), but can be resisted
            result.Hit = HitResult.Hit;

            int baseMATK = attacker.MATK;
            int rawDmg   = Mathf.Max(1, baseMATK - defender.MDEF);

            float variance = Random.Range(0.85f, 1.15f);
            rawDmg = Mathf.RoundToInt(rawDmg * variance);

            result.ElementMultiplier = ElementChart.GetMultiplier(spellElement, defender.BodyElement);
            result.RawDamage   = rawDmg;
            result.FinalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDmg * result.ElementMultiplier));
            return result;
        }

        // ── XP Rewards ───────────────────────────────────────────────────────

        /// <summary>
        /// Calculate base XP for defeating an enemy (scaled by level difference).
        /// </summary>
        public static long CalculateKillXP(CharacterStats enemy, int playerLevel)
        {
            float levelScale = Mathf.Clamp(1f + (enemy.BaseLevel - playerLevel) * 0.1f, 0.5f, 2f);
            long baseXP = (long)(enemy.BaseLevel * 10 * levelScale);
            return baseXP;
        }
    }
}
