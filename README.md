# RagnaRune — Godot 4

> Ragnarok Online × RuneScape hybrid RPG. Converted from Unity C# to Godot 4 GDScript.

---

## Requirements

| Tool | Version |
|------|---------|
| Godot | 4.3 stable or newer |
| GUT (tests, optional) | github.com/bitwes/Gut |

No .NET SDK needed — pure GDScript.

---

## Project structure

```
RagnaRune_Godot/
├─ project.godot               ← Input map, physics layers, autoloads
├─ scripts/
│   ├─ core/
│   │   ├─ element.gd          ← Element enum + full 10×10 damage chart
│   │   ├─ character_stats.gd  ← Shared stat Resource (replaces ScriptableObject)
│   │   └─ damage_result.gd    ← Value object returned by CombatCalculator
│   ├─ combat/
│   │   ├─ combat_calculator.gd   ← Pure-static RO math (melee/ranged/magic/XP)
│   │   ├─ combat_manager.gd      ← Auto-attack loop, spells, ranged; child Node
│   │   ├─ status_effect_system.gd← Timed debuffs/buffs with tick damage
│   │   └─ regen_system.gd        ← Passive HP/SP regen + sit bonus
│   ├─ cards/
│   │   ├─ card_data.gd           ← Card Resource with all effects + presets
│   │   └─ card_system.gd         ← Inventory, socket slots, stat bonuses
│   ├─ skills/
│   │   ├─ experience_curve.gd    ← Exact RS XP formula
│   │   ├─ skill_system.gd        ← 12 skills, award_xp, level_up signal
│   │   └─ skill_persistence.gd   ← JSON save/load to user://
│   ├─ crafting/
│   │   ├─ crafting_item.gd
│   │   ├─ crafting_recipe.gd
│   │   ├─ crafting_catalog.gd    ← Maps save IDs back to Resources at load time
│   │   ├─ crafting_service.gd    ← Pure-static craft logic
│   │   └─ item_inventory.gd      ← Stack-based inventory Node
│   ├─ enemy/
│   │   ├─ enemy_data.gd          ← Enemy Resource + presets (Poring, Swordfish)
│   │   └─ enemy_controller.gd    ← CharacterBody2D + NavigationAgent2D AI
│   ├─ player/
│   │   └─ player_controller.gd   ← CharacterBody2D, WASD, click-to-target
│   ├─ world/
│   │   ├─ loot_pickup.gd         ← Ground item (Area2D), vacuum + E-key
│   │   ├─ warp_portal.gd         ← RO-style zone transitions + SceneSpawnController
│   │   ├─ shop_npc.gd            ← Walk-up Zeny merchant
│   │   ├─ gathering_interactable.gd
│   │   └─ crafting_station.gd
│   ├─ managers/
│   │   └─ game_manager.gd        ← Autoload singleton
│   └─ ui/
│       ├─ combat_hud.gd          ← HP/SP bars, damage numbers, target panel
│       ├─ skill_row_view.gd      ← Single row with flash animation
│       └─ skills_hud.gd          ← Full RS-style skills panel
└─ .github/workflows/godot-ci.yml ← Test + export + Pages deploy
```

---

## Unity → Godot conversion table

| Unity | Godot 4 |
|-------|---------|
| `MonoBehaviour` | `Node` / `Node2D` / `CharacterBody2D` |
| `ScriptableObject` | `Resource` |
| `[SerializeField]` / `[Export]` | `@export` |
| `Rigidbody2D` | `CharacterBody2D` + `move_and_slide()` |
| `NavMeshAgent` | `NavigationAgent2D` |
| `Physics2D.OverlapPoint` | `PhysicsDirectSpaceState2D.intersect_point()` |
| `event Action<T>` | `signal name(arg: Type)` |
| `StartCoroutine` | `await get_tree().create_timer(n).timeout` or `Tween` |
| `Update()` | `_process(delta)` |
| `FixedUpdate()` | `_physics_process(delta)` |
| `GetComponent<T>()` | `get_node("NodeName")` or `$NodeName` |
| `Instantiate(prefab)` | `scene.instantiate()` + `add_child()` |
| `Destroy(go)` | `node.queue_free()` |
| `SceneManager.LoadScene` | `get_tree().change_scene_to_file("res://...")` |
| `JsonUtility` | `JSON.stringify()` / `JSON.parse_string()` |
| `Application.persistentDataPath` | `user://` |
| `Debug.Log` | `print()` |
| `[RequireComponent]` | `@onready var x = $ChildName` (enforced by scene tree) |

---

## Quick start

### 1. Open the project
File → Open → select `RagnaRune_Godot/` folder. Godot will import all scripts.

### 2. Physics layers
Layers are pre-configured in `project.godot`:

| Layer | Name |
|-------|------|
| 1 | world |
| 2 | player |
| 3 | enemy |
| 4 | interactable |
| 5 | loot |

### 3. Create the Player scene
1. New Scene → `CharacterBody2D` root → rename `PlayerController`
2. Add child nodes: `CollisionShape2D`, `AnimatedSprite2D`
3. Add script: `scripts/player/player_controller.gd`
4. Add sibling Node children (no scripts needed on the nodes themselves — scripts go as root):
   - `CombatManager` (Node, script: `combat_manager.gd`)
   - `CardSystem` (Node, script: `card_system.gd`)
   - `SkillSystem` (Node, script: `skill_system.gd`)
   - `StatusEffectSystem` (Node, script: `status_effect_system.gd`)
   - `RegenSystem` (Node, script: `regen_system.gd`)
   - `ItemInventory` (Node, script: `item_inventory.gd`)
5. Set Physics Layer to **2 (player)**; add tag **"player"** via Groups

### 4. Create the Enemy scene
1. New Scene → `CharacterBody2D` → rename `EnemyController`
2. Add: `NavigationAgent2D`, `CollisionShape2D`, `AnimatedSprite2D`, `StatusEffectSystem`
3. Attach `scripts/enemy/enemy_controller.gd`
4. Set Physics Layer to **3 (enemy)**

### 5. Create the Loot scene
1. New Scene → `Area2D` → rename `LootPickup`
2. Add: `CollisionShape2D`, `Sprite2D`
3. Attach `scripts/world/loot_pickup.gd`

### 6. Wire the GameManager autoload
It is already registered in `project.godot`. In the Inspector set:
- `Player Scene` → your player `.tscn`
- `Enemy Scene` → your enemy `.tscn`
- `Loot Scene` → your loot `.tscn`

### 7. Create a game scene
1. New Scene → `Node2D` → name `Game`
2. Add a `TileMapLayer` (ground), bake a `NavigationRegion2D`
3. Add a `SpawnManager` node or call `GameManager.spawn_player()` from a `_ready()` script
4. Add a `CanvasLayer` → attach `CombatHUD` + `SkillsHUD`

---

## Controls

| Input | Action |
|-------|--------|
| WASD / arrows | Move |
| Left-click enemy | Target → auto-attack |
| `1` | Fire spell (15 SP) |
| `2` | Water spell (15 SP) |
| `3` | Holy spell (20 SP) |
| `4` | Wind spell (15 SP) |
| `R` | Ranged shot (5 SP) |
| `E` | Interact / pick up / gather / craft |
| `F` | Talk to NPC / enter warp |

---

## Status effects

| Effect | Behaviour |
|--------|-----------|
| POISON | 1% max HP per second tick; cannot kill |
| STUN | Cannot act |
| FREEZE | Cannot act; extra damage from Fire/Wind |
| SLEEP | Cannot act; next hit ×2 + wakes |
| SILENCE | Cannot cast spells |
| BLIND | HIT −50% |
| SLOW | ASPD −50%, FLEE −50% |
| CURSE | STR/DEX/AGI ÷2, LUK = 0 |
| BLEEDING | Flat HP tick every 2 s |
| BLESS (buff) | STR/INT/DEX +10 |
| AGI_UP (buff) | FLEE +20, ASPD +0.3 |

Apply via `status_effects.apply_poison()`, `.apply_stun()`, etc.

---

## CI / CD

`.github/workflows/godot-ci.yml` uses `barichello/godot-ci:4.3` Docker image:
1. **GUT tests** on every push/PR
2. **Export** Linux64, Windows64, Web (main branch only, after tests pass)
3. **Deploy Web** to GitHub Pages automatically

No secrets needed beyond the default `GITHUB_TOKEN` (provided by Actions automatically).

---

## Roadmap

- [ ] AnimatedSprite2D sheets (RO 8-dir walking)
- [ ] TileMapLayer world with NavigationRegion2D
- [ ] Card album UI
- [ ] Party system with shared XP
- [ ] Boss enemies with multi-phase behaviour trees
- [ ] Auction House / player trade
- [ ] Mobile virtual joystick (built-in Godot touch input)
- [ ] Sound: SFX bus + Music bus with RO-inspired tracks

---

## License
MIT — see `LICENSE`.
