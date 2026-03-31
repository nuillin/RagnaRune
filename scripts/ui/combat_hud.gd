## combat_hud.gd
## Wires combat signals to the HUD.
## Add to a CanvasLayer. Call initialise(combat_manager) from the Player scene or GameManager.
##
## Scene tree (CanvasLayer):
##   CombatHUD
##   ├─ VBoxContainer
##   │   ├─ HPBar      (ProgressBar)
##   │   ├─ HPLabel    (Label)
##   │   ├─ SPBar      (ProgressBar)
##   │   └─ SPLabel    (Label)
##   ├─ ZenyLabel      (Label)
##   ├─ TargetPanel    (PanelContainer)
##   │   ├─ TargetName (Label)
##   │   ├─ TargetHP   (ProgressBar)
##   │   └─ TargetElement (Label)
##   └─ Notification   (Label, anchored top-centre)
class_name CombatHUD
extends CanvasLayer

@onready var hp_bar: ProgressBar      = $VBox/HPBar
@onready var hp_label: Label          = $VBox/HPLabel
@onready var sp_bar: ProgressBar      = $VBox/SPBar
@onready var sp_label: Label          = $VBox/SPLabel
@onready var zeny_label: Label        = $ZenyLabel
@onready var target_panel: Control    = $TargetPanel
@onready var target_name: Label       = $TargetPanel/TargetName
@onready var target_hp: ProgressBar   = $TargetPanel/TargetHP
@onready var target_element: Label    = $TargetPanel/TargetElement
@onready var notification: Label      = $Notification

## Prefab for floating damage numbers (Label node).
@export var damage_number_scene: PackedScene

var _combat: CombatManager = null
var _notify_tween: Tween   = null

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	add_to_group("hud")
	_hide_target_panel()
	if notification: notification.modulate.a = 0.0
	GameManager.zeny_changed.connect(_on_zeny_changed)

func initialise(combat: CombatManager) -> void:
	_combat = combat
	combat.damage_dealt.connect(_on_damage_dealt)
	combat.damage_taken.connect(_on_damage_taken)
	combat.enemy_defeated.connect(_on_enemy_defeated)
	combat.player_defeated.connect(_on_player_defeated)
	combat.state_changed.connect(_on_state_changed)

# ── Per-frame vitals update ────────────────────────────────────────────────────

func _process(_delta: float) -> void:
	if _combat == null or _combat.player_stats == null: return
	var s := _combat.player_stats
	_set_bar(hp_bar, hp_label, s.current_hp, s.max_hp)
	_set_bar(sp_bar, sp_label, s.current_sp, s.max_sp)

	if zeny_label:
		zeny_label.text = "Z %s" % str(GameManager.get_zeny())

	if _combat.current_target and _combat.current_target.is_alive():
		var ts := _combat.current_target.stats
		if target_hp: target_hp.value = float(ts.current_hp) / float(ts.max_hp)

func _set_bar(bar: ProgressBar, lbl: Label, current: int, maximum: int) -> void:
	if bar: bar.value = float(current) / float(maxi(maximum, 1))
	if lbl: lbl.text  = "%d / %d" % [current, maximum]

# ── Damage numbers ─────────────────────────────────────────────────────────────

## Called via group broadcast from PlayerController.
func on_damage_dealt(result: DamageResult, target: EnemyController) -> void:
	if result.final_damage <= 0:
		_spawn_floating_text("MISS", Color.WHITE, target)
		return
	var col  := Color.YELLOW if result.is_critical else Color.RED
	var text := ("CRIT! %d" % result.final_damage) if result.is_critical else str(result.final_damage)
	_spawn_floating_text(text, col, target)

func _on_damage_taken(result: DamageResult) -> void:
	if result.final_damage <= 0: return
	_spawn_floating_text("-%d" % result.final_damage, Color(1.0, 0.5, 0.0), null)

func _spawn_floating_text(text: String, color: Color, anchor: EnemyController) -> void:
	if damage_number_scene == null: return
	var lbl: Label = damage_number_scene.instantiate()
	add_child(lbl)
	lbl.text            = text
	lbl.modulate        = color
	lbl.z_index         = 10

	# Convert world position to canvas coordinates
	var world_pos: Vector2 = Vector2.ZERO
	if anchor:
		world_pos = get_viewport().get_canvas_transform() * anchor.global_position
	else:
		var player := get_tree().get_first_node_in_group("player") as Node2D
		if player:
			world_pos = get_viewport().get_canvas_transform() * player.global_position
	lbl.global_position = world_pos + Vector2(-20, -40)

	# Float upward and fade
	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(lbl, "position:y", lbl.position.y - 60.0, 0.8)
	tween.tween_property(lbl, "modulate:a", 0.0, 0.8).set_delay(0.2)
	tween.tween_callback(lbl.queue_free).set_delay(1.0)

# ── Target panel ───────────────────────────────────────────────────────────────

func _on_state_changed(state: int) -> void:
	if state == CombatManager.State.IN_COMBAT and _combat.current_target:
		_show_target_panel(_combat.current_target)
	else:
		_hide_target_panel()

func _show_target_panel(enemy: EnemyController) -> void:
	if target_panel: target_panel.visible = true
	if target_name:    target_name.text    = enemy.data.enemy_name if enemy.data else "Unknown"
	if target_element: target_element.text = Element.get_name(enemy.stats.body_element)

func _hide_target_panel() -> void:
	if target_panel: target_panel.visible = false

# ── Notifications ──────────────────────────────────────────────────────────────

func _show_notification(msg: String) -> void:
	if notification == null: return
	if _notify_tween: _notify_tween.kill()
	notification.text       = msg
	notification.modulate.a = 1.0
	_notify_tween = create_tween()
	_notify_tween.tween_interval(2.0)
	_notify_tween.tween_property(notification, "modulate:a", 0.0, 0.6)

func _on_enemy_defeated(enemy: EnemyController) -> void:
	_hide_target_panel()
	_show_notification("%s defeated!" % (enemy.data.enemy_name if enemy.data else "Enemy"))

func _on_player_defeated() -> void:
	_show_notification("You died…")

func on_player_defeated() -> void:  ## called via group from PlayerController
	_on_player_defeated()

func _on_zeny_changed(total: int) -> void:
	if zeny_label: zeny_label.text = "Z %s" % str(total)
