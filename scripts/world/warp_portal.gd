## warp_portal.gd
## RO-style warp portal.
##
## Scene tree:
##   WarpPortal (Area2D)
##   ├─ CollisionShape2D  (trigger zone — player walks into it)
##   └─ Sprite2D / AnimatedSprite2D  (optional visual)
class_name WarpPortal
extends Area2D

enum WarpType { SAME_SCENE, LOAD_SCENE }

@export var warp_type: WarpType = WarpType.SAME_SCENE
@export var activate_on_enter: bool = true  ## false = requires "talk" key press

## Same-scene destination: drag a Marker2D/Node2D from the scene tree.
@export var destination: Node2D = null

## Cross-scene destination.
@export_file("*.tscn") var destination_scene: String = ""
@export var destination_spawn_id: String = ""

@export var portal_label: String = ""
@export var fade_time: float = 0.3

var _player_inside: bool = false
var _activated: bool     = false

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _process(_delta: float) -> void:
	if not _player_inside or activate_on_enter or _activated: return
	if Input.is_action_just_pressed("talk"):
		_activate()

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player"): return
	_player_inside = true
	if activate_on_enter:
		_activate()

func _on_body_exited(body: Node2D) -> void:
	if body.is_in_group("player"):
		_player_inside = false

func _activate() -> void:
	if _activated: return
	_activated = true
	_do_warp()

func _do_warp() -> void:
	# Freeze player movement
	var player := get_tree().get_first_node_in_group("player") as CharacterBody2D
	if player: player.velocity = Vector2.ZERO

	await get_tree().create_timer(fade_time).timeout

	match warp_type:
		WarpType.SAME_SCENE:
			if destination and player:
				player.global_position = destination.global_position
		WarpType.LOAD_SCENE:
			if not destination_scene.is_empty():
				GameManager.warp_to_scene(destination_scene, destination_spawn_id)
				return  # don't reset _activated — scene is changing

	_activated = false


## ── SceneSpawnController ────────────────────────────────────────────────────
## Place one node per scene. Reads GameManager.pending_spawn_id on _ready
## and moves the player to the matching spawn point.
class_name SceneSpawnController
extends Node

@export var spawn_points: Array = []

func _ready() -> void:
	if GameManager.pending_spawn_id.is_empty(): return
	for sp in spawn_points:
		if sp.id != GameManager.pending_spawn_id: continue
		await get_tree().process_frame  # wait one frame for player node to exist
		var player := get_tree().get_first_node_in_group("player") as Node2D
		if player and sp.marker:
			player.global_position = sp.marker.global_position
		break
	GameManager.pending_spawn_id = ""


class SpawnPointEntry:
	@export var id: String = ""
	@export var marker: Marker2D = null
