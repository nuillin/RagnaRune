using UnityEngine;
using RagnaRune.Skills;

namespace RagnaRune.Crafting
{
    /// <summary>
    /// Stand in trigger, press key to run <see cref="CraftingService.TryCraft"/> for one recipe.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CraftingStation : MonoBehaviour
    {
        public CraftingRecipe Recipe;
        public KeyCode InteractKey = KeyCode.E;
        public float CooldownSeconds = 0.35f;

        private float _cooldownEnd;
        private ItemInventory _inventory;
        private SkillSystem _skills;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var inv = other.GetComponentInParent<ItemInventory>(true);
            var ss  = other.GetComponentInParent<SkillSystem>(true);
            if (inv != null) _inventory = inv;
            if (ss != null) _skills = ss;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var inv = other.GetComponentInParent<ItemInventory>(true);
            var ss  = other.GetComponentInParent<SkillSystem>(true);
            if (inv != null && inv == _inventory) _inventory = null;
            if (ss != null && ss == _skills) _skills = null;
        }

        private void Update()
        {
            if (Recipe == null || _inventory == null || _skills == null) return;
            if (Time.time < _cooldownEnd) return;
            if (!Input.GetKeyDown(InteractKey)) return;

            var r = CraftingService.TryCraft(Recipe, _inventory, _skills);
            _cooldownEnd = Time.time + CooldownSeconds;

            switch (r)
            {
                case CraftTryResult.Success:
                    Debug.Log($"[Crafting] Made {Recipe.ResultItem.DisplayName} x{Recipe.ResultCount}");
                    break;
                case CraftTryResult.LevelTooLow:
                    Debug.Log($"[Crafting] Need {Recipe.RequiredSkill} level {Recipe.RequiredLevel} for {Recipe.RecipeName}.");
                    break;
                case CraftTryResult.MissingIngredients:
                    Debug.Log($"[Crafting] Missing ingredients for {Recipe.RecipeName}.");
                    break;
                case CraftTryResult.InvalidSetup:
                    Debug.LogWarning("[Crafting] Invalid recipe or references.");
                    break;
            }
        }
    }
}
