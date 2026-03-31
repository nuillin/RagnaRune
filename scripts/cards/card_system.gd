## card_system.gd
## Card inventory and socket equipment system. Add as child Node of Player.
class_name CardSystem
extends Node

signal cards_changed

# ── Equipment slot inner class ─────────────────────────────────────────────────

class EquipmentSlot:
	var slot_name: String
	var slot_mask: int          ## bitmask matching CardData.allowed_slot_mask
	var sockets: int
	var cards: Array[CardData]  ## socketed cards

	func _init(name: String, mask: int, socket_count: int = 1) -> void:
		slot_name = name
		slot_mask = mask
		sockets   = socket_count
		cards     = []
		cards.resize(socket_count)

	func try_insert(card: CardData, index: int) -> bool:
		if index < 0 or index >= sockets: return false
		if cards[index] != null: return false
		if (card.allowed_slot_mask & slot_mask) == 0: return false
		cards[index] = card
		return true

	func remove_card(index: int) -> CardData:
		if index < 0 or index >= sockets: return null
		var c: CardData = cards[index]
		cards[index] = null
		return c

# ── Slots ──────────────────────────────────────────────────────────────────────
var weapon_slot:    EquipmentSlot = EquipmentSlot.new("Weapon",    0b001, 4)
var armor_slot:     EquipmentSlot = EquipmentSlot.new("Armor",     0b010, 4)
var accessory_slot: EquipmentSlot = EquipmentSlot.new("Accessory", 0b100, 2)

var card_inventory: Array[CardData] = []

var _all_slots: Array

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	_all_slots = [weapon_slot, armor_slot, accessory_slot]

	## Seed starter cards in editor/dev builds only.
	if OS.is_debug_build():
		card_inventory.append(CardData.make_poring())
		card_inventory.append(CardData.make_swordfish())
		card_inventory.append(CardData.make_orc_lord())
		card_inventory.append(CardData.make_hunter_fly())

# ── Equipping ──────────────────────────────────────────────────────────────────

func equip_card(card: CardData, slot: EquipmentSlot, socket_index: int) -> bool:
	if not card_inventory.has(card): return false
	if not slot.try_insert(card, socket_index): return false
	card_inventory.erase(card)
	cards_changed.emit()
	return true

func unequip_card(slot: EquipmentSlot, socket_index: int) -> void:
	var card := slot.remove_card(socket_index)
	if card:
		card_inventory.append(card)
		cards_changed.emit()

func add_to_inventory(card: CardData) -> void:
	card_inventory.append(card)
	print("[CardSystem] Picked up: ", card.card_name)
	cards_changed.emit()

# ── Stat application ───────────────────────────────────────────────────────────

func apply_card_bonuses(stats: CharacterStats) -> void:
	for slot in _all_slots:
		for card: CardData in slot.cards:
			if card == null: continue
			_apply_effect(card.effect, card.effect_value, card.target_element, stats)
			if card.has_secondary_effect:
				_apply_effect(card.secondary_effect, card.secondary_value, card.target_element, stats)

func _apply_effect(effect: int, value: int, target_element: int, stats: CharacterStats) -> void:
	match effect:
		CardData.Effect.BONUS_ATK:       stats.bonus_atk    += value
		CardData.Effect.BONUS_DEF:       stats.bonus_def    += value
		CardData.Effect.BONUS_MAX_HP:    stats.bonus_max_hp += value
		CardData.Effect.BONUS_ASPD:      stats.bonus_aspd   += value * 0.01
		CardData.Effect.BONUS_HIT:       stats.bonus_hit    += value
		CardData.Effect.BONUS_FLEE:      stats.bonus_flee   += value
		CardData.Effect.ELEMENT_WEAPON:  stats.weapon_element = target_element
		CardData.Effect.ELEMENT_BODY:    stats.body_element   = target_element

# ── Per-hit bonus lookups (called by CombatCalculator) ─────────────────────────

func get_bonus_damage_vs_size(size: int) -> int:
	var bonus := 0
	for slot in _all_slots:
		for card: CardData in slot.cards:
			if card == null: continue
			match size:
				0: if card.effect == CardData.Effect.BONUS_VS_SMALL:  bonus += card.effect_value
				1: if card.effect == CardData.Effect.BONUS_VS_MEDIUM: bonus += card.effect_value
				2: if card.effect == CardData.Effect.BONUS_VS_LARGE:  bonus += card.effect_value
	return bonus

func get_ignore_def_percent() -> int:
	var pct := 0
	for slot in _all_slots:
		for card: CardData in slot.cards:
			if card == null: continue
			if card.effect == CardData.Effect.IGNORE_DEF_PERCENT:
				pct += card.effect_value
			if card.has_secondary_effect and card.secondary_effect == CardData.Effect.IGNORE_DEF_PERCENT:
				pct += card.secondary_value
	return mini(pct, 100)

func get_bonus_ranged_atk() -> int:
	var bonus := 0
	for slot in _all_slots:
		for card: CardData in slot.cards:
			if card == null: continue
			if card.effect == CardData.Effect.BONUS_RANGED: bonus += card.effect_value
			if card.has_secondary_effect and card.secondary_effect == CardData.Effect.BONUS_RANGED:
				bonus += card.secondary_value
	return bonus

func get_resist_status_percent() -> float:
	var resist := 0.0
	for slot in _all_slots:
		for card: CardData in slot.cards:
			if card == null: continue
			if card.effect == CardData.Effect.RESIST_STATUS:
				resist += card.effect_value
	return minf(resist, 90.0)
