## regen_system.gd
## Passive HP and SP regeneration. Mirrors RO natural regen.
##   - HP ticks every hp_tick_interval seconds (faster when still).
##   - SP ticks every sp_tick_interval seconds (faster when still).
##   - Both pause for combat_regen_delay seconds after taking damage.
## Add as a child Node of the Player scene alongside CombatManager.
class_name RegenSystem
extends Node

@export var hp_tick_interval: float  = 8.0
@export var hp_sit_multiplier: float = 2.5   ## Regen speed multiplier when standing still
@export var sp_tick_interval: float  = 6.0
@export var sp_sit_multiplier: float = 3.0
@export var combat_regen_delay: float = 5.0  ## Seconds after taking damage before regen resumes
@export var still_threshold: float   = 5.0   ## Speed (px/s) below which player is "sitting"

var _combat: CombatManager
var _body: CharacterBody2D
var _hp_timer: float = 0.0
var _sp_timer: float = 0.0
var _damage_cooldown: float = 0.0

func _ready() -> void:
	_combat = get_parent().get_node_or_null("CombatManager")
	_body   = get_parent() as CharacterBody2D
	if _combat:
		_combat.damage_taken.connect(_on_damage_taken)

func _process(delta: float) -> void:
	if _combat == null or _combat.player_stats == null:
		return
	if _combat.get_state() == CombatManager.State.PLAYER_DEAD:
		return

	_damage_cooldown -= delta
	if _damage_cooldown > 0.0:
		return

	var is_still: bool = (_body == null or _body.velocity.length() < still_threshold)
	var hp_interval := hp_tick_interval / hp_sit_multiplier if is_still else hp_tick_interval
	var sp_interval := sp_tick_interval / sp_sit_multiplier if is_still else sp_tick_interval

	var s := _combat.player_stats
	_hp_timer += delta
	_sp_timer += delta

	if _hp_timer >= hp_interval:
		_hp_timer = 0.0
		var heal := maxi(1, s.VIT / 5 + s.base_level / 10)
		s.current_hp = mini(s.current_hp + heal, s.max_hp)

	if _sp_timer >= sp_interval:
		_sp_timer = 0.0
		var sp_heal := maxi(1, s.INT / 6 + s.base_level / 12)
		s.current_sp = mini(s.current_sp + sp_heal, s.max_sp)

func _on_damage_taken(_result: DamageResult) -> void:
	_damage_cooldown = combat_regen_delay
