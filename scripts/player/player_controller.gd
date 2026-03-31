## player_controller.gd
## Player scene root (CharacterBody2D).
##
## Scene tree:
##   PlayerController (CharacterBody2D)  — tag: "Player"
##   ├─ CollisionShape2D
##   ├─ AnimatedSprite2D
##   ├─ CombatManager
##   ├─ CardSystem
##   ├─ SkillSystem
##   ├─ StatusEffectSystem
##   ├─ RegenSystem
##   └─ ItemInventory
class_name PlayerController
extends CharacterBody2D

# ── Exports ────────────────────────────────────────────────────────────────────
@export var move_speed: float = 120.0  ## pixels per second
@export var enemy_layer_mask: int = 0b100  ## Physics layer 3 = enemy

# ── Child refs ─────────────────────────────────────────────────────────────────
@onready var combat: CombatManager       = $CombatManager
@onready var cards: CardSystem           = $CardSystem
@onready var skills: SkillSystem         = $SkillSystem
@onready var status: StatusEffectSystem  = $StatusEffectSystem
@onready var anim: AnimatedSprite2D      = $AnimatedSprite2D

# ── State ──────────────────────────────────────────────────────────────────────
var _is_dead: bool = false

# ──────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	add_to_group("player")
	combat.damage_dealt.connect(_on_damage_dealt)
	combat.player_defeated.connect(_on_player_defeated)
	combat.enemy_defeated.connect(_on_enemy_defeated)

# ── Input ──────────────────────────────────────────────────────────────────────

func _unhandled_input(event: InputEvent) -> void:
	if _is_dead: return

	# Click to target enemy
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		_handle_click(get_global_mouse_position())
		return

	# Spells
	if event.is_action_pressed("spell_fire"):
		combat.cast_spell(Element.Type.FIRE,  15)
	elif event.is_action_pressed("spell_water"):
		combat.cast_spell(Element.Type.WATER, 15)
	elif event.is_action_pressed("spell_holy"):
		combat.cast_spell(Element.Type.HOLY,  20)
	elif event.is_action_pressed("spell_wind"):
		combat.cast_spell(Element.Type.WIND,  15)
	elif event.is_action_pressed("ranged_attack"):
		combat.fire_ranged_attack()

# ── Movement ───────────────────────────────────────────────────────────────────

func _physics_process(_delta: float) -> void:
	if _is_dead: return

	var dir := Input.get_vector("move_left", "move_right", "move_up", "move_down")
	velocity = dir * move_speed
	move_and_slide()
	_update_animation(dir)

func _update_animation(dir: Vector2) -> void:
	if anim == null: return
	if dir.length() > 0.1:
		# 4-dir or 8-dir: pick animation by dominant axis
		if absf(dir.x) >= absf(dir.y):
			anim.play("walk_right" if dir.x > 0 else "walk_left")
		else:
			anim.play("walk_down" if dir.y > 0 else "walk_up")
		anim.flip_h = dir.x < 0 and anim.sprite_frames and anim.sprite_frames.has_animation("walk_right")
	else:
		anim.play("idle") if anim.sprite_frames and anim.sprite_frames.has_animation("idle") else anim.stop()

# ── Click to target ────────────────────────────────────────────────────────────

func _handle_click(world_pos: Vector2) -> void:
	var space := get_world_2d().direct_space_state
	var params := PhysicsPointQueryParameters2D.new()
	params.position       = world_pos
	params.collision_mask = enemy_layer_mask
	params.collide_with_areas = true
	params.collide_with_bodies = true
	var hits := space.intersect_point(params, 1)
	if hits.is_empty():
		return
	var body = hits[0]["collider"]
	if body is EnemyController:
		combat.set_target(body)

# ── Event handlers ─────────────────────────────────────────────────────────────

func _on_damage_dealt(result: DamageResult) -> void:
	# Forward to HUD via group broadcast — HUD subscribes to "combat_event"
	get_tree().call_group("hud", "on_damage_dealt", result, combat.current_target)

func _on_player_defeated() -> void:
	_is_dead      = true
	velocity      = Vector2.ZERO
	if anim: anim.play("death") if anim.sprite_frames and anim.sprite_frames.has_animation("death") else null
	get_tree().call_group("hud", "on_player_defeated")

func _on_enemy_defeated(enemy: EnemyController) -> void:
	# Drop loot (card drops already handled by EnemyController._roll_drops)
	# Award card to inventory if it landed on the player
	if enemy.data == null: return
	for drop in enemy.data.card_drops:
		if randf() < drop.drop_rate and drop.card != null:
			cards.add_to_inventory(drop.card)

func respawn() -> void:
	_is_dead = false
	combat.respawn()
	if anim: anim.play("idle")
