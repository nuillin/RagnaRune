using System;
using System.Collections.Generic;
using UnityEngine;

namespace RagnaRune.Crafting
{
    /// <summary>
    /// Maps saved <see cref="CraftingItem.ItemId"/> (and asset <c>name</c>) back to SO instances at load time.
    /// Assign every craftable item here (or a curated list).
    /// </summary>
    [CreateAssetMenu(menuName = "RagnaRune/Crafting/Catalog", fileName = "CraftingCatalog")]
    public class CraftingCatalog : ScriptableObject
    {
        [SerializeField] private List<CraftingItem> _items = new();

        private Dictionary<string, CraftingItem> _byKey;
        private bool _built;

        private void OnEnable() => _built = false;

        public void RebuildIfNeeded()
        {
            if (_built && _byKey != null) return;
            _byKey = new Dictionary<string, CraftingItem>(StringComparer.Ordinal);
            foreach (var item in _items)
            {
                if (item == null) continue;
                if (!string.IsNullOrEmpty(item.ItemId))
                    _byKey.TryAdd(item.ItemId, item);
                _byKey.TryAdd(item.name, item);
            }
            _built = true;
        }

        /// <summary>Resolve by <see cref="CraftingItem.ItemId"/> first, then by asset name.</summary>
        public CraftingItem Resolve(string itemIdOrName)
        {
            if (string.IsNullOrEmpty(itemIdOrName)) return null;
            RebuildIfNeeded();
            if (_byKey.TryGetValue(itemIdOrName, out var found)) return found;
            foreach (var item in _items)
            {
                if (item == null) continue;
                if (item.name == itemIdOrName) return item;
            }
            return null;
        }
    }
}
