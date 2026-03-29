using UnityEngine;

namespace RagnaRune.Crafting
{
    [CreateAssetMenu(menuName = "RagnaRune/Crafting/Crafting Item", fileName = "Item_")]
    public class CraftingItem : ScriptableObject
    {
        [Tooltip("Stable id for saves (recommended). If empty, the asset name is used when saving.")]
        public string ItemId;

        public string DisplayName;
    }
}
