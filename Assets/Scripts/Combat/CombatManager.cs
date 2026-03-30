using System;
using System.Collections;
using UnityEngine;
using RagnaRune.Core;
using RagnaRune.Cards;
using RagnaRune.Skills;

namespace RagnaRune.Combat
{
    public enum CombatState { Idle, InCombat, PlayerDead, EnemyDead }

    /// <summary>
    /// Drives the real-time auto-attack combat loop (RO-style).
    /// Supports melee auto-attack, magic spells, and ranged attacks.
    /// Attach to the Player GameObject.
    /// </summary>
    [RequireComponent(typeof(SkillSystem))]
    [RequireComponent(typeof(StatusEffectSystem))]
    public class CombatManager : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<DamageResult> OnPlayerDamageDealt;
        public event Action<DamageResult> OnPlayerDamageTaken;
        public event Action<Enemy.EnemyController> OnEnemyDefeated;
        public event Action OnPlayerDefeated;
        public event Action<CombatState> OnStateChanged;

        // ── References ────────────────────────────────────────────────────────
        [Header("Player Stats")]
        public CharacterStats PlayerStats;
        public CardSystem CardSystem;
        private SkillSystem _skillSystem;
        private StatusEffectSystem _statusEffects;

        [Header("Current Target")]
        public Enemy.EnemyController CurrentTarget;

        [Header("Ranged")]
        [Tooltip("Maximum range for ranged attacks (units).")]
        public float RangedRange = 8f;
        [Tooltip("SP cost per ranged shot.")]
        public int RangedSPCost = 5;

        // ── Internal State ────────────────────────────────────────────────────
        private CombatState _state = CombatState.Idle;
        private float _attackTimer = 0f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _skillSystem   = GetComponent<SkillSystem>();
            _statusEffects = GetComponent<StatusEffectSystem>();
        }

        private void Start()
        {
            if (PlayerStats == null) PlayerStats = new CharacterStats();
            RebuildStats();
            PlayerStats.FullRestore();
        }

        // ── Stat rebuild ──────────────────────────────────────────────────────

        public void RebuildStats()
        {
            if (PlayerStats == null) PlayerStats = new CharacterStats();
            PlayerStats.BonusATK   = 0;
            PlayerStats.BonusDEF   = 0;
            PlayerStats.BonusMaxHP = 0;
            PlayerStats.BonusASPD  = 0f;
            PlayerStats.BonusHIT   = 0;
            PlayerStats.BonusFLEE  = 0;

            _skillSystem?.ApplySkillBonusesToStats(PlayerStats);
            CardSystem?.ApplyCardBonuses(PlayerStats);
            PlayerStats.RecalcDerived();
        }

        // ── Targeting ─────────────────────────────────────────────────────────

        public void SetTarget(Enemy.EnemyController enemy)
        {
            if (enemy == null || !enemy.IsAlive()) { ClearTarget(); return; }
            CurrentTarget = enemy;
            SetState(CombatState.InCombat);
            _attackTimer = 0f;
        }

        public void ClearTarget()
        {
            CurrentTarget = null;
            if (_state == CombatState.InCombat) SetState(CombatState.Idle);
        }

        // ── Update Loop ───────────────────────────────────────────────────────

        private void Update()
        {
            if (_state != CombatState.InCombat) return;
            if (CurrentTarget == null || !CurrentTarget.IsAlive()) { ClearTarget(); return; }

            // Stunned / frozen players can't attack
            if (_statusEffects != null &&
                (_statusEffects.Has(StatusEffectType.Stun) ||
                 _statusEffects.Has(StatusEffectType.Freeze) ||
                 _statusEffects.Has(StatusEffectType.Sleep)))
                return;

            _attackTimer += Time.deltaTime;
            float attackInterval = 1f / PlayerStats.ASPD;

            if (_attackTimer >= attackInterval)
            {
                _attackTimer -= attackInterval;
                ExecuteAutoAttack();
            }
        }

        // ── Auto Attack ───────────────────────────────────────────────────────

        private void ExecuteAutoAttack()
        {
            var result = CombatCalculator.PhysicalDamage(PlayerStats, CurrentTarget.Stats, CardSystem);
            OnPlayerDamageDealt?.Invoke(result);

            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge) return;

            CurrentTarget.TakeDamage(result.FinalDamage);
            _skillSystem?.OnMeleeHit(result.FinalDamage);
            RebuildStats();

            if (!CurrentTarget.IsAlive()) HandleEnemyDeath(CurrentTarget);
        }

        // ── Magic Spells ──────────────────────────────────────────────────────

        public void CastSpell(Element spellElement, int spCost = 10)
        {
            if (CurrentTarget == null || !CurrentTarget.IsAlive()) return;
            if (_statusEffects != null && _statusEffects.Has(StatusEffectType.Silence))
            {
                Debug.Log("[Combat] Silenced — cannot cast spells.");
                return;
            }
            if (!PlayerStats.ConsumeSP(spCost)) { Debug.Log("[Combat] Not enough SP!"); return; }

            var result = CombatCalculator.MagicDamage(PlayerStats, CurrentTarget.Stats, spellElement);
            OnPlayerDamageDealt?.Invoke(result);
            CurrentTarget.TakeDamage(result.FinalDamage);
            _skillSystem?.OnMagicHit(result.FinalDamage);
            RebuildStats();

            if (!CurrentTarget.IsAlive()) HandleEnemyDeath(CurrentTarget);
        }

        // ── Ranged Attack ─────────────────────────────────────────────────────

        /// <summary>
        /// Fire a single ranged arrow/bolt at the current target.
        /// Costs SP, uses Ranged skill XP, scaled by DEX.
        /// </summary>
        public void FireRangedAttack()
        {
            if (CurrentTarget == null || !CurrentTarget.IsAlive()) return;

            float dist = Vector3.Distance(transform.position, CurrentTarget.transform.position);
            if (dist > RangedRange) { Debug.Log("[Combat] Target out of range for ranged attack."); return; }
            if (!PlayerStats.ConsumeSP(RangedSPCost)) { Debug.Log("[Combat] Not enough SP!"); return; }

            var result = CombatCalculator.RangedDamage(PlayerStats, CurrentTarget.Stats, CardSystem);
            OnPlayerDamageDealt?.Invoke(result);

            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge) return;

            CurrentTarget.TakeDamage(result.FinalDamage);
            _skillSystem?.OnRangedHit(result.FinalDamage);
            RebuildStats();

            if (!CurrentTarget.IsAlive()) HandleEnemyDeath(CurrentTarget);
        }

        // ── Receive Damage ────────────────────────────────────────────────────

        public void ReceiveDamage(DamageResult result)
        {
            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge) return;

            int dmg = result.FinalDamage;

            // Sleeping players take double damage and wake up on first hit
            if (_statusEffects != null && _statusEffects.Has(StatusEffectType.Sleep))
            {
                dmg *= 2;
                _statusEffects.Remove(StatusEffectType.Sleep);
            }

            PlayerStats.TakeDamage(dmg);
            _skillSystem?.OnDamageTaken(dmg);
            RebuildStats();
            OnPlayerDamageTaken?.Invoke(result);

            if (!PlayerStats.IsAlive()) HandlePlayerDeath();
        }

        // ── Death Handling ────────────────────────────────────────────────────

        private void HandleEnemyDeath(Enemy.EnemyController enemy)
        {
            long xp = CombatCalculator.CalculateKillXP(enemy.Stats, PlayerStats.BaseLevel);
            _skillSystem?.AwardXP(SkillType.Attack,    xp);
            _skillSystem?.AwardXP(SkillType.Hitpoints, xp / 3);
            OnEnemyDefeated?.Invoke(enemy);
            ClearTarget();
        }

        private void HandlePlayerDeath()
        {
            _statusEffects?.ClearAll();
            SetState(CombatState.PlayerDead);
            OnPlayerDefeated?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetState(CombatState state)
        {
            _state = state;
            OnStateChanged?.Invoke(state);
        }

        public CombatState GetState() => _state;

        public void Respawn()
        {
            PlayerStats.FullRestore();
            SetState(CombatState.Idle);
        }
    }
}
