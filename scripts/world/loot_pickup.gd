## loot_pickup.gd
## Ground loot node. Add as root of a scene with:
##   LootPickup (Area2D)
##   ├─ CollisionShape2D  (small circle, trigger)
##   └─ Sprite2D or AnimatedSprite2D
class_name LootPickup
extends Area2D

enum LootType { ZENY, CARD, ITEM }

@export var loot_type: LootType = LootType.ZENY
@export var zeny_amount: int    = 0
@export var card: CardData      = null
@export var item: CraftingItem  = null
@export var item_count: int     = 1
@export var lifetime_seconds: float = 60.0
@export var vacuum_range: float     = 40.0  ## Auto-collect when player within this distance (0 = off)

var _player: PlayerController = null
var _picked_up: bool = false

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
	get_tree().create_timer(lifetime_seconds).timeout.connect(queue_free)

func _process(_delta: float) -> void:
	if _picked_up or _player == null: return
	if vacuum_range > 0.0:
		if global_position.distance_to(_player.global_position) <= vacuum_range:
			_collect()
			return
	if Input.is_action_just_pressed("interact"):
		_collect()

func _on_body_entered(body: Node2D) -> void:
	if body is PlayerController:
		_player = body

func _on_body_exited(body: Node2D) -> void:
	if body == _player:
		_player = null

func _collect() -> void:
	if _picked_up or _player == null: return
	_picked_up = true
	match loot_type:
		LootType.ZENY:
			GameManager.add_zeny(zeny_amount)
			print("[Loot] Picked up ", zeny_amount, " Zeny")
		LootType.CARD:
			if card:
				var card_sys: CardSystem = _player.get_node_or_null("CardSystem")
				if card_sys: card_sys.add_to_inventory(card)
				print("[Loot] Picked up ", card.card_name)
		LootType.ITEM:
			if item:
				var inv: ItemInventory = _player.get_node_or_null("ItemInventory")
				if inv: inv.add(item, item_count)
				print("[Loot] Picked up ", item.display_name, " ×", item_count)
	queue_free()
