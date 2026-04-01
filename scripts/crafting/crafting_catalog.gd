## crafting_catalog.gd
## Maps saved item IDs back to CraftingItem Resource instances at load time.
## Create one per project: FileSystem → New Resource → CraftingCatalog.
class_name CraftingCatalog
extends Resource

@export var items: Array = []

var _by_key: Dictionary = {}
var _built: bool = false

func rebuild_if_needed() -> void:
	if _built: return
	_by_key.clear()
	for item in items:
		if item == null: continue
		if not item.item_id.is_empty():
			_by_key.get_or_add(item.item_id, item)
		_by_key.get_or_add(item.resource_name, item)
	_built = true

## Resolve by item_id first, then by resource_name.
func resolve(id_or_name: String) -> CraftingItem:
	if id_or_name.is_empty(): return null
	rebuild_if_needed()
	if _by_key.has(id_or_name):
		return _by_key[id_or_name]
	for item in items:
		if item != null and item.resource_name == id_or_name:
			return item
	return null
