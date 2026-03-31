## damage_result.gd
## Value object returned by every CombatCalculator method.
class_name DamageResult
extends RefCounted

enum HitType {
	MISS,
	HIT,
	CRITICAL,
	PERFECT_DODGE,
}

var hit: int             = HitType.MISS
var raw_damage: int      = 0
var final_damage: int    = 0
var is_critical: bool    = false
var attack_element: int  = Element.Type.NEUTRAL
var element_multiplier: float = 1.0
var is_ranged: bool      = false

func landed() -> bool:
	return hit == HitType.HIT or hit == HitType.CRITICAL
