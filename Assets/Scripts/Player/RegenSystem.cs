using UnityEngine;
using RagnaRune.Combat;
using RagnaRune.Core;

namespace RagnaRune.Player
{
    /// <summary>
    /// Passive HP and SP regeneration. Mirrors RO's natural regen:
    ///   - HP regen every <see cref="HPTickInterval"/> seconds = max(1, VIT/5 + level/10)
    ///   - SP regen every <see cref="SPTickInterval"/> seconds = max(1, INT/6 + level/12)
    ///   - Both tick faster when the player is standing still (sitting bonus).
    ///   - Regen pauses for <see cref="CombatRegenDelay"/> seconds after taking damage.
    ///
    /// Attach to the Player GameObject alongside CombatManager.
    /// </summary>
    [RequireComponent(typeof(CombatManager))]
    public class RegenSystem : MonoBehaviour
    {
        [Header("HP Regen")]
        public float HPTickInterval   = 8f;   // seconds between HP ticks (standing)
        public float HPSitMultiplier  = 2.5f; // multiplier when standing still

        [Header("SP Regen")]
        public float SPTickInterval   = 6f;
        public float SPSitMultiplier  = 3f;

        [Header("Combat Penalty")]
        [Tooltip("Seconds after taking damage before regen resumes.")]
        public float CombatRegenDelay = 5f;

        [Header("Still Threshold")]
        [Tooltip("Speed (units/s) below which the player is considered standing still.")]
        public float StillThreshold = 0.05f;

        // ── Private ───────────────────────────────────────────────────────────
        private CombatManager _combat;
        private Rigidbody2D   _rb;

        private float _hpTimer;
        private float _spTimer;
        private float _damageCooldown;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _combat = GetComponent<CombatManager>();
            _rb     = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _combat.OnPlayerDamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            _combat.OnPlayerDamageTaken -= OnDamageTaken;
        }

        private void Update()
        {
            if (_combat?.PlayerStats == null) return;
            if (_combat.GetState() == CombatState.PlayerDead) return;

            _damageCooldown -= Time.deltaTime;

            // No regen while recently hit
            if (_damageCooldown > 0f) return;

            bool isStill = _rb == null || _rb.linearVelocity.sqrMagnitude < StillThreshold * StillThreshold;
            float hpInterval = isStill ? HPTickInterval / HPSitMultiplier : HPTickInterval;
            float spInterval = isStill ? SPTickInterval / SPSitMultiplier : SPTickInterval;

            _hpTimer += Time.deltaTime;
            _spTimer += Time.deltaTime;

            var s = _combat.PlayerStats;

            if (_hpTimer >= hpInterval)
            {
                _hpTimer = 0f;
                int heal = Mathf.Max(1, s.VIT / 5 + s.BaseLevel / 10);
                s.CurrentHP = Mathf.Min(s.CurrentHP + heal, s.MaxHP);
            }

            if (_spTimer >= spInterval)
            {
                _spTimer = 0f;
                int spHeal = Mathf.Max(1, s.INT / 6 + s.BaseLevel / 12);
                s.CurrentSP = Mathf.Min(s.CurrentSP + spHeal, s.MaxSP);
            }
        }

        private void OnDamageTaken(DamageResult _)
        {
            _damageCooldown = CombatRegenDelay;
        }
    }
}
