## crafting_station.gd
## Stand in trigger, press "interact" (E) to craft one recipe.
##
## Scene tree:
##   CraftingStation (Area2D)
##   ├─ CollisionShape2D
##   └─ Sprite2D
class_name CraftingStation
extends Area2D

@export var recipe: CraftingRecipe
@export var cooldown_seconds: float = 0.35

var _inventory: ItemInventory  = null
var _skills: SkillSystem       = null
var _cooldown_end: float       = 0.0

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _process(_delta: float) -> void:
	if recipe == null or _inventory == null or _skills == null: return
	if Time.get_ticks_msec() / 1000.0 < _cooldown_end: return
	if not Input.is_action_just_pressed("interact"): return

	var result := CraftingService.try_craft(recipe, _inventory, _skills)
	_cooldown_end = Time.get_ticks_msec() / 1000.0 + cooldown_seconds
	match result:
		CraftingService.Result.SUCCESS:
			print("[Crafting] Made ", recipe.result_item.display_name, " ×", recipe.result_count)
		CraftingService.Result.LEVEL_TOO_LOW:
			print("[Crafting] Need ", SkillSystem.Type.keys()[recipe.required_skill],
				" level ", recipe.required_level, " for ", recipe.recipe_name, ".")
		CraftingService.Result.MISSING_INGREDIENTS:
			print("[Crafting] Missing ingredients for ", recipe.recipe_name, ".")
		CraftingService.Result.INVALID_SETUP:
			push_warning("[Crafting] Invalid recipe or references.")

func _on_body_entered(body: Node2D) -> void:
	var inv: ItemInventory = body.get_node_or_null("ItemInventory")
	var ss: SkillSystem    = body.get_node_or_null("SkillSystem")
	if inv: _inventory = inv
	if ss:  _skills    = ss

func _on_body_exited(body: Node2D) -> void:
	var inv: ItemInventory = body.get_node_or_null("ItemInventory")
	var ss: SkillSystem    = body.get_node_or_null("SkillSystem")
	if inv and inv == _inventory: _inventory = null
	if ss  and ss  == _skills:   _skills    = null
