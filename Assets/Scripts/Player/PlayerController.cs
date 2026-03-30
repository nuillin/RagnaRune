using UnityEngine;
using RagnaRune.Combat;
using RagnaRune.Core;
using RagnaRune.Cards;

namespace RagnaRune.Player
{
    /// <summary>
    /// Handles player input, movement, click-to-target, and spell/ranged hotkeys.
    /// Uses the legacy Input Manager only — no dependency on the New Input System package.
    /// If you want New Input System, remove this file and implement PlayerInputActions instead.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CombatManager))]
    public class PlayerController : MonoBehaviour
    {
        // ── Config ────────────────────────────────────────────────────────────
        [Header("Movement")]
        public float MoveSpeed = 4f;

        [Header("Interaction")]
        public float InteractRange = 1.5f;
        public LayerMask EnemyLayer;
        public LayerMask InteractableLayer;

        [Header("Combat Hotkeys — Magic")]
        public KeyCode FireSpellKey  = KeyCode.Alpha1;
        public KeyCode WaterSpellKey = KeyCode.Alpha2;
        public KeyCode HolySpellKey  = KeyCode.Alpha3;
        public KeyCode WindSpellKey  = KeyCode.Alpha4;

        [Header("Combat Hotkeys — Ranged")]
        public KeyCode RangedAttackKey = KeyCode.R;

        // ── References ────────────────────────────────────────────────────────
        private Rigidbody2D    _rb;
        private CombatManager  _combat;
        private CardSystem     _cardSystem;
        private Camera         _cam;

        // ── State ─────────────────────────────────────────────────────────────
        private Vector2 _moveInput;
        private bool    _isDead;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb         = GetComponent<Rigidbody2D>();
            _combat     = GetComponent<CombatManager>();
            _cardSystem = GetComponent<CardSystem>();
            _cam        = Camera.main;

            _rb.gravityScale = 0f;
            _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }

        private void OnEnable()
        {
            _combat.OnPlayerDefeated += OnDied;
            _combat.OnEnemyDefeated  += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            _combat.OnPlayerDefeated -= OnDied;
            _combat.OnEnemyDefeated  -= OnEnemyDefeated;
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_isDead) return;

            ReadMovementInput();
            HandleMouseClick();
            HandleSpellHotkeys();
            HandleRangedHotkey();
        }

        private void ReadMovementInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            _moveInput = new Vector2(h, v).normalized;
        }

        private void FixedUpdate()
        {
            if (_isDead) return;
            _rb.linearVelocity = _moveInput * MoveSpeed;
        }

        // ── Click to Target ───────────────────────────────────────────────────

        private void HandleMouseClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector2 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);

            // Priority: enemy → interactable → empty (move)
            var hit = Physics2D.OverlapPoint(worldPos, EnemyLayer);
            if (hit != null && hit.TryGetComponent<Enemy.EnemyController>(out var enemy))
            {
                _combat.SetTarget(enemy);
                return;
            }

            // Interactables (shops, warps, gathering nodes) handled by their own triggers
        }

        // ── Spells ────────────────────────────────────────────────────────────

        private void HandleSpellHotkeys()
        {
            if (Input.GetKeyDown(FireSpellKey))  _combat.CastSpell(Element.Fire,  15);
            if (Input.GetKeyDown(WaterSpellKey)) _combat.CastSpell(Element.Water, 15);
            if (Input.GetKeyDown(HolySpellKey))  _combat.CastSpell(Element.Holy,  20);
            if (Input.GetKeyDown(WindSpellKey))  _combat.CastSpell(Element.Wind,  15);
        }

        // ── Ranged ────────────────────────────────────────────────────────────

        private void HandleRangedHotkey()
        {
            if (Input.GetKeyDown(RangedAttackKey))
                _combat.FireRangedAttack();
        }

        // ── Death / Respawn ───────────────────────────────────────────────────

        private void OnDied()
        {
            _isDead = true;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log("[Player] Died.");
        }

        private void OnEnemyDefeated(Enemy.EnemyController enemy)
        {
            if (enemy.Data?.CardDrops == null) return;
            foreach (var drop in enemy.Data.CardDrops)
            {
                if (Random.value < drop.DropRate && drop.Card != null)
                    _cardSystem?.AddToInventory(drop.Card);
            }
        }

        public void Respawn()
        {
            _isDead = false;
            _combat.Respawn();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, InteractRange);
        }
#endif
    }
}
