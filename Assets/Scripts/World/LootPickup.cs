using System.Collections;
using UnityEngine;
using RagnaRune.Cards;
using RagnaRune.Crafting;

namespace RagnaRune.World
{
    public enum LootType { Zeny, Card, Item }

    /// <summary>
    /// A ground loot object (RO-style). Spawned by EnemyController on death.
    /// Player walks into the trigger or presses E to pick up.
    /// Auto-destroys after <see cref="LifetimeSeconds"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LootPickup : MonoBehaviour
    {
        [Header("Content")]
        public LootType Type = LootType.Zeny;
        public int      ZenyAmount = 0;
        public CardData Card;
        public CraftingItem Item;
        public int      ItemCount = 1;

        [Header("Behaviour")]
        public KeyCode  PickupKey       = KeyCode.E;
        public float    LifetimeSeconds = 60f;
        [Tooltip("Auto-vacuum into player once they're within this range (0 = off).")]
        public float    VacuumRange     = 1f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private bool _playerInRange;
        private Transform _playerTransform;
        private bool _pickedUp;

        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            GetComponent<Collider2D>().isTrigger = true;
            StartCoroutine(ExpireAfter(LifetimeSeconds));
        }

        private void Update()
        {
            if (_pickedUp) return;

            // Vacuum pick-up (like RS area loot or RO auto-loot)
            if (VacuumRange > 0f && _playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, _playerTransform.position);
                if (dist <= VacuumRange)
                {
                    Collect(_playerTransform);
                    return;
                }
            }

            // Manual pick-up
            if (_playerInRange && Input.GetKeyDown(PickupKey) && _playerTransform != null)
                Collect(_playerTransform);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange    = true;
            _playerTransform  = other.transform;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
        }

        // ── Collection ────────────────────────────────────────────────────────

        private void Collect(Transform player)
        {
            _pickedUp = true;

            switch (Type)
            {
                case LootType.Zeny:
                    Managers.GameManager.Instance?.AddZeny(ZenyAmount);
                    Debug.Log($"[Loot] Picked up {ZenyAmount} Zeny");
                    break;

                case LootType.Card:
                    if (Card != null)
                    {
                        var cards = player.GetComponent<CardSystem>();
                        cards?.AddToInventory(Card);
                        Debug.Log($"[Loot] Picked up {Card.CardName}");
                    }
                    break;

                case LootType.Item:
                    if (Item != null)
                    {
                        var inv = player.GetComponent<ItemInventory>();
                        inv?.Add(Item, ItemCount);
                        Debug.Log($"[Loot] Picked up {Item.DisplayName} ×{ItemCount}");
                    }
                    break;
            }

            Destroy(gameObject);
        }

        private IEnumerator ExpireAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!_pickedUp) Destroy(gameObject);
        }

        // ── Static factory ────────────────────────────────────────────────────

        /// <summary>
        /// Spawn a loot drop at position. Pass a prefab that has LootPickup + SpriteRenderer + Collider2D.
        /// </summary>
        public static LootPickup SpawnZeny(GameObject prefab, Vector3 position, int amount)
        {
            var go   = Instantiate(prefab, position, Quaternion.identity);
            var loot = go.GetComponent<LootPickup>();
            if (loot == null) loot = go.AddComponent<LootPickup>();
            loot.Type       = LootType.Zeny;
            loot.ZenyAmount = amount;
            return loot;
        }

        public static LootPickup SpawnCard(GameObject prefab, Vector3 position, CardData card)
        {
            var go   = Instantiate(prefab, position, Quaternion.identity);
            var loot = go.GetComponent<LootPickup>();
            if (loot == null) loot = go.AddComponent<LootPickup>();
            loot.Type = LootType.Card;
            loot.Card = card;
            return loot;
        }
    }
}
