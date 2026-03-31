## crafting_recipe.gd
class_name CraftingRecipe
extends Resource

@export var recipe_name: String = ""

@export_group("Requirements")
@export var required_skill: int = SkillSystem.Type.COOKING
@export var required_level: int = 1

@export_group("Ingredients / Output")
@export var ingredients: Array[RecipeIngredient] = []
@export var result_item: CraftingItem
@export var result_count: int = 1

@export_group("Rewards")
@export var skill_xp_award: int = 25


## Inline ingredient entry so the whole recipe is one Resource file.
class RecipeIngredient:
	@export var item: CraftingItem
	@export var count: int = 1
