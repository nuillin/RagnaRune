using RagnaRune.Skills;

namespace RagnaRune.Crafting
{
    public enum CraftTryResult
    {
        Success,
        InvalidSetup,
        LevelTooLow,
        MissingIngredients,
    }

    public static class CraftingService
    {
        /// <summary>
        /// Checks skill level first, then ingredients; only consumes items and awards XP on full success.
        /// </summary>
        public static CraftTryResult TryCraft(
            CraftingRecipe recipe,
            ItemInventory inventory,
            SkillSystem skills)
        {
            if (recipe == null || inventory == null || skills == null)
                return CraftTryResult.InvalidSetup;
            if (recipe.ResultItem == null)
                return CraftTryResult.InvalidSetup;

            if (skills.GetLevel(recipe.RequiredSkill) < recipe.RequiredLevel)
                return CraftTryResult.LevelTooLow;

            if (!inventory.HasIngredients(recipe.Ingredients))
                return CraftTryResult.MissingIngredients;

            if (!inventory.TryConsume(recipe.Ingredients))
                return CraftTryResult.MissingIngredients;

            inventory.Add(recipe.ResultItem, recipe.ResultCount);
            skills.AwardXP(recipe.RequiredSkill, recipe.SkillXpAward);
            return CraftTryResult.Success;
        }
    }
}
