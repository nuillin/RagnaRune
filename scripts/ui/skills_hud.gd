## skills_hud.gd
## Compact skills panel. Subscribes to SkillSystem signals.
##
## Scene tree (PanelContainer or VBoxContainer):
##   SkillsHUD
##   ├─ LevelUpBanner  (Label, anchored top-centre, initially hidden)
##   └─ RowParent      (VBoxContainer — rows injected here at runtime)
class_name SkillsHUD
extends Control

@export var row_prefab: PackedScene
@export var level_up_visible_seconds: float  = 2.5
@export var xp_flash_color: Color            = Color(1.0, 0.92, 0.4, 0.45)

@onready var level_up_banner: Label = $LevelUpBanner
@onready var row_parent: VBoxContainer = $RowParent

var _skills: SkillSystem = null
var _rows: Dictionary    = {}   # int(SkillType) -> SkillRowView
var _banner_tween: Tween = null

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	if level_up_banner: level_up_banner.modulate.a = 0.0
	# Auto-find SkillSystem one frame after scene load
	await get_tree().process_frame
	var player := get_tree().get_first_node_in_group("player")
	if player:
		var ss: SkillSystem = player.get_node_or_null("SkillSystem")
		if ss: bind(ss)

func bind(skill_system: SkillSystem) -> void:
	if _skills == skill_system: return
	_unbind()
	_skills = skill_system
	_skills.xp_gained.connect(_on_xp_gained)
	_skills.level_up.connect(_on_level_up)
	_skills.skills_restored.connect(_on_skills_restored)
	_build_rows()

func _unbind() -> void:
	if _skills == null: return
	_skills.xp_gained.disconnect(_on_xp_gained)
	_skills.level_up.disconnect(_on_level_up)
	_skills.skills_restored.disconnect(_on_skills_restored)
	_skills = null

# ── Row construction ───────────────────────────────────────────────────────────

func _build_rows() -> void:
	if row_prefab == null or row_parent == null or _skills == null: return
	for child in row_parent.get_children(): child.queue_free()
	_rows.clear()
	for type in SkillSystem.Type.values():
		var row: SkillRowView = row_prefab.instantiate()
		row_parent.add_child(row)
		row.initialise(type)
		row.refresh(_skills.get_skill(type))
		_rows[type] = row

# ── Signal handlers ────────────────────────────────────────────────────────────

func _on_xp_gained(type: int, _amount: int) -> void:
	if not _rows.has(type) or _skills == null: return
	var row: SkillRowView = _rows[type]
	row.refresh(_skills.get_skill(type))
	row.flash(xp_flash_color)

func _on_level_up(type: int, new_level: int) -> void:
	if not _rows.has(type) or _skills == null: return
	_rows[type].refresh(_skills.get_skill(type))
	_show_banner("%s level up! → %d" % [SkillSystem.Type.keys()[type], new_level])

func _on_skills_restored() -> void:
	if _skills == null: return
	for type in _rows:
		_rows[type].refresh(_skills.get_skill(type))

# ── Level-up banner ────────────────────────────────────────────────────────────

func _show_banner(message: String) -> void:
	if level_up_banner == null: return
	if _banner_tween: _banner_tween.kill()
	level_up_banner.text       = message
	level_up_banner.modulate.a = 1.0
	level_up_banner.visible    = true
	_banner_tween = create_tween()
	_banner_tween.tween_interval(level_up_visible_seconds)
	_banner_tween.tween_property(level_up_banner, "modulate:a", 0.0, 0.5)
	_banner_tween.tween_callback(func(): level_up_banner.visible = false)

func _exit_tree() -> void:
	_unbind()
