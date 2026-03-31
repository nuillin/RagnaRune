## item_inventory.gd
## Stack-based inventory for CraftingItems. Add as child Node of Player.
class_name ItemInventory
extends Node

signal inventory_changed

class ItemStack:
	var item: CraftingItem
	var count: int

var _stacks: Array[ItemStack] = []

func get_stacks() -> Array[ItemStack]:
	return _stacks

func count_of(item: CraftingItem) -> int:
	var total := 0
	for s in _stacks:
		if s.item == item: total += s.count
	return total

func has_ingredients(ingredients: Array) -> bool:
	for ing in ingredients:
		if ing.item == null or ing.count <= 0: return false
		if count_of(ing.item) < ing.count: return false
	return true

func add(item: CraftingItem, count: int) -> void:
	if item == null or count == 0: return
	if count < 0:
		try_remove(item, -count)
		return
	for s in _stacks:
		if s.item == item:
			s.count += count
			inventory_changed.emit()
			return
	var s := ItemStack.new()
	s.item  = item
	s.count = count
	_stacks.append(s)
	inventory_changed.emit()

func try_remove(item: CraftingItem, count: int) -> bool:
	if item == null or count <= 0: return false
	if count_of(item) < count: return false
	var remaining := count
	for i in range(_stacks.size() - 1, -1, -1):
		var s := _stacks[i]
		if s.item != item: continue
		var take := mini(s.count, remaining)
		s.count  -= take
		remaining -= take
		if s.count <= 0: _stacks.remove_at(i)
		if remaining == 0: break
	inventory_changed.emit()
	return remaining == 0

func try_consume(ingredients: Array) -> bool:
	if not has_ingredients(ingredients): return false
	for ing in ingredients:
		var need := ing.count
		for s in _stacks:
			if s.item != ing.item: continue
			var take := mini(s.count, need)
			s.count -= take; need -= take
			if need == 0: break
	for i in range(_stacks.size() - 1, -1, -1):
		if _stacks[i].count <= 0: _stacks.remove_at(i)
	inventory_changed.emit()
	return true

func build_save_entries() -> Array:
	var out := []
	for s in _stacks:
		if s.item == null or s.count <= 0: continue
		var id: String = s.item.item_id if not s.item.item_id.is_empty() else s.item.resource_name
		out.append({"item_id": id, "count": s.count})
	return out

func apply_save_entries(entries: Array, catalog: CraftingCatalog) -> void:
	_stacks.clear()
	if entries == null or entries.is_empty():
		inventory_changed.emit(); return
	if catalog == null:
		push_warning("[ItemInventory] No catalog — cannot restore items.")
		inventory_changed.emit(); return
	for e in entries:
		if e["count"] <= 0: continue
		var item := catalog.resolve(e["item_id"])
		if item == null:
			push_warning("[ItemInventory] Unknown item: ", e["item_id"]); continue
		add(item, e["count"])
	inventory_changed.emit()
