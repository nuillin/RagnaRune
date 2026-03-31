## card_data.gd
## Defines a monster drop card.
## Create via: FileSystem → right-click → New Resource → CardData
class_name CardData
extends Resource

enum Rarity { COMMON, UNCOMMON, RARE, UNIQUE }

enum Effect {
	BONUS_ATK, BONUS_DEF, BONUS_MAX_HP, BONUS_ASPD,
	BONUS_HIT, BONUS_FLEE, BONUS_CRIT,
	ELEMENT_WEAPON, ELEMENT_BODY,
	BONUS_VS_SMALL, BONUS_VS_MEDIUM, BONUS_VS_LARGE, BONUS_VS_ELEMENT,
	SP_REGEN, HP_REGEN,
	IGNORE_DEF_PERCENT,
	RESIST_STATUS,
	BONUS_RANGED,
}

@export_group("Identity")
@export var card_name: String = ""
@export_multiline var description: String = ""
@export var icon: Texture2D
@export var rarity: Rarity = Rarity.COMMON

@export_group("Drop Source")
@export var monster_name: String = ""
@export_range(0.0, 1.0) var drop_rate: float = 0.01

@export_group("Primary Effect")
@export var effect: Effect = Effect.BONUS_ATK
@export var effect_value: int = 10
@export var target_element: int = Element.Type.NEUTRAL

@export_group("Secondary Effect")
@export var has_secondary_effect: bool = false
@export var secondary_effect: Effect = Effect.BONUS_ATK
@export var secondary_value: int = 0

@export_group("Socket Restriction")
## Bitmask: bit 0 = Weapon, bit 1 = Armor, bit 2 = Accessory
@export_flags("Weapon", "Armor", "Accessory") var allowed_slot_mask: int = 0b111

# ── Built-in presets ───────────────────────────────────────────────────────────

static func make_poring() -> CardData:
	var c := CardData.new()
	c.card_name          = "Poring Card"
	c.monster_name       = "Poring"
	c.drop_rate          = 0.01
	c.rarity             = Rarity.COMMON
	c.effect             = Effect.BONUS_MAX_HP
	c.effect_value       = 50
	c.description        = "Accessory slot. Max HP +50."
	c.allowed_slot_mask  = 0b100
	return c

static func make_swordfish() -> CardData:
	var c := CardData.new()
	c.card_name          = "Swordfish Card"
	c.monster_name       = "Swordfish"
	c.drop_rate          = 0.05
	c.rarity             = Rarity.COMMON
	c.effect             = Effect.ELEMENT_WEAPON
	c.target_element     = Element.Type.WATER
	c.description        = "Weapon slot. Adds Water element to weapon."
	c.allowed_slot_mask  = 0b001
	return c

static func make_orc_lord() -> CardData:
	var c := CardData.new()
	c.card_name            = "Orc Lord Card"
	c.monster_name         = "Orc Lord"
	c.drop_rate            = 0.002
	c.rarity               = Rarity.RARE
	c.effect               = Effect.BONUS_ATK
	c.effect_value         = 25
	c.has_secondary_effect = true
	c.secondary_effect     = Effect.IGNORE_DEF_PERCENT
	c.secondary_value      = 20
	c.description          = "Weapon slot. ATK +25, ignore 20%% DEF."
	c.allowed_slot_mask    = 0b001
	return c

static func make_skeleton_worker() -> CardData:
	var c := CardData.new()
	c.card_name         = "Skeleton Worker Card"
	c.monster_name      = "Skeleton Worker"
	c.drop_rate         = 0.01
	c.rarity            = Rarity.COMMON
	c.effect            = Effect.BONUS_VS_LARGE
	c.effect_value      = 15
	c.description       = "Weapon slot. +15%% damage vs Large monsters."
	c.allowed_slot_mask = 0b001
	return c

static func make_hunter_fly() -> CardData:
	var c := CardData.new()
	c.card_name            = "Hunter Fly Card"
	c.monster_name         = "Hunter Fly"
	c.drop_rate            = 0.01
	c.rarity               = Rarity.UNCOMMON
	c.effect               = Effect.BONUS_RANGED
	c.effect_value         = 10
	c.has_secondary_effect = true
	c.secondary_effect     = Effect.BONUS_ASPD
	c.secondary_value      = 5
	c.description          = "Weapon slot. Ranged ATK +10, ASPD +5%%."
	c.allowed_slot_mask    = 0b001
	return c
