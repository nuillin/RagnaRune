## character_stats.gd
## Shared stat block for player and enemies. Mirrors the RO formula set.
## Use as a Resource so it can be embedded in EnemyData ScriptableObjects.
class_name CharacterStats
extends Resource

@export_group("Base Stats")
@export var STR: int = 1  ## Physical damage
@export var AGI: int = 1  ## ASPD, Flee
@export var VIT: int = 1  ## Max HP, DEF
@export var INT: int = 1  ## Max SP, MATK, MDEF
@export var DEX: int = 1  ## HIT, ranged ATK, cast time
@export var LUK: int = 1  ## Crit, perfect dodge, status resist

@export_group("Level")
@export var base_level: int = 1

@export_group("Element")
@export var body_element: int = Element.Type.NEUTRAL
@export var weapon_element: int = Element.Type.NEUTRAL

@export_group("Size")
@export_range(0, 2) var size: int = 1  ## 0=Small, 1=Medium, 2=Large

# ── Bonus stats (zeroed before each recalc, then filled by skills/cards) ──────
var bonus_atk: int = 0
var bonus_def: int = 0
var bonus_hit: int = 0
var bonus_flee: int = 0
var bonus_max_hp: int = 0
var bonus_aspd: float = 0.0

# ── Derived stats (computed by recalc_derived) ─────────────────────────────────
var max_hp: int = 0
var current_hp: int = 0
var max_sp: int = 0
var current_sp: int = 0
var atk: int = 0
var matk: int = 0
var def: int = 0
var mdef: int = 0
var hit: int = 0
var flee: int = 0
var crit: int = 0
var aspd: float = 1.0

# ──────────────────────────────────────────────────────────────────────────────

func recalc_derived() -> void:
	max_hp = 35 + VIT * 5 + VIT * VIT / 10 + base_level * 3 + bonus_max_hp
	max_hp = maxi(max_hp, 1)

	max_sp = 10 + INT * 3 + base_level * 2
	max_sp = maxi(max_sp, 1)

	atk  = STR + STR * STR / 10 + DEX / 5 + LUK / 5 + bonus_atk
	matk = INT + INT * INT / 7
	def  = VIT / 2 + bonus_def
	mdef = INT / 5
	hit  = base_level + DEX + bonus_hit
	flee = base_level + AGI + bonus_flee
	crit = maxi(1, LUK / 3)
	aspd = clampf(1.0 + AGI * 0.01 + DEX * 0.005 + bonus_aspd, 0.5, 3.0)

func is_alive() -> bool:
	return current_hp > 0

func full_restore() -> void:
	current_hp = max_hp
	current_sp = max_sp

func take_damage(amount: int) -> void:
	current_hp = clampi(current_hp - amount, 0, max_hp)

func consume_sp(cost: int) -> bool:
	if current_sp < cost:
		return false
	current_sp -= cost
	return true

## Deep-copy this stat block so the source Resource isn't mutated at runtime.
func duplicate_runtime() -> CharacterStats:
	var c := CharacterStats.new()
	c.STR = STR; c.AGI = AGI; c.VIT = VIT
	c.INT = INT; c.DEX = DEX; c.LUK = LUK
	c.base_level    = base_level
	c.body_element  = body_element
	c.weapon_element = weapon_element
	c.size          = size
	return c
