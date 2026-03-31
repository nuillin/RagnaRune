## skill_persistence.gd
## Saves and loads SkillSystem + ItemInventory state as JSON under user://.
class_name SkillPersistence
extends RefCounted

const SAVE_FILE := "user://player_save.json"
const FORMAT_VERSION := 2

static func save(skill_system: SkillSystem, inventory = null, path: String = SAVE_FILE) -> void:
	if skill_system == null: return
	var data := {
		"format_version": FORMAT_VERSION,
		"skills": skill_system.build_save_snapshot(),
	}
	if inventory != null:
		data["items"] = inventory.build_save_entries()
	var json_str := JSON.stringify(data, "\t")
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_warning("[SkillPersistence] Cannot open save file for writing: ", path)
		return
	file.store_string(json_str)
	file.close()

static func try_load(skill_system: SkillSystem, inventory = null,
		catalog = null, path: String = SAVE_FILE) -> bool:
	if skill_system == null: return false
	if not FileAccess.file_exists(path): return false
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null: return false
	var text := file.get_as_text()
	file.close()
	var result = JSON.parse_string(text)
	if result == null or not result is Dictionary:
		push_warning("[SkillPersistence] Failed to parse save file.")
		return false
	var data: Dictionary = result
	# Restore skills
	if data.has("skills"):
		var snap: Dictionary = {}
		for key in data["skills"]:
			snap[int(key)] = int(data["skills"][key])
		skill_system.apply_save_snapshot(snap)
	# Restore inventory
	if inventory != null and data.has("items"):
		if catalog == null and not data["items"].is_empty():
			push_warning("[SkillPersistence] Items in save but no catalog assigned.")
		else:
			inventory.apply_save_entries(data["items"], catalog)
	return true

static func delete_save(path: String = SAVE_FILE) -> void:
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(path)
