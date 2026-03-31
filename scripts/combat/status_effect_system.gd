## status_effect_system.gd
## Manages active status effects on one character (player or enemy).
## Add as a child Node of PlayerController or EnemyController.
class_name StatusEffectSystem
extends Node

signal effect_applied(effect: StatusEffect)
signal effect_expired(type: int)

enum Type {
	# Debuffs
	POISON, STUN, FREEZE, SLEEP, SILENCE, BLIND, SLOW, CURSE, BLEEDING,
	# Buffs
	ENDURE, BLESS, AGI_UP,
}

## Inner value type for a single active effect.
class StatusEffect:
	var type: int
	var duration: float
	var tick_interval: float
	var tick_timer: float
	var power: float

	func is_debuff() -> bool:
		return type <= Type.BLEEDING

# ── State ──────────────────────────────────────────────────────────────────────
var _active: Dictionary = {}  # int(Type) -> StatusEffect

# ── Cached stat modifiers (read by CombatManager.rebuild_stats) ───────────────
var hit_penalty: int     = 0
var aspd_penalty: float  = 0.0
var flee_penalty: float  = 0.0
var str_penalty: int     = 0
var dex_penalty: int     = 0
var agi_penalty: int     = 0
var bless_bonus: int     = 0
var agi_aspd_bonus: float = 0.0
var agi_flee_bonus: int  = 0

# ──────────────────────────────────────────────────────────────────────────────

## Apply a status effect. Refreshes duration/power if already active.
func apply(type: int, duration: float, power: float = 0.0, tick_interval: float = 0.0) -> void:
	if _active.has(type):
		var e: StatusEffect = _active[type]
		e.duration = maxf(e.duration, duration)
		e.power    = maxf(e.power, power)
		return
	var e := StatusEffect.new()
	e.type          = type
	e.duration      = duration
	e.power         = power
	e.tick_interval = tick_interval
	e.tick_timer    = tick_interval
	_active[type]   = e
	_recalc_penalties()
	effect_applied.emit(e)

func remove(type: int) -> void:
	if _active.erase(type):
		_recalc_penalties()
		effect_expired.emit(type)

func has(type: int) -> bool:
	return _active.has(type)

func get_all() -> Array:
	return _active.values()

func clear_all() -> void:
	var types := _active.keys().duplicate()
	_active.clear()
	_recalc_penalties()
	for t in types:
		effect_expired.emit(t)

# ── Per-frame tick ─────────────────────────────────────────────────────────────

func _process(delta: float) -> void:
	if _active.is_empty():
		return
	var to_remove: Array[int] = []
	for type: int in _active:
		var e: StatusEffect = _active[type]
		e.duration -= delta
		if e.tick_interval > 0.0:
			e.tick_timer -= delta
			if e.tick_timer <= 0.0:
				e.tick_timer = e.tick_interval
				_process_tick(e)
		if e.duration <= 0.0:
			to_remove.append(type)
	for t in to_remove:
		remove(t)

func _process_tick(e: StatusEffect) -> void:
	var stats: CharacterStats = _get_stats()
	if stats == null:
		return
	match e.type:
		Type.POISON:
			# RO: ~1/12 max HP per tick, cannot kill
			var dmg := maxi(1, int(stats.max_hp * e.power / 100.0))
			stats.current_hp = maxi(1, stats.current_hp - dmg)
		Type.BLEEDING:
			var dmg := maxi(1, int(e.power))
			stats.current_hp = maxi(1, stats.current_hp - dmg)

func _get_stats() -> CharacterStats:
	var p = get_parent()
	if p.has_method("get_stats"):
		return p.get_stats()
	return null

func _recalc_penalties() -> void:
	hit_penalty    = 0;  aspd_penalty   = 0.0; flee_penalty  = 0.0
	str_penalty    = 0;  dex_penalty    = 0;   agi_penalty   = 0
	bless_bonus    = 0;  agi_aspd_bonus = 0.0; agi_flee_bonus = 0
	for type: int in _active:
		match type:
			Type.BLIND:  hit_penalty  += 50
			Type.SLOW:   aspd_penalty += 0.5; flee_penalty += 0.5
			Type.CURSE:  str_penalty = 50; dex_penalty = 50; agi_penalty = 50
			Type.BLESS:  bless_bonus  = 10
			Type.AGI_UP: agi_aspd_bonus = 0.3; agi_flee_bonus = 20

# ── Factory helpers ────────────────────────────────────────────────────────────

func apply_poison(duration: float = 10.0, pct_per_tick: float = 1.0) -> void:
	apply(Type.POISON,   duration, pct_per_tick, 1.0)
func apply_stun(duration: float = 2.0) -> void:
	apply(Type.STUN,     duration)
func apply_freeze(duration: float = 3.0) -> void:
	apply(Type.FREEZE,   duration)
func apply_sleep(duration: float = 10.0) -> void:
	apply(Type.SLEEP,    duration)
func apply_silence(duration: float = 8.0) -> void:
	apply(Type.SILENCE,  duration)
func apply_blind(duration: float = 10.0) -> void:
	apply(Type.BLIND,    duration)
func apply_slow(duration: float = 8.0) -> void:
	apply(Type.SLOW,     duration)
func apply_curse(duration: float = 15.0) -> void:
	apply(Type.CURSE,    duration)
func apply_bleeding(duration: float = 15.0, dmg_per_tick: float = 5.0) -> void:
	apply(Type.BLEEDING, duration, dmg_per_tick, 2.0)
