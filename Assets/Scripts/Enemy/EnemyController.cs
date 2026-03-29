using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using RagnaRune.Core;
using RagnaRune.Combat;
using RagnaRune.Cards;

namespace RagnaRune.Enemy
{
    public enum EnemyAIState { Idle, Wandering, Chasing, Attacking, Dead }

    /// <summary>
    /// Enemy MonoBehaviour. Uses NavMeshAgent for movement.
    /// Requires a NavMesh in the scene (bake via Window > AI > Navigation).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        public event Action<EnemyController> OnDied;
        public event Action<int>             OnDamageTaken;

        [Header("Data")]
        public EnemyData Data;

        [Header("Runtime State — read-only in play mode")]
        public CharacterStats Stats;
        public EnemyAIState AIState = EnemyAIState.Idle;

        // ── Private ───────────────────────────────────────────────────────────
        private NavMeshAgent _agent;
        private Transform    _playerTransform;
        private CombatManager _playerCombat;

        private float _attackTimer = 0f;
        private float _wanderTimer = 0f;
        private Vector3 _wanderTarget;
        private bool _isInitialised = false;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updateUpAxis   = false;   // 2D sprite worlds
        }

        public void Initialise(EnemyData data, Transform player, CombatManager playerCombat)
        {
            Data = data;
            // Deep-copy stats so the ScriptableObject baseline isn't mutated at runtime
            Stats = new CharacterStats
            {
                STR = data.BaseStats.STR, AGI = data.BaseStats.AGI,
                VIT = data.BaseStats.VIT, INT = data.BaseStats.INT,
                DEX = data.BaseStats.DEX, LUK = data.BaseStats.LUK,
                BaseLevel    = data.BaseStats.BaseLevel,
                BodyElement  = data.BaseStats.BodyElement,
                WeaponElement = data.BaseStats.WeaponElement,
                Size         = data.BaseStats.Size,
            };
            Stats.RecalcDerived();
            Stats.FullRestore();

            _playerTransform = player;
            _playerCombat    = playerCombat;
            _agent.speed     = data.MoveSpeed;
            _isInitialised   = true;
        }

        public bool IsAlive() => Stats != null && Stats.IsAlive();

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isInitialised || !IsAlive()) return;

            float distToPlayer = _playerTransform
                ? Vector3.Distance(transform.position, _playerTransform.position)
                : float.MaxValue;

            switch (AIState)
            {
                case EnemyAIState.Idle:       UpdateIdle(distToPlayer);      break;
                case EnemyAIState.Wandering:  UpdateWandering(distToPlayer); break;
                case EnemyAIState.Chasing:    UpdateChasing(distToPlayer);   break;
                case EnemyAIState.Attacking:  UpdateAttacking(distToPlayer); break;
            }
        }

        // ── AI States ─────────────────────────────────────────────────────────

        private void UpdateIdle(float distToPlayer)
        {
            _wanderTimer += Time.deltaTime;
            if (_wanderTimer > UnityEngine.Random.Range(3f, 7f))
            {
                SetState(EnemyAIState.Wandering);
                _wanderTimer = 0f;
            }
            if (distToPlayer < Data.AggroRange)
                SetState(EnemyAIState.Chasing);
        }

        private void UpdateWandering(float distToPlayer)
        {
            if (distToPlayer < Data.AggroRange)
            {
                SetState(EnemyAIState.Chasing);
                return;
            }
            if (!_agent.hasPath || _agent.remainingDistance < 0.3f)
            {
                Vector2 rand = UnityEngine.Random.insideUnitCircle * Data.WanderRadius;
                _wanderTarget = transform.position + new Vector3(rand.x, rand.y, 0);
                _agent.SetDestination(_wanderTarget);
                _wanderTimer += Time.deltaTime;
                if (_wanderTimer > 10f) SetState(EnemyAIState.Idle);
            }
        }

        private void UpdateChasing(float distToPlayer)
        {
            if (distToPlayer > Data.AggroRange * 1.5f)
            {
                SetState(EnemyAIState.Idle);
                return;
            }
            if (distToPlayer <= Data.AttackRange)
            {
                _agent.ResetPath();
                SetState(EnemyAIState.Attacking);
                return;
            }
            if (_playerTransform)
                _agent.SetDestination(_playerTransform.position);
        }

        private void UpdateAttacking(float distToPlayer)
        {
            if (distToPlayer > Data.AttackRange)
            {
                SetState(EnemyAIState.Chasing);
                return;
            }
            _attackTimer += Time.deltaTime;
            float interval = 1f / Stats.ASPD;
            if (_attackTimer >= interval)
            {
                _attackTimer -= interval;
                ExecuteAttack();
            }
        }

        private void ExecuteAttack()
        {
            if (_playerCombat == null) return;
            var result = CombatCalculator.PhysicalDamage(Stats, _playerCombat.PlayerStats);
            _playerCombat.ReceiveDamage(result);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void TakeDamage(int amount)
        {
            Stats.TakeDamage(amount);
            OnDamageTaken?.Invoke(amount);

            if (!Stats.IsAlive()) Die();
        }

        // ── Death ─────────────────────────────────────────────────────────────

        private void Die()
        {
            SetState(EnemyAIState.Dead);
            _agent.ResetPath();
            _agent.enabled = false;
            OnDied?.Invoke(this);

            // Roll for card drops
            if (Data.CardDrops != null)
            {
                foreach (var drop in Data.CardDrops)
                {
                    if (UnityEngine.Random.value < drop.DropRate)
                        Debug.Log($"[Drop] {Data.EnemyName} dropped: {drop.Card.CardName}!");
                }
            }

            // Visual death — in a real project, trigger an animation here
            StartCoroutine(DestroyAfterDelay(1.5f));
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private void SetState(EnemyAIState state)
        {
            AIState = state;
        }
    }
}
