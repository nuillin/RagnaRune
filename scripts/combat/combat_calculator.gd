## combat_calculator.gd
## Pure-static combat math. No Node, no state.
## Mirrors RO's published formulas as closely as practical.
class_name CombatCalculator
extends RefCounted

# ── Hit / Miss ─────────────────────────────────────────────────────────────────

## RO formula: hit chance = (attacker.hit - defender.flee) + 80%.
## Criticals bypass flee entirely.
static func roll_hit(attacker: CharacterStats, defender: CharacterStats) -> int:
	var crit_chance: int = clampi(attacker.crit, 1, 100)
	if randi() % 100 < crit_chance:
		return DamageResult.HitType.CRITICAL

	var perfect_dodge: int = maxi(1, defender.LUK / 10)
	if randi() % 100 < perfect_dodge:
		return DamageResult.HitType.PERFECT_DODGE

	var hit_chance: int = clampi((attacker.hit - defender.flee) + 80, 5, 95)
	if randi() % 100 < hit_chance:
		return DamageResult.HitType.HIT
	return DamageResult.HitType.MISS

# ── Physical Melee ─────────────────────────────────────────────────────────────

static func physical_damage(
		attacker: CharacterStats,
		defender: CharacterStats,
		attacker_cards = null) -> DamageResult:

	var result := DamageResult.new()
	result.hit = roll_hit(attacker, defender)
	if not result.landed():
		return result

	result.is_critical   = result.hit == DamageResult.HitType.CRITICAL
	result.attack_element = attacker.weapon_element

	var base_atk: int = attacker.atk
	if attacker_cards:
		base_atk += attacker_cards.get_bonus_damage_vs_size(defender.size)

	var ignore_pct: int  = attacker_cards.get_ignore_def_percent() if attacker_cards else 0
	var eff_def: int     = defender.def * (100 - ignore_pct) / 100
	var raw_dmg: int     = maxi(1, base_atk - eff_def)
	raw_dmg = roundi(raw_dmg * randf_range(0.9, 1.1))

	result.element_multiplier = Element.get_multiplier(attacker.weapon_element, defender.body_element)
	var dmg: int = roundi(raw_dmg * result.element_multiplier)

	if result.is_critical:
		dmg = roundi(base_atk * 1.4 * result.element_multiplier)

	result.raw_damage   = raw_dmg
	result.final_damage = maxi(1, dmg)
	return result

# ── Ranged Physical ────────────────────────────────────────────────────────────

## DEX-dominant ranged formula. Uses BonusRanged card bonuses.
static func ranged_damage(
		attacker: CharacterStats,
		defender: CharacterStats,
		attacker_cards = null) -> DamageResult:

	var result := DamageResult.new()
	result.is_ranged     = true
	result.hit           = roll_hit(attacker, defender)
	if not result.landed():
		return result

	result.is_critical   = result.hit == DamageResult.HitType.CRITICAL
	result.attack_element = attacker.weapon_element

	var ranged_atk: int = (attacker.DEX + attacker.DEX * attacker.DEX / 10
						+ attacker.STR / 5 + attacker.LUK / 10)
	if attacker_cards:
		ranged_atk += attacker_cards.get_bonus_ranged_atk()
		ranged_atk += attacker_cards.get_bonus_damage_vs_size(defender.size)

	var raw_dmg: int = maxi(1, ranged_atk - defender.def)
	raw_dmg = roundi(raw_dmg * randf_range(0.85, 1.15))

	result.element_multiplier = Element.get_multiplier(attacker.weapon_element, defender.body_element)
	var dmg: int = roundi(raw_dmg * result.element_multiplier)

	if result.is_critical:
		dmg = roundi(ranged_atk * 1.4 * result.element_multiplier)

	result.raw_damage   = raw_dmg
	result.final_damage = maxi(1, dmg)
	return result

# ── Magic ──────────────────────────────────────────────────────────────────────

static func magic_damage(
		attacker: CharacterStats,
		defender: CharacterStats,
		spell_element: int = Element.Type.NEUTRAL) -> DamageResult:

	var result := DamageResult.new()
	result.attack_element = spell_element
	result.hit            = DamageResult.HitType.HIT  # magic always lands

	var raw_dmg: int = maxi(1, attacker.matk - defender.mdef)
	raw_dmg = roundi(raw_dmg * randf_range(0.85, 1.15))

	result.element_multiplier = Element.get_multiplier(spell_element, defender.body_element)
	result.raw_damage         = raw_dmg
	result.final_damage       = maxi(1, roundi(raw_dmg * result.element_multiplier))
	return result

# ── XP ────────────────────────────────────────────────────────────────────────

static func calculate_kill_xp(enemy_stats: CharacterStats, player_level: int) -> int:
	var scale: float = clampf(1.0 + (enemy_stats.base_level - player_level) * 0.1, 0.5, 2.0)
	return int(enemy_stats.base_level * 10 * scale)
