## gathering_interactable.gd
## Stand in trigger, press "interact" (E), gain skill XP.
##
## Scene tree:
##   GatheringInteractable (Area2D)
##   ├─ CollisionShape2D
##   └─ Sprite2D
class_name GatheringInteractable
extends Area2D

@export var skill: int = SkillSystem.Type.MINING
@export var xp_reward: int = 25
@export var required_level: int = 1
@export var cooldown_seconds: float = 2.0
@export var node_label: String = "Rock"

var _player_skills: SkillSystem = null
var _cooldown_end: float = 0.0

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _process(_delta: float) -> void:
	if _player_skills == null: return
	if Time.get_ticks_msec() / 1000.0 < _cooldown_end: return
	if not Input.is_action_just_pressed("interact"): return
	if _player_skills.get_level(skill) < required_level:
		print("[Gathering] Need ", SkillSystem.Type.keys()[skill], " level ", required_level, ".")
		return
	_player_skills.award_xp(skill, xp_reward)
	_cooldown_end = Time.get_ticks_msec() / 1000.0 + cooldown_seconds

func _on_body_entered(body: Node2D) -> void:
	var ss: SkillSystem = body.get_node_or_null("SkillSystem")
	if ss: _player_skills = ss

func _on_body_exited(body: Node2D) -> void:
	var ss: SkillSystem = body.get_node_or_null("SkillSystem")
	if ss == _player_skills: _player_skills = null
