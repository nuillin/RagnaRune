using System;
using UnityEngine;

namespace RagnaRune.Skills
{
    /// <summary>One stack line in JSON (resolved via <see cref="Crafting.CraftingCatalog"/>).</summary>
    [Serializable]
    public class ItemStackSaveEntry
    {
        public string ItemId;
        public int Count;
    }

    /// <summary>
    /// Serializable progress snapshot for <see cref="JsonUtility"/> (arrays, not dictionaries).
    /// Skill indices match <see cref="SkillType"/> enum underlying values (0, 1, 2, …).
    /// </summary>
    [Serializable]
    public class SkillsSaveData
    {
        public const int CurrentFormatVersion = 2;

        public int FormatVersion = CurrentFormatVersion;

        /// <summary>Total XP per skill; index = (int)<see cref="SkillType"/>.</summary>
        public long[] TotalXpBySkillIndex = Array.Empty<long>();

        /// <summary>Present from format 2+; null means omit inventory changes on load (legacy saves).</summary>
        public ItemStackSaveEntry[] Items;
    }
}
