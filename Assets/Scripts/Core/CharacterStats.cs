using System;
using UnityEngine;

namespace RagnaRune.Core
{
    /// <summary>
    /// Shared stat block used by both player and enemies.
    /// Mirrors the Ragnarok Online formula set.
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        // ── Base Stats ────────────────────────────────────────────────────────
        [Header("Base Stats")]
        public int STR = 1;   // Physical damage
        public int AGI = 1;   // ASPD, Flee
        public int VIT = 1;   // Max HP, DEF
        public int INT = 1;   // Max SP, MATK, MDEF
        public int DEX = 1;   // HIT, ranged ATK, cast time
        public int LUK = 1;   // Crit, perfect dodge, status resist

        [Header("Level")]
        public int BaseLevel = 1;

        // ── Elemental ─────────────────────────────────────────────────────────
        [Header("Element")]
        public Element BodyElement = Element.Neutral;
        public Element WeaponElement = Element.Neutral;

        // ── Size (affects card/skill bonuses) ─────────────────────────────────
        // 0 = Small, 1 = Medium, 2 = Large
        [Range(0, 2)] public int Size = 1;

        // ── Derived Stats (recalculated via RecalcDerived) ────────────────────
        [HideInInspector] public int MaxHP;
        [HideInInspector] public int CurrentHP;
        [HideInInspector] public int MaxSP;
        [HideInInspector] public int CurrentSP;

        [HideInInspector] public int ATK;    // Physical attack
        [HideInInspector] public int MATK;   // Magic attack
        [HideInInspector] public int DEF;    // Physical defense
        [HideInInspector] public int MDEF;   // Magic defense
        [HideInInspector] public int HIT;    // Hit rate
        [HideInInspector] public int FLEE;   // Flee rate
        [HideInInspector] public int CRIT;   // Critical rate (%)
        [HideInInspector] public float ASPD; // Attacks per second

        // ── Bonus Stats from equipment/cards ─────────────────────────────────
        [HideInInspector] public int BonusATK;
        [HideInInspector] public int BonusDEF;
        [HideInInspector] public int BonusHIT;
        [HideInInspector] public int BonusFLEE;
        [HideInInspector] public int BonusMaxHP;
        [HideInInspector] public float BonusASPD;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Recalculate all derived stats from base stats + bonuses.</summary>
        public void RecalcDerived()
        {
            // ── Max HP (RO approximation) ─────────────────────────────────────
            MaxHP = 35 + (VIT * 5) + (VIT * VIT / 10) + (BaseLevel * 3) + BonusMaxHP;
            MaxHP = Mathf.Max(MaxHP, 1);

            // ── Max SP ────────────────────────────────────────────────────────
            MaxSP = 10 + (INT * 3) + (BaseLevel * 2);
            MaxSP = Mathf.Max(MaxSP, 1);

            // ── Physical ATK ──────────────────────────────────────────────────
            ATK = STR + (STR * STR / 10) + (DEX / 5) + (LUK / 5) + BonusATK;

            // ── Magic ATK ─────────────────────────────────────────────────────
            MATK = INT + (INT * INT / 7);

            // ── Defenses ─────────────────────────────────────────────────────
            DEF  = (VIT / 2) + BonusDEF;
            MDEF = INT / 5;

            // ── HIT / FLEE ────────────────────────────────────────────────────
            HIT  = BaseLevel + DEX + BonusHIT;
            FLEE = BaseLevel + AGI + BonusFLEE;

            // ── Critical ──────────────────────────────────────────────────────
            CRIT = Mathf.Max(1, (LUK / 3));

            // ── ASPD (clamped 0.5–3 attacks/sec) ─────────────────────────────
            ASPD = 1.0f + (AGI * 0.01f) + (DEX * 0.005f) + BonusASPD;
            ASPD = Mathf.Clamp(ASPD, 0.5f, 3.0f);
        }

        public bool IsAlive() => CurrentHP > 0;

        public void FullRestore()
        {
            CurrentHP = MaxHP;
            CurrentSP = MaxSP;
        }

        public void TakeDamage(int amount)
        {
            CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, MaxHP);
        }

        public bool ConsumeSP(int cost)
        {
            if (CurrentSP < cost) return false;
            CurrentSP -= cost;
            return true;
        }
    }
}
