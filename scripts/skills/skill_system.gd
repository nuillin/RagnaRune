## skill_system.gd
## RuneScape-style skill system. Add as child Node of Player.
## Each skill gains XP by use; level derived from cumulative XP.
class_name SkillSystem
extends Node

# ── Signals ────────────────────────────────────────────────────────────────────
signal level_up(skill_type: int, new_level: int)
signal xp_gained(skill_type: int, amount: int)
signal skills_restored  ## emitted after loading from save

# ── Skill type enum ────────────────────────────────────────────────────────────
enum Type {
	## Combat
	ATTACK, STRENGTH, DEFENSE, HITPOINTS, MAGIC, RANGED,
	## Gathering / crafting
	MINING, FISHING, WOODCUTTING, CRAFTING, SMITHING, COOKING,
}

# ── Inner Skill class ──────────────────────────────────────────────────────────
class Skill:
	var type: int
	var level: int   = 1
	var total_xp: int = 0

	const MAX_LEVEL := ExperienceCurve.DEFAULT_MAX_LEVEL

	static var _table: Array[int] = []

	static func _get_table() -> Array[int]:
		if _table.is_empty():
			_table = ExperienceCurve.build_min_total_xp_per_level(MAX_LEVEL)
		return _table

	func xp_for_level(lvl: int) -> int:
		return _get_table()[clampi(lvl, 0, MAX_LEVEL)]

	func xp_to_next() -> int:
		return ExperienceCurve.xp_to_next_level(total_xp, level, _get_table(), MAX_LEVEL)

	func level_progress() -> float:
		return ExperienceCurve.level_progress(total_xp, level, _get_table(), MAX_LEVEL)

	## Returns true on level-up.
	func add_xp(amount: int) -> bool:
		if level >= MAX_LEVEL: return false
		var prev := level
		total_xp += amount
		level = mini(ExperienceCurve.level_from_total_xp(total_xp, _get_table(), MAX_LEVEL), MAX_LEVEL)
		return level > prev

	func set_from_save(xp: int) -> void:
		total_xp = maxi(0, xp)
		level    = mini(ExperienceCurve.level_from_total_xp(total_xp, _get_table(), MAX_LEVEL), MAX_LEVEL)

# ── Internal skill dictionary ──────────────────────────────────────────────────
var _skills: Dictionary = {}  # int(Type) -> Skill

func _ready() -> void:
	for t in Type.values():
		var s := Skill.new()
		s.type   = t
		_skills[t] = s

func get_skill(type: int) -> Skill:
	return _skills[type]

func get_level(type: int) -> int:
	return _skills[type].level

# ── XP award ──────────────────────────────────────────────────────────────────

func award_xp(type: int, amount: int) -> void:
	if amount <= 0: return
	var skill: Skill = _skills[type]
	xp_gained.emit(type, amount)
	if skill.add_xp(amount):
		level_up.emit(type, skill.level)
		print("[SkillSystem] ", Type.keys()[type], " → level ", skill.level)

# ── Combat XP helpers ──────────────────────────────────────────────────────────

func on_melee_hit(damage: int) -> void:
	award_xp(Type.ATTACK,    damage * 4)
	award_xp(Type.STRENGTH,  damage * 4)
	award_xp(Type.HITPOINTS, int(damage * 1.33))

func on_ranged_hit(damage: int) -> void:
	award_xp(Type.RANGED,    damage * 4)
	award_xp(Type.HITPOINTS, int(damage * 1.33))

func on_magic_hit(damage: int) -> void:
	award_xp(Type.MAGIC,     damage * 5)
	award_xp(Type.HITPOINTS, int(damage * 1.33))

func on_damage_taken(damage: int) -> void:
	award_xp(Type.DEFENSE,   damage * 3)
	award_xp(Type.HITPOINTS, damage)

# ── Stat bonuses ───────────────────────────────────────────────────────────────

func apply_skill_bonuses_to_stats(stats: CharacterStats) -> void:
	stats.bonus_atk    += get_level(Type.ATTACK)    / 5
	stats.bonus_atk    += get_level(Type.STRENGTH)  / 4
	stats.bonus_def    += get_level(Type.DEFENSE)   / 5
	stats.bonus_max_hp += get_level(Type.HITPOINTS) * 2
	stats.bonus_hit    += get_level(Type.RANGED)    / 5

# ── Save / Load ────────────────────────────────────────────────────────────────

func build_save_snapshot() -> Dictionary:
	var snap: Dictionary = {}
	for t: int in Type.values():
		snap[t] = _skills[t].total_xp
	return snap

func apply_save_snapshot(snap: Dictionary) -> void:
	for t: int in Type.values():
		if snap.has(t):
			_skills[t].set_from_save(snap[t])
	skills_restored.emit()
	var combat := get_parent().get_node_or_null("CombatManager")
	if combat:
		combat.rebuild_stats()

func debug_print_skills() -> void:
	for t: int in Type.values():
		var s: Skill = _skills[t]
		print("  ", Type.keys()[t], ": Lvl ", s.level,
			" | ", s.total_xp, " xp | ",
			"%.1f" % (s.level_progress() * 100), "%% to next")
