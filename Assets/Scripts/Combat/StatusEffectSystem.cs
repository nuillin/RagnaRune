using System;
using System.Collections.Generic;
using UnityEngine;

namespace RagnaRune.Combat
{
    public enum StatusEffectType
    {
        // ── Debuffs ───────────────────────────────────────────────
        Poison,     // Deals % max HP damage per tick; cannot reduce below 1 HP
        Stun,       // Cannot act; ends after duration
        Freeze,     // Cannot act; takes extra Fire/Wind damage; thaws on fire hit
        Sleep,      // Cannot act; next hit deals ×2 damage and wakes
        Silence,    // Cannot cast magic
        Blind,      // HIT -50%
        Slow,       // ASPD -50%, FLEE -50%
        Curse,      // STR/DEX/AGI ÷2; LUK=0; move speed ×0.1
        Bleeding,   // Flat HP loss per tick (persistent; cured by First Aid)

        // ── Buffs ─────────────────────────────────────────────────
        Endure,     // Ignores knockback; MDEF +DEF amount temporarily
        Bless,      // STR/INT/DEX +10
        Agi,        // AGI/FLEE +20, ASPD +0.3
    }

    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        public float Duration;          // seconds remaining
        public float TickInterval;      // seconds between damage/heal ticks (0 = no tick)
        public float TickTimer;
        public float Power;             // magnitude (e.g. % for Poison, flat for Bleed)
        public bool  IsDebuff => Type <= StatusEffectType.Bleeding;
    }

    /// <summary>
    /// Manages active status effects on one character (player or enemy).
    /// Attach alongside CombatManager / EnemyController.
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour
    {
        public event Action<StatusEffect> OnEffectApplied;
        public event Action<StatusEffectType> OnEffectExpired;

        private readonly Dictionary<StatusEffectType, StatusEffect> _active = new();

        // cached stat penalties applied each RebuildStats call
        public int   HitPenalty   { get; private set; }
        public float AspdPenalty  { get; private set; }
        public float FleePenalty  { get; private set; }
        public int   StrPenalty   { get; private set; }
        public int   DexPenalty   { get; private set; }
        public int   AgiPenalty   { get; private set; }
        public int   BlessBonus   { get; private set; }
        public float AgiAspd      { get; private set; }
        public int   AgiFleeBonus { get; private set; }

        // ── Apply ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Apply a status effect. If one of the same type already exists,
        /// it is refreshed (duration resets, power takes the higher value).
        /// </summary>
        public void Apply(StatusEffectType type, float duration, float power = 0f, float tickInterval = 0f)
        {
            if (_active.TryGetValue(type, out var existing))
            {
                existing.Duration     = Mathf.Max(existing.Duration, duration);
                existing.Power        = Mathf.Max(existing.Power, power);
                return;
            }

            var effect = new StatusEffect
            {
                Type         = type,
                Duration     = duration,
                Power        = power,
                TickInterval = tickInterval,
                TickTimer    = tickInterval,
            };
            _active[type] = effect;
            RecalcPenalties();
            OnEffectApplied?.Invoke(effect);
        }

        public void Remove(StatusEffectType type)
        {
            if (_active.Remove(type))
            {
                RecalcPenalties();
                OnEffectExpired?.Invoke(type);
            }
        }

        public bool Has(StatusEffectType type) => _active.ContainsKey(type);
        public IReadOnlyCollection<StatusEffect> All => _active.Values;

        public void ClearAll()
        {
            var types = new List<StatusEffectType>(_active.Keys);
            _active.Clear();
            RecalcPenalties();
            foreach (var t in types) OnEffectExpired?.Invoke(t);
        }

        // ── Tick ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_active.Count == 0) return;

            var toRemove = new List<StatusEffectType>();

            foreach (var kv in _active)
            {
                var e = kv.Value;
                e.Duration -= Time.deltaTime;

                if (e.TickInterval > 0f)
                {
                    e.TickTimer -= Time.deltaTime;
                    if (e.TickTimer <= 0f)
                    {
                        e.TickTimer = e.TickInterval;
                        ProcessTick(e);
                    }
                }

                if (e.Duration <= 0f)
                    toRemove.Add(kv.Key);
            }

            foreach (var t in toRemove) Remove(t);
        }

        private void ProcessTick(StatusEffect e)
        {
            var stats = GetComponent<Core.CharacterStats>()
                     ?? GetComponent<CombatManager>()?.PlayerStats
                     ?? GetComponent<Enemy.EnemyController>()?.Stats;

            if (stats == null) return;

            switch (e.Type)
            {
                case StatusEffectType.Poison:
                    // RO: poison does 1/12 max HP per tick, cannot kill
                    int poisonDmg = Mathf.Max(1, Mathf.RoundToInt(stats.MaxHP * (e.Power / 100f)));
                    int newHP     = Mathf.Max(1, stats.CurrentHP - poisonDmg);
                    stats.CurrentHP = newHP;
                    break;

                case StatusEffectType.Bleeding:
                    int bleedDmg = Mathf.Max(1, Mathf.RoundToInt(e.Power));
                    stats.CurrentHP = Mathf.Max(1, stats.CurrentHP - bleedDmg);
                    break;
            }
        }

        // ── Stat penalties ────────────────────────────────────────────────────

        private void RecalcPenalties()
        {
            HitPenalty   = 0;
            AspdPenalty  = 0f;
            FleePenalty  = 0f;
            StrPenalty   = 0;
            DexPenalty   = 0;
            AgiPenalty   = 0;
            BlessBonus   = 0;
            AgiAspd      = 0f;
            AgiFleeBonus = 0;

            foreach (var kv in _active)
            {
                switch (kv.Key)
                {
                    case StatusEffectType.Blind:
                        HitPenalty += 50;
                        break;
                    case StatusEffectType.Slow:
                        AspdPenalty += 0.5f;
                        FleePenalty += 0.5f;
                        break;
                    case StatusEffectType.Curse:
                        StrPenalty = 50;
                        DexPenalty = 50;
                        AgiPenalty = 50;
                        break;
                    case StatusEffectType.Bless:
                        BlessBonus = 10;
                        break;
                    case StatusEffectType.Agi:
                        AgiAspd      = 0.3f;
                        AgiFleeBonus = 20;
                        break;
                }
            }
        }

        // ── Factory helpers ───────────────────────────────────────────────────
        // Call these from abilities or enemy AI.

        public void ApplyPoison(float duration = 10f, float percentPerTick = 1f)
            => Apply(StatusEffectType.Poison, duration, percentPerTick, tickInterval: 1f);

        public void ApplyStun(float duration = 2f)
            => Apply(StatusEffectType.Stun, duration);

        public void ApplyFreeze(float duration = 3f)
            => Apply(StatusEffectType.Freeze, duration);

        public void ApplySleep(float duration = 10f)
            => Apply(StatusEffectType.Sleep, duration);

        public void ApplySilence(float duration = 8f)
            => Apply(StatusEffectType.Silence, duration);

        public void ApplyBlind(float duration = 10f)
            => Apply(StatusEffectType.Blind, duration);

        public void ApplySlow(float duration = 8f)
            => Apply(StatusEffectType.Slow, duration);

        public void ApplyCurse(float duration = 15f)
            => Apply(StatusEffectType.Curse, duration);

        public void ApplyBleeding(float duration = 15f, float flatDamagePerTick = 5f)
            => Apply(StatusEffectType.Bleeding, duration, flatDamagePerTick, tickInterval: 2f);
    }
}
