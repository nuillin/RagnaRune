using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Cards;
using RagnaRune.Crafting;

namespace RagnaRune.World
{
    public enum ShopItemType { Card, CraftingItem }

    [Serializable]
    public class ShopEntry
    {
        public ShopItemType Type;
        public CardData     Card;
        public CraftingItem Item;
        public int          ItemCount  = 1;
        public int          BuyPrice;    // Zeny cost to buy
        public int          SellPrice;   // Zeny gained when player sells back
    }

    /// <summary>
    /// RO-style NPC shop. Walk up and press InteractKey to open.
    /// Uses Zeny (managed by GameManager) as currency.
    ///
    /// For a UI, subscribe to OnShopOpened and render the ShopEntry list.
    /// Call TryBuy / TrySell from UI button callbacks.
    ///
    /// This class handles only the data and logic — no UGUI dependency.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShopNPC : MonoBehaviour
    {
        public event Action<ShopNPC>   OnShopOpened;
        public event Action            OnShopClosed;
        public event Action<string>    OnTransactionMessage;  // e.g. "Not enough Zeny."

        [Header("Identity")]
        public string ShopName  = "Tool Dealer";
        public string NPCDialog = "Welcome! Take a look at my wares.";

        [Header("Stock")]
        public List<ShopEntry> Stock = new();

        [Header("Interaction")]
        public KeyCode InteractKey = KeyCode.F;
        public float   InteractRange = 2f;

        // ── Private ───────────────────────────────────────────────────────────
        private bool      _playerInRange;
        private Transform _playerTransform;
        private bool      _isOpen;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerTransform = other.transform;
            _playerInRange   = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            Close();
        }

        private void Update()
        {
            if (!_playerInRange) return;
            if (Input.GetKeyDown(InteractKey))
            {
                if (_isOpen) Close();
                else         Open();
            }
        }

        // ── Open / Close ──────────────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            OnShopOpened?.Invoke(this);
            Debug.Log($"[Shop] {ShopName}: {NPCDialog}");
        }

        public void Close()
        {
            _isOpen = false;
            OnShopClosed?.Invoke();
        }

        // ── Transactions ──────────────────────────────────────────────────────

        /// <summary>Player buys an item. Returns true on success.</summary>
        public bool TryBuy(ShopEntry entry)
        {
            if (_playerTransform == null) return false;

            var gm = Managers.GameManager.Instance;
            if (gm == null) return false;

            if (!gm.SpendZeny(entry.BuyPrice))
            {
                OnTransactionMessage?.Invoke("Not enough Zeny.");
                return false;
            }

            DeliverToPlayer(entry);
            OnTransactionMessage?.Invoke($"Bought {GetEntryName(entry)} for {entry.BuyPrice} z.");
            return true;
        }

        /// <summary>Player sells an item back. Returns true on success.</summary>
        public bool TrySell(ShopEntry entry)
        {
            if (_playerTransform == null) return false;
            if (entry.SellPrice <= 0) { OnTransactionMessage?.Invoke("This item cannot be sold here."); return false; }

            if (!TakeFromPlayer(entry))
            {
                OnTransactionMessage?.Invoke("You don't have that item.");
                return false;
            }

            Managers.GameManager.Instance?.AddZeny(entry.SellPrice);
            OnTransactionMessage?.Invoke($"Sold {GetEntryName(entry)} for {entry.SellPrice} z.");
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void DeliverToPlayer(ShopEntry entry)
        {
            switch (entry.Type)
            {
                case ShopItemType.Card:
                    if (entry.Card != null)
                        _playerTransform.GetComponent<CardSystem>()?.AddToInventory(entry.Card);
                    break;
                case ShopItemType.CraftingItem:
                    if (entry.Item != null)
                        _playerTransform.GetComponent<ItemInventory>()?.Add(entry.Item, entry.ItemCount);
                    break;
            }
        }

        private bool TakeFromPlayer(ShopEntry entry)
        {
            switch (entry.Type)
            {
                case ShopItemType.CraftingItem:
                    var inv = _playerTransform.GetComponent<ItemInventory>();
                    return inv != null && inv.TryRemove(entry.Item, entry.ItemCount);
                default:
                    // Cards are unique instances; selling them is a future feature
                    return false;
            }
        }

        private static string GetEntryName(ShopEntry entry) => entry.Type switch
        {
            ShopItemType.Card         => entry.Card?.CardName   ?? "Card",
            ShopItemType.CraftingItem => entry.Item?.DisplayName ?? "Item",
            _                         => "Unknown",
        };

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, InteractRange);
        }
#endif
    }
}
