## game_manager.gd
## Autoload singleton. Registered in project.godot as "GameManager".
## Handles: player spawning, enemy spawning + respawn, Zeny, loot drops,
## cross-scene warp coordination, and skill persistence.
extends Node

# ── Signals ────────────────────────────────────────────────────────────────────
signal zeny_changed(new_total: int)

# ── Exports (set via the Inspector on the autoload) ────────────────────────────
@export var player_scene: PackedScene
@export var enemy_scene: PackedScene
@export var loot_scene: PackedScene          ## Scene with LootPickup root node
@export var respawn_delay: float = 10.0
@export var persist_skills: bool = true
@export var item_catalog: CraftingCatalog

# ── Runtime ────────────────────────────────────────────────────────────────────
var _player: PlayerController = null
var _zeny: int = 0
var _active_enemies: Array[EnemyController] = []

## Used by WarpPortal to pass spawn-point id across scene loads.
var pending_spawn_id: String = ""

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	get_tree().node_added.connect(_on_node_added)

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST:
		_save_if_needed()
	elif what == NOTIFICATION_APPLICATION_PAUSED:
		_save_if_needed()

# ── Player ─────────────────────────────────────────────────────────────────────

func spawn_player(spawn_position: Vector2 = Vector2.ZERO) -> PlayerController:
	if player_scene == null:
		push_error("[GameManager] player_scene not assigned!")
		return null
	_player = player_scene.instantiate() as PlayerController
	get_tree().current_scene.add_child(_player)
	_player.global_position = spawn_position

	if persist_skills:
		var skills: SkillSystem   = _player.get_node_or_null("SkillSystem")
		var inv: ItemInventory    = _player.get_node_or_null("ItemInventory")
		if skills:
			SkillPersistence.try_load(skills, inv, item_catalog)

	return _player

func get_player() -> PlayerController:
	return _player

# ── Enemy spawning ─────────────────────────────────────────────────────────────

func spawn_enemy(data: EnemyData, position: Vector2) -> EnemyController:
	if enemy_scene == null or data == null: return null
	var scatter := Vector2(randf_range(-40.0, 40.0), randf_range(-40.0, 40.0))
	var ctrl: EnemyController = enemy_scene.instantiate()
	get_tree().current_scene.add_child(ctrl)
	ctrl.global_position = position + scatter

	var player_combat: CombatManager = null
	if _player: player_combat = _player.get_node_or_null("CombatManager")
	ctrl.initialise(data, _player, player_combat)
	ctrl.died.connect(_on_enemy_died)
	_active_enemies.append(ctrl)
	return ctrl

func _on_enemy_died(enemy: EnemyController) -> void:
	_active_enemies.erase(enemy)
	if enemy.data:
		var z := randi_range(enemy.data.base_zeny_drop, enemy.data.max_zeny_drop)
		add_zeny(z)
		# Schedule respawn
		_respawn_later(enemy.data, enemy.global_position)

func _respawn_later(data: EnemyData, pos: Vector2) -> void:
	await get_tree().create_timer(respawn_delay).timeout
	spawn_enemy(data, pos)

# ── Loot spawning (called by EnemyController) ──────────────────────────────────

func spawn_card_loot(position: Vector2, card: CardData) -> void:
	if loot_scene == null: return
	var loot: LootPickup = loot_scene.instantiate()
	get_tree().current_scene.add_child(loot)
	loot.global_position = position
	loot.loot_type       = LootPickup.LootType.CARD
	loot.card            = card

func spawn_item_loot(position: Vector2, item: CraftingItem, count: int = 1) -> void:
	if loot_scene == null: return
	var loot: LootPickup = loot_scene.instantiate()
	get_tree().current_scene.add_child(loot)
	loot.global_position = position
	loot.loot_type       = LootPickup.LootType.ITEM
	loot.item            = item
	loot.item_count      = count

func spawn_zeny_loot(position: Vector2, amount: int) -> void:
	if loot_scene == null: return
	var loot: LootPickup = loot_scene.instantiate()
	get_tree().current_scene.add_child(loot)
	loot.global_position = position
	loot.loot_type       = LootPickup.LootType.ZENY
	loot.zeny_amount     = amount

# ── Zeny ───────────────────────────────────────────────────────────────────────

func add_zeny(amount: int) -> void:
	_zeny += amount
	zeny_changed.emit(_zeny)

func spend_zeny(amount: int) -> bool:
	if _zeny < amount: return false
	_zeny -= amount
	zeny_changed.emit(_zeny)
	return true

func get_zeny() -> int:
	return _zeny

# ── Scene transitions ──────────────────────────────────────────────────────────

func warp_to_scene(scene_path: String, spawn_id: String = "") -> void:
	pending_spawn_id = spawn_id
	_save_if_needed()
	get_tree().change_scene_to_file(scene_path)

# ── Save ───────────────────────────────────────────────────────────────────────

func _save_if_needed() -> void:
	if not persist_skills or _player == null: return
	var skills: SkillSystem  = _player.get_node_or_null("SkillSystem")
	var inv: ItemInventory   = _player.get_node_or_null("ItemInventory")
	if skills:
		SkillPersistence.save(skills, inv)

# ── Auto-wire player when spawned in scene ─────────────────────────────────────

func _on_node_added(node: Node) -> void:
	if node is PlayerController and _player == null:
		_player = node
