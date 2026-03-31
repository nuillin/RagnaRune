## crafting_service.gd
## Pure-static crafting logic. No Node, no state.
class_name CraftingService
extends RefCounted

enum Result { SUCCESS, INVALID_SETUP, LEVEL_TOO_LOW, MISSING_INGREDIENTS }

static func try_craft(recipe: CraftingRecipe, inventory: ItemInventory,
		skills: SkillSystem) -> int:
	if recipe == null or inventory == null or skills == null:
		return Result.INVALID_SETUP
	if recipe.result_item == null:
		return Result.INVALID_SETUP
	if skills.get_level(recipe.required_skill) < recipe.required_level:
		return Result.LEVEL_TOO_LOW
	if not inventory.has_ingredients(recipe.ingredients):
		return Result.MISSING_INGREDIENTS
	if not inventory.try_consume(recipe.ingredients):
		return Result.MISSING_INGREDIENTS
	inventory.add(recipe.result_item, recipe.result_count)
	skills.award_xp(recipe.required_skill, recipe.skill_xp_award)
	return Result.SUCCESS
