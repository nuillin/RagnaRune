## crafting_item.gd
class_name CraftingItem
extends Resource

@export var item_id: String = ""      ## Stable id for saves; falls back to resource name.
@export var display_name: String = ""
@export var icon: Texture2D
