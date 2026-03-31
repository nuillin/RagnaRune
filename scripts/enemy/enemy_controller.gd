## enemy_controller.gd
## Enemy scene root (CharacterBody2D).
## Scene tree:
##   EnemyController (CharacterBody2D)
##   ├─ NavigationAgent2D
##   ├─ CollisionShape2D
##   ├─ AnimatedSprite2D
##   ├─ StatusEffectSystem
##   └─ Area2D  (aggro detection)
##        └─ CollisionShape2D
class_name EnemyController
extends CharacterBody2D

signal died(enemy: EnemyController)
signal damage_taken_sig(amount: int)

enum AIState { IDLE, WANDERING, CHASING, ATTACKING, DEAD }

# ── Exports ────────────────────────────────────────────────────────────────────
@export var data: EnemyData

# ── Runtime stats (deep-copied from data.base_stats on initialise) ─────────────
var stats: CharacterStats = null
var ai_state: int = AIState.IDLE

# ── Child node references ──────────────────────────────────────────────────────
@onready var nav_agent: NavigationAgent2D = $NavigationAgent2D
@onready var anim: AnimatedSprite2D        = $AnimatedSprite2D
@onready var status_effects: StatusEffectSystem = $StatusEffectSystem

# ── Private ────────────────────────────────────────────────────────────────────
var _player_transform: Node2D = null
var _player_combat: CombatManager = null
var _attack_timer: float = 0.0
var _wander_timer: float = 0.0
var _is_initialised: bool = false

# ──────────────────────────────────────────────────────────────────────────────

func initialise(enemy_data: EnemyData, player: Node2D, player_combat: CombatManager) -> void:
	data             = enemy_data
	_player_transform = player
	_player_combat    = player_combat

	stats = enemy_data.base_stats.duplicate_runtime()
	stats.recalc_derived()
	stats.full_restore()

	nav_agent.max_speed = enemy_data.move_speed
	if enemy_data.sprite_frames:
		anim.sprite_frames = enemy_data.sprite_frames

	_is_initialised = true

func get_stats() -> CharacterStats:
	return stats

func is_alive() -> bool:
	return stats != null and stats.is_alive()

# ── Per-frame ──────────────────────────────────────────────────────────────────

func _physics_process(delta: float) -> void:
	if not _is_initialised or not is_alive():
		return

	# Status effects can lock movement / attacks
	var is_locked: bool = (status_effects.has(StatusEffectSystem.Type.STUN)
		or status_effects.has(StatusEffectSystem.Type.FREEZE)
		or status_effects.has(StatusEffectSystem.Type.SLEEP))

	var dist: float = INF
	if _player_transform:
		dist = global_position.distance_to(_player_transform.global_position)

	match ai_state:
		AIState.IDLE:      _update_idle(dist, delta)
		AIState.WANDERING: _update_wandering(dist, delta)
		AIState.CHASING:   _update_chasing(dist, delta)
		AIState.ATTACKING: if not is_locked: _update_attacking(dist, delta)

	if not is_locked and ai_state in [AIState.WANDERING, AIState.CHASING]:
		velocity = nav_agent.get_next_path_position() - global_position
		if velocity.length() > 0.1:
			velocity = velocity.normalized() * data.move_speed
		move_and_slide()
	elif ai_state == AIState.IDLE or ai_state == AIState.ATTACKING:
		velocity = Vector2.ZERO
		move_and_slide()

# ── AI states ──────────────────────────────────────────────────────────────────

func _update_idle(dist: float, delta: float) -> void:
	_wander_timer += delta
	if _wander_timer > randf_range(3.0, 7.0):
		_set_ai_state(AIState.WANDERING)
		_wander_timer = 0.0
	if dist < data.aggro_range:
		_set_ai_state(AIState.CHASING)

func _update_wandering(dist: float, delta: float) -> void:
	if dist < data.aggro_range:
		_set_ai_state(AIState.CHASING)
		return
	if nav_agent.is_navigation_finished():
		var rand := Vector2(randf_range(-1.0, 1.0), randf_range(-1.0, 1.0)).normalized()
		var target := global_position + rand * data.wander_radius
		nav_agent.target_position = target
		_wander_timer += delta
		if _wander_timer > 10.0:
			_set_ai_state(AIState.IDLE)

func _update_chasing(dist: float, _delta: float) -> void:
	if dist > data.aggro_range * 1.5:
		_set_ai_state(AIState.IDLE)
		return
	if dist <= data.attack_range:
		_set_ai_state(AIState.ATTACKING)
		return
	if _player_transform:
		nav_agent.target_position = _player_transform.global_position

func _update_attacking(dist: float, delta: float) -> void:
	if dist > data.attack_range:
		_set_ai_state(AIState.CHASING)
		return
	_attack_timer += delta
	var interval: float = 1.0 / stats.aspd
	if _attack_timer >= interval:
		_attack_timer -= interval
		_execute_attack()

func _execute_attack() -> void:
	if _player_combat == null: return
	var result := CombatCalculator.physical_damage(stats, _player_combat.player_stats)
	_player_combat.receive_damage(result)

# ── Public API ─────────────────────────────────────────────────────────────────

func take_damage(amount: int) -> void:
	stats.take_damage(amount)
	damage_taken_sig.emit(amount)

	# Wake from sleep on hit
	if status_effects.has(StatusEffectSystem.Type.SLEEP):
		status_effects.remove(StatusEffectSystem.Type.SLEEP)

	# Thaw freeze with fire/wind (handled by caller, but break AI lock here)
	if not is_alive():
		_die()

# ── Death ──────────────────────────────────────────────────────────────────────

func _die() -> void:
	_set_ai_state(AIState.DEAD)
	velocity = Vector2.ZERO
	set_physics_process(false)
	died.emit(self)

	# Roll for card/item drops
	_roll_drops()

	# Visual death then free
	anim.play("death") if anim.sprite_frames and anim.sprite_frames.has_animation("death") else _queue_free_delayed()

func _roll_drops() -> void:
	if data == null: return
	for drop in data.card_drops:
		if randf() < drop.drop_rate:
			print("[Drop] ", data.enemy_name, " dropped: ", drop.card.card_name, "!")
			# GameManager handles spawning the LootPickup node
			GameManager.spawn_card_loot(global_position, drop.card)
	for drop in data.item_drops:
		if randf() < drop.drop_rate:
			GameManager.spawn_item_loot(global_position, drop.item, drop.count)

func _queue_free_delayed() -> void:
	await get_tree().create_timer(1.5).timeout
	queue_free()

func _on_animation_finished() -> void:
	if anim.animation == "death":
		await get_tree().create_timer(0.5).timeout
		queue_free()

# ── Helpers ────────────────────────────────────────────────────────────────────

func _set_ai_state(new_state: int) -> void:
	ai_state      = new_state
	_attack_timer = 0.0
	_wander_timer = 0.0
