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
    /// Attach to the Player GameObject.
    /// Subscribe to the events to update UI and spawn effects.
    /// </summary>
    [RequireComponent(typeof(SkillSystem))]
    public class CombatManager : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────
        public event Action<DamageResult> OnPlayerDamageDealt;
        public event Action<DamageResult> OnPlayerDamageTaken;
        public event Action<Enemy.EnemyController> OnEnemyDefeated;
        public event Action OnPlayerDefeated;
        public event Action<CombatState> OnStateChanged;

        // ── References ────────────────────────────────────────────────────────
        [Header("Player")]
        public CharacterStats PlayerStats;
        public CardSystem CardSystem;
        private SkillSystem _skillSystem;

        [Header("Current Target")]
        public Enemy.EnemyController CurrentTarget;

        // ── Internal State ────────────────────────────────────────────────────
        private CombatState _state = CombatState.Idle;
        private float _attackTimer = 0f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _skillSystem = GetComponent<SkillSystem>();
        }

        private void Start()
        {
            if (PlayerStats == null) PlayerStats = new CharacterStats();
            RebuildStats();
            PlayerStats.FullRestore();
        }

        // ── Called externally to rebuild after card/skill changes ─────────────
        public void RebuildStats()
        {
            if (PlayerStats == null) PlayerStats = new CharacterStats();
            // Clear bonus fields before re-applying
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
            if (enemy == null || !enemy.IsAlive())
            {
                ClearTarget();
                return;
            }
            CurrentTarget = enemy;
            SetState(CombatState.InCombat);
            _attackTimer = 0f;   // start attacking immediately
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
            if (CurrentTarget == null || !CurrentTarget.IsAlive())
            {
                ClearTarget();
                return;
            }

            _attackTimer += Time.deltaTime;
            float attackInterval = 1f / PlayerStats.ASPD;

            if (_attackTimer >= attackInterval)
            {
                _attackTimer -= attackInterval;
                ExecuteAutoAttack();
            }
        }

        // ── Auto-attack ───────────────────────────────────────────────────────

        private void ExecuteAutoAttack()
        {
            var result = CombatCalculator.PhysicalDamage(
                PlayerStats,
                CurrentTarget.Stats,
                CardSystem);

            OnPlayerDamageDealt?.Invoke(result);

            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge)
                return;

            CurrentTarget.TakeDamage(result.FinalDamage);
            _skillSystem?.OnMeleeHit(result.FinalDamage);
            RebuildStats();   // skills changed — recalculate

            if (!CurrentTarget.IsAlive())
                HandleEnemyDeath(CurrentTarget);
        }

        // ── Skill Attacks (call from input / hotbar) ──────────────────────────

        /// <summary>Fire a magic spell against the current target.</summary>
        public void CastSpell(Element spellElement, int spCost = 10)
        {
            if (CurrentTarget == null || !CurrentTarget.IsAlive()) return;
            if (!PlayerStats.ConsumeSP(spCost)) { Debug.Log("Not enough SP!"); return; }

            var result = CombatCalculator.MagicDamage(PlayerStats, CurrentTarget.Stats, spellElement);
            OnPlayerDamageDealt?.Invoke(result);
            CurrentTarget.TakeDamage(result.FinalDamage);
            _skillSystem?.OnMagicHit(result.FinalDamage);
            RebuildStats();

            if (!CurrentTarget.IsAlive())
                HandleEnemyDeath(CurrentTarget);
        }

        // ── Receive Damage (called by EnemyController) ────────────────────────

        public void ReceiveDamage(DamageResult result)
        {
            if (result.Hit == HitResult.Miss || result.Hit == HitResult.PerfectDodge) return;
            PlayerStats.TakeDamage(result.FinalDamage);
            _skillSystem?.OnDamageTaken(result.FinalDamage);
            RebuildStats();
            OnPlayerDamageTaken?.Invoke(result);

            if (!PlayerStats.IsAlive()) HandlePlayerDeath();
        }

        // ── Death Handling ────────────────────────────────────────────────────

        private void HandleEnemyDeath(Enemy.EnemyController enemy)
        {
            // Award kill XP to combat skills
            long xp = CombatCalculator.CalculateKillXP(enemy.Stats, PlayerStats.BaseLevel);
            _skillSystem?.AwardXP(SkillType.Attack,    xp);
            _skillSystem?.AwardXP(SkillType.Hitpoints, xp / 3);

            OnEnemyDefeated?.Invoke(enemy);
            ClearTarget();
        }

        private void HandlePlayerDeath()
        {
            SetState(CombatState.PlayerDead);
            OnPlayerDefeated?.Invoke();
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private void SetState(CombatState state)
        {
            _state = state;
            OnStateChanged?.Invoke(state);
        }

        public CombatState GetState() => _state;

        /// <summary>Respawn player at full HP/SP.</summary>
        public void Respawn()
        {
            PlayerStats.FullRestore();
            SetState(CombatState.Idle);
        }
    }
}
