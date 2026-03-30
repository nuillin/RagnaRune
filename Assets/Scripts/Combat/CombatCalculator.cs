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
        public bool IsRanged;
    }

    /// <summary>
    /// Pure-static combat math. No MonoBehaviour, no state.
    /// Mirrors RO's published formulas as closely as sensible.
    /// </summary>
    public static class CombatCalculator
    {
        // ── Hit / Miss ────────────────────────────────────────────────────────

        /// <summary>
        /// RO formula: base hit chance = (attacker HIT - defender FLEE) + 80%
        /// Criticals bypass FLEE entirely.
        /// </summary>
        public static HitResult RollHit(CharacterStats attacker, CharacterStats defender)
        {
            int critChance = Mathf.Clamp(attacker.CRIT, 1, 100);
            if (Random.Range(0, 100) < critChance)
                return HitResult.Critical;

            int perfectDodge = Mathf.Max(1, defender.LUK / 10);
            if (Random.Range(0, 100) < perfectDodge)
                return HitResult.PerfectDodge;

            int hitChance = Mathf.Clamp((attacker.HIT - defender.FLEE) + 80, 5, 95);
            return Random.Range(0, 100) < hitChance ? HitResult.Hit : HitResult.Miss;
        }

        // ── Physical Melee ────────────────────────────────────────────────────

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

            int baseATK = attacker.ATK;
            baseATK += attackerCards?.GetBonusDamageVsSize(defender.Size) ?? 0;

            int ignorePct    = attackerCards?.GetIgnoreDefPercent() ?? 0;
            int effectiveDEF = defender.DEF * (100 - ignorePct) / 100;
            int rawDmg       = Mathf.Max(1, baseATK - effectiveDEF);
            rawDmg           = Mathf.RoundToInt(rawDmg * Random.Range(0.9f, 1.1f));

            result.ElementMultiplier = ElementChart.GetMultiplier(attacker.WeaponElement, defender.BodyElement);
            int dmg = Mathf.RoundToInt(rawDmg * result.ElementMultiplier);

            if (result.IsCritical)
                dmg = Mathf.RoundToInt(baseATK * 1.4f * result.ElementMultiplier);

            result.RawDamage   = rawDmg;
            result.FinalDamage = Mathf.Max(1, dmg);
            return result;
        }

        // ── Ranged Physical ───────────────────────────────────────────────────

        /// <summary>
        /// RO-style ranged attack. Scales with DEX instead of STR; no ignore-DEF from melee cards.
        /// Ranged cards (BonusRanged) are applied here.
        /// </summary>
        public static DamageResult RangedDamage(
            CharacterStats attacker,
            CharacterStats defender,
            CardSystem attackerCards = null)
        {
            var result = new DamageResult();
            result.IsRanged = true;
            result.Hit      = RollHit(attacker, defender);
            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge)
                return result;

            result.IsCritical    = result.Hit == HitResult.Critical;
            result.AttackElement = attacker.WeaponElement;

            // Ranged ATK = DEX-dominant formula (RO approximation)
            int rangedATK = attacker.DEX + (attacker.DEX * attacker.DEX / 10)
                          + (attacker.STR / 5) + (attacker.LUK / 10);
            rangedATK += attackerCards?.GetBonusRangedATK() ?? 0;
            rangedATK += attackerCards?.GetBonusDamageVsSize(defender.Size) ?? 0;

            int effectiveDEF = defender.DEF;   // ranged doesn't ignore DEF unless using special card
            int rawDmg       = Mathf.Max(1, rangedATK - effectiveDEF);
            rawDmg           = Mathf.RoundToInt(rawDmg * Random.Range(0.85f, 1.15f));

            result.ElementMultiplier = ElementChart.GetMultiplier(attacker.WeaponElement, defender.BodyElement);
            int dmg = Mathf.RoundToInt(rawDmg * result.ElementMultiplier);

            if (result.IsCritical)
                dmg = Mathf.RoundToInt(rangedATK * 1.4f * result.ElementMultiplier);

            result.RawDamage   = rawDmg;
            result.FinalDamage = Mathf.Max(1, dmg);
            return result;
        }

        // ── Magic ─────────────────────────────────────────────────────────────

        public static DamageResult MagicDamage(
            CharacterStats attacker,
            CharacterStats defender,
            Element spellElement = Element.Neutral)
        {
            var result = new DamageResult();
            result.AttackElement = spellElement;
            result.Hit           = HitResult.Hit;

            int rawDmg = Mathf.Max(1, attacker.MATK - defender.MDEF);
            rawDmg     = Mathf.RoundToInt(rawDmg * Random.Range(0.85f, 1.15f));

            result.ElementMultiplier = ElementChart.GetMultiplier(spellElement, defender.BodyElement);
            result.RawDamage         = rawDmg;
            result.FinalDamage       = Mathf.Max(1, Mathf.RoundToInt(rawDmg * result.ElementMultiplier));
            return result;
        }

        // ── XP Rewards ────────────────────────────────────────────────────────

        public static long CalculateKillXP(CharacterStats enemy, int playerLevel)
        {
            float levelScale = Mathf.Clamp(1f + (enemy.BaseLevel - playerLevel) * 0.1f, 0.5f, 2f);
            return (long)(enemy.BaseLevel * 10 * levelScale);
        }
    }
}
