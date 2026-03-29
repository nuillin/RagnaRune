using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Skills;

namespace RagnaRune.Crafting
{
    [Serializable]
    public class ItemStack
    {
        public CraftingItem Item;
        [Min(0)] public int Count;
    }

    /// <summary>
    /// Simple stack list for <see cref="CraftingItem"/>. Attach to the player.
    /// </summary>
    public class ItemInventory : MonoBehaviour
    {
        public event Action OnInventoryChanged;

        [SerializeField] private List<ItemStack> _stacks = new();

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public ItemStackSaveEntry[] BuildSaveEntries()
        {
            var list = new List<ItemStackSaveEntry>();
            foreach (var s in _stacks)
            {
                if (s.Item == null || s.Count <= 0) continue;
                string id = string.IsNullOrEmpty(s.Item.ItemId) ? s.Item.name : s.Item.ItemId;
                list.Add(new ItemStackSaveEntry { ItemId = id, Count = s.Count });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Replaces all stacks. Requires <paramref name="catalog"/> when <paramref name="entries"/> has rows.
        /// </summary>
        public void ApplySaveEntries(ItemStackSaveEntry[] entries, CraftingCatalog catalog)
        {
            _stacks.Clear();
            if (entries == null || entries.Length == 0)
            {
                OnInventoryChanged?.Invoke();
                return;
            }

            if (catalog == null)
            {
                Debug.LogWarning("[ItemInventory] Cannot restore items: CraftingCatalog is null.");
                OnInventoryChanged?.Invoke();
                return;
            }

            foreach (var e in entries)
            {
                if (e.Count <= 0 || string.IsNullOrEmpty(e.ItemId)) continue;
                var item = catalog.Resolve(e.ItemId);
                if (item == null)
                {
                    Debug.LogWarning($"[ItemInventory] Unknown item id or name '{e.ItemId}'.");
                    continue;
                }
                MergeAddInternal(item, e.Count);
            }

            OnInventoryChanged?.Invoke();
        }

        private void MergeAddInternal(CraftingItem item, int count)
        {
            foreach (var s in _stacks)
            {
                if (s.Item == item)
                {
                    s.Count += count;
                    return;
                }
            }
            _stacks.Add(new ItemStack { Item = item, Count = count });
        }

        public int CountOf(CraftingItem item)
        {
            if (item == null) return 0;
            int sum = 0;
            foreach (var s in _stacks)
            {
                if (s.Item == item) sum += s.Count;
            }
            return sum;
        }

        public bool HasIngredients(IReadOnlyList<RecipeIngredient> ingredients)
        {
            if (ingredients == null) return true;
            foreach (var ing in ingredients)
            {
                if (ing.Item == null || ing.Count <= 0) return false;
                if (CountOf(ing.Item) < ing.Count) return false;
            }
            return true;
        }

        /// <summary>Adds or merges stacks; clamps count at 0.</summary>
        public void Add(CraftingItem item, int count)
        {
            if (item == null || count == 0) return;
            if (count < 0)
            {
                TryRemove(item, -count);
                return;
            }

            foreach (var s in _stacks)
            {
                if (s.Item == item)
                {
                    s.Count += count;
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }

            _stacks.Add(new ItemStack { Item = item, Count = count });
            OnInventoryChanged?.Invoke();
        }

        public bool TryRemove(CraftingItem item, int count)
        {
            if (item == null || count <= 0) return false;
            if (CountOf(item) < count) return false;

            int remaining = count;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var s = _stacks[i];
                if (s.Item != item) continue;
                int take = Mathf.Min(s.Count, remaining);
                s.Count -= take;
                remaining -= take;
                if (s.Count <= 0) _stacks.RemoveAt(i);
            }

            OnInventoryChanged?.Invoke();
            return remaining == 0;
        }

        /// <summary>Removes all ingredient lines atomically (validates first via <see cref="HasIngredients"/>).</summary>
        public bool TryConsume(IReadOnlyList<RecipeIngredient> ingredients)
        {
            if (ingredients == null) return true;
            if (!HasIngredients(ingredients)) return false;

            foreach (var ing in ingredients)
            {
                int need = ing.Count;
                for (int i = 0; i < _stacks.Count && need > 0; i++)
                {
                    var s = _stacks[i];
                    if (s.Item != ing.Item) continue;
                    int take = Mathf.Min(s.Count, need);
                    s.Count -= take;
                    need -= take;
                }
            }

            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                if (_stacks[i].Count <= 0) _stacks.RemoveAt(i);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}
