## enemy_data.gd
## Defines a monster type as a Resource.
## Create via: FileSystem → New Resource → EnemyData
class_name EnemyData
extends Resource

@export_group("Identity")
@export var enemy_name: String = "Poring"
@export var sprite_frames: SpriteFrames  ## AnimatedSprite2D frames

@export_group("Stats")
@export var base_stats: CharacterStats = CharacterStats.new()

@export_group("AI")
@export var aggro_range: float  = 4.0
@export var attack_range: float = 1.2
@export var wander_radius: float = 5.0
@export var move_speed: float   = 1.5

@export_group("Loot")
@export var base_zeny_drop: int = 5
@export var max_zeny_drop: int  = 20
@export var card_drops: Array[CardDropEntry] = []
@export var item_drops: Array[ItemDropEntry] = []

@export_group("Respawn")
@export var respawn_delay: float = 10.0


class CardDropEntry:
	@export var card: CardData
	@export_range(0.0, 1.0) var drop_rate: float = 0.01

class ItemDropEntry:
	@export var item: CraftingItem
	@export var count: int = 1
	@export_range(0.0, 1.0) var drop_rate: float = 0.3


# ── Built-in presets ───────────────────────────────────────────────────────────

static func make_poring() -> EnemyData:
	var d := EnemyData.new()
	d.enemy_name           = "Poring"
	d.base_stats           = CharacterStats.new()
	d.base_stats.STR       = 4;  d.base_stats.AGI = 4
	d.base_stats.VIT       = 5;  d.base_stats.INT = 0
	d.base_stats.DEX       = 4;  d.base_stats.LUK = 30
	d.base_stats.base_level = 1; d.base_stats.size = 1
	d.base_stats.body_element = Element.Type.WATER
	d.aggro_range    = 3.0
	d.move_speed     = 1.5
	d.base_zeny_drop = 5
	d.max_zeny_drop  = 20
	var drop := CardDropEntry.new()
	drop.card      = CardData.make_poring()
	drop.drop_rate = 0.01
	d.card_drops.append(drop)
	return d

static func make_swordfish() -> EnemyData:
	var d := EnemyData.new()
	d.enemy_name           = "Swordfish"
	d.base_stats           = CharacterStats.new()
	d.base_stats.STR       = 10; d.base_stats.AGI = 10
	d.base_stats.VIT       = 10; d.base_stats.INT = 0
	d.base_stats.DEX       = 10; d.base_stats.LUK = 5
	d.base_stats.base_level = 10; d.base_stats.size = 1
	d.base_stats.body_element = Element.Type.WATER
	d.aggro_range    = 6.0
	d.move_speed     = 2.5
	d.base_zeny_drop = 50
	d.max_zeny_drop  = 120
	var drop := CardDropEntry.new()
	drop.card      = CardData.make_swordfish()
	drop.drop_rate = 0.05
	d.card_drops.append(drop)
	return d
