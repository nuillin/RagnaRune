## combat_manager.gd
## Drives the real-time auto-attack loop (RO-style).
## Add as a child Node of the Player scene.
## Requires sibling nodes: SkillSystem, CardSystem, StatusEffectSystem.
class_name CombatManager
extends Node

# ── Signals ────────────────────────────────────────────────────────────────────
signal damage_dealt(result: DamageResult)
signal damage_taken(result: DamageResult)
signal enemy_defeated(enemy: EnemyController)
signal player_defeated
signal state_changed(new_state: int)

enum State { IDLE, IN_COMBAT, PLAYER_DEAD }

# ── Exports ────────────────────────────────────────────────────────────────────
@export var player_stats: CharacterStats
@export var ranged_range: float = 8.0
@export var ranged_sp_cost: int = 5

# ── Child node refs (set in _ready via sibling paths) ─────────────────────────
var card_system: CardSystem
var skill_system: SkillSystem
var status_effects: StatusEffectSystem

# ── Runtime ────────────────────────────────────────────────────────────────────
var current_target: EnemyController = null
var _state: int = State.IDLE
var _attack_timer: float = 0.0

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	card_system    = get_parent().get_node_or_null("CardSystem")
	skill_system   = get_parent().get_node_or_null("SkillSystem")
	status_effects = get_parent().get_node_or_null("StatusEffectSystem")
	if player_stats == null:
		player_stats = CharacterStats.new()
	rebuild_stats()
	player_stats.full_restore()

func get_stats() -> CharacterStats:
	return player_stats

# ── Stat rebuild ───────────────────────────────────────────────────────────────

func rebuild_stats() -> void:
	if player_stats == null:
		player_stats = CharacterStats.new()
	player_stats.bonus_atk    = 0
	player_stats.bonus_def    = 0
	player_stats.bonus_max_hp = 0
	player_stats.bonus_aspd   = 0.0
	player_stats.bonus_hit    = 0
	player_stats.bonus_flee   = 0

	if skill_system:
		skill_system.apply_skill_bonuses_to_stats(player_stats)
	if card_system:
		card_system.apply_card_bonuses(player_stats)
	player_stats.recalc_derived()

# ── Targeting ──────────────────────────────────────────────────────────────────

func set_target(enemy: EnemyController) -> void:
	if enemy == null or not enemy.is_alive():
		clear_target()
		return
	current_target = enemy
	_attack_timer  = 0.0
	_set_state(State.IN_COMBAT)

func clear_target() -> void:
	current_target = null
	if _state == State.IN_COMBAT:
		_set_state(State.IDLE)

# ── Per-frame update ───────────────────────────────────────────────────────────

func _process(delta: float) -> void:
	if _state != State.IN_COMBAT:
		return
	if current_target == null or not current_target.is_alive():
		clear_target()
		return
	if _is_action_locked():
		return

	_attack_timer += delta
	var interval: float = 1.0 / player_stats.aspd
	if _attack_timer >= interval:
		_attack_timer -= interval
		_execute_auto_attack()

func _is_action_locked() -> bool:
	if status_effects == null:
		return false
	return (status_effects.has(StatusEffectSystem.Type.STUN)
		or  status_effects.has(StatusEffectSystem.Type.FREEZE)
		or  status_effects.has(StatusEffectSystem.Type.SLEEP))

# ── Auto-attack ────────────────────────────────────────────────────────────────

func _execute_auto_attack() -> void:
	var result := CombatCalculator.physical_damage(
		player_stats, current_target.stats, card_system)
	damage_dealt.emit(result)
	if not result.landed():
		return
	current_target.take_damage(result.final_damage)
	if skill_system:
		skill_system.on_melee_hit(result.final_damage)
	rebuild_stats()
	if not current_target.is_alive():
		_handle_enemy_death(current_target)

# ── Spells ─────────────────────────────────────────────────────────────────────

func cast_spell(spell_element: int, sp_cost: int = 10) -> void:
	if current_target == null or not current_target.is_alive():
		return
	if status_effects and status_effects.has(StatusEffectSystem.Type.SILENCE):
		push_warning("[Combat] Silenced — cannot cast.")
		return
	if not player_stats.consume_sp(sp_cost):
		push_warning("[Combat] Not enough SP!")
		return
	var result := CombatCalculator.magic_damage(player_stats, current_target.stats, spell_element)
	damage_dealt.emit(result)
	current_target.take_damage(result.final_damage)
	if skill_system:
		skill_system.on_magic_hit(result.final_damage)
	rebuild_stats()
	if not current_target.is_alive():
		_handle_enemy_death(current_target)

# ── Ranged ─────────────────────────────────────────────────────────────────────

func fire_ranged_attack() -> void:
	if current_target == null or not current_target.is_alive():
		return
	var dist: float = get_parent().global_position.distance_to(current_target.global_position)
	if dist > ranged_range:
		push_warning("[Combat] Target out of ranged range.")
		return
	if not player_stats.consume_sp(ranged_sp_cost):
		push_warning("[Combat] Not enough SP!")
		return
	var result := CombatCalculator.ranged_damage(player_stats, current_target.stats, card_system)
	damage_dealt.emit(result)
	if not result.landed():
		return
	current_target.take_damage(result.final_damage)
	if skill_system:
		skill_system.on_ranged_hit(result.final_damage)
	rebuild_stats()
	if not current_target.is_alive():
		_handle_enemy_death(current_target)

# ── Receive damage (called by EnemyController) ─────────────────────────────────

func receive_damage(result: DamageResult) -> void:
	if not result.landed():
		return
	var dmg := result.final_damage
	if status_effects and status_effects.has(StatusEffectSystem.Type.SLEEP):
		dmg *= 2
		status_effects.remove(StatusEffectSystem.Type.SLEEP)
	player_stats.take_damage(dmg)
	if skill_system:
		skill_system.on_damage_taken(dmg)
	rebuild_stats()
	damage_taken.emit(result)
	if not player_stats.is_alive():
		_handle_player_death()

# ── Death handling ─────────────────────────────────────────────────────────────

func _handle_enemy_death(enemy: EnemyController) -> void:
	var xp := CombatCalculator.calculate_kill_xp(enemy.stats, player_stats.base_level)
	if skill_system:
		skill_system.award_xp(SkillSystem.Type.ATTACK,    xp)
		skill_system.award_xp(SkillSystem.Type.HITPOINTS, xp / 3)
	enemy_defeated.emit(enemy)
	clear_target()

func _handle_player_death() -> void:
	if status_effects:
		status_effects.clear_all()
	_set_state(State.PLAYER_DEAD)
	player_defeated.emit()

func _set_state(new_state: int) -> void:
	_state = new_state
	state_changed.emit(new_state)

func get_state() -> int:
	return _state

func respawn() -> void:
	player_stats.full_restore()
	_set_state(State.IDLE)
