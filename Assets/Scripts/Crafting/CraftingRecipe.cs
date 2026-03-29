using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Skills;

namespace RagnaRune.Crafting
{
    [Serializable]
    public class RecipeIngredient
    {
        public CraftingItem Item;
        [Min(1)] public int Count = 1;
    }

    [CreateAssetMenu(menuName = "RagnaRune/Crafting/Recipe", fileName = "Recipe_")]
    public class CraftingRecipe : ScriptableObject
    {
        public string RecipeName;

        [Header("Requirements")]
        public SkillType RequiredSkill = SkillType.Cooking;
        [Min(1)] public int RequiredLevel = 1;

        [Header("Cost / output")]
        public List<RecipeIngredient> Ingredients = new();
        public CraftingItem ResultItem;
        [Min(1)] public int ResultCount = 1;

        [Header("Rewards")]
        [Min(0)] public long SkillXpAward = 25;
    }
}
