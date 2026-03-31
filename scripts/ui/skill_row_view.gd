## skill_row_view.gd
## One row in the SkillsHUD panel.
## Scene tree:
##   SkillRowView (HBoxContainer)
##   ├─ NameLabel    (Label, min_size_x ~110)
##   ├─ LevelLabel   (Label, min_size_x ~30)
##   ├─ ProgressBar  (ProgressBar, size_flags_h = Expand)
##   └─ HighlightBG  (ColorRect, full-size, mouse_filter = Ignore) [optional]
class_name SkillRowView
extends HBoxContainer

@onready var name_label: Label        = $NameLabel
@onready var level_label: Label       = $LevelLabel
@onready var xp_bar: ProgressBar      = $ProgressBar
@onready var highlight: ColorRect     = $HighlightBG  # may be null

var skill_type: int = 0

func initialise(type: int) -> void:
	skill_type = type
	if name_label: name_label.text = SkillSystem.Type.keys()[type]

func refresh(skill: SkillSystem.Skill) -> void:
	if level_label: level_label.text = str(skill.level)
	if xp_bar:      xp_bar.value     = skill.level_progress()

func flash(color: Color) -> void:
	if highlight == null: return
	highlight.color   = color
	highlight.visible = true
	var tween := create_tween()
	tween.tween_interval(0.12)
	tween.tween_property(highlight, "color:a", 0.0, 0.35)
	tween.tween_callback(func(): highlight.visible = false)
