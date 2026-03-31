## shop_npc.gd
## RO-style NPC shop. Walk into the Area2D and press "talk" (F) to open.
##
## Scene tree:
##   ShopNPC (Area2D)
##   ├─ CollisionShape2D
##   └─ Sprite2D
##
## To show the shop UI, connect the shop_opened signal to your ShopUI node.
class_name ShopNPC
extends Area2D

signal shop_opened(npc: ShopNPC)
signal shop_closed
signal transaction_message(msg: String)

class ShopEntry:
	enum EntryType { CARD, ITEM }
	@export var entry_type: EntryType = EntryType.ITEM
	@export var card: CardData        = null
	@export var item: CraftingItem    = null
	@export var item_count: int       = 1
	@export var buy_price: int        = 0
	@export var sell_price: int       = 0

	func display_name() -> String:
		match entry_type:
			EntryType.CARD: return card.card_name if card else "?"
			EntryType.ITEM: return item.display_name if item else "?"
		return "?"

@export var shop_name: String    = "Tool Dealer"
@export var npc_dialog: String   = "Welcome! Take a look at my wares."
@export var stock: Array[ShopEntry] = []

var _player: PlayerController = null
var _is_open: bool = false

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _process(_delta: float) -> void:
	if _player == null: return
	if Input.is_action_just_pressed("talk"):
		if _is_open: close()
		else:        open()

func _on_body_entered(body: Node2D) -> void:
	if body is PlayerController: _player = body
func _on_body_exited(body: Node2D) -> void:
	if body == _player: _player = null; close()

# ── Open / Close ───────────────────────────────────────────────────────────────

func open() -> void:
	_is_open = true
	shop_opened.emit(self)
	print("[Shop] ", shop_name, ": ", npc_dialog)

func close() -> void:
	_is_open = false
	shop_closed.emit()

# ── Transactions ───────────────────────────────────────────────────────────────

func try_buy(entry: ShopEntry) -> bool:
	if _player == null: return false
	if not GameManager.spend_zeny(entry.buy_price):
		transaction_message.emit("Not enough Zeny.")
		return false
	_deliver_to_player(entry)
	transaction_message.emit("Bought %s for %d z." % [entry.display_name(), entry.buy_price])
	return true

func try_sell(entry: ShopEntry) -> bool:
	if _player == null: return false
	if entry.sell_price <= 0:
		transaction_message.emit("This item cannot be sold here.")
		return false
	if not _take_from_player(entry):
		transaction_message.emit("You don't have that item.")
		return false
	GameManager.add_zeny(entry.sell_price)
	transaction_message.emit("Sold %s for %d z." % [entry.display_name(), entry.sell_price])
	return true

func _deliver_to_player(entry: ShopEntry) -> void:
	match entry.entry_type:
		ShopEntry.EntryType.CARD:
			if entry.card:
				var cs: CardSystem = _player.get_node_or_null("CardSystem")
				if cs: cs.add_to_inventory(entry.card)
		ShopEntry.EntryType.ITEM:
			if entry.item:
				var inv: ItemInventory = _player.get_node_or_null("ItemInventory")
				if inv: inv.add(entry.item, entry.item_count)

func _take_from_player(entry: ShopEntry) -> bool:
	match entry.entry_type:
		ShopEntry.EntryType.ITEM:
			var inv: ItemInventory = _player.get_node_or_null("ItemInventory")
			return inv != null and inv.try_remove(entry.item, entry.item_count)
	return false
