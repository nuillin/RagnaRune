# RagnaRune

> A hybrid Ragnarok Online × RuneScape RPG built in Unity 2D.

---

## Requirements

| Tool | Version |
|------|---------|
| Unity | 2022.3 LTS or newer |
| Render Pipeline | URP (Universal) or Built-in 2D |
| TextMeshPro | via Package Manager |
| AI Navigation | via Package Manager (NavMesh) |

---

## Architecture

```
GameManager (singleton)
├── PlayerController        ← WASD input, click-to-target, hotkeys
│   ├── CombatManager       ← Auto-attack loop, spells, ranged, death
│   │   ├── CombatCalculator (static) ← RO math: melee, ranged, magic
│   │   └── StatusEffectSystem  ← Timed debuffs: Poison, Stun, Freeze…
│   ├── CardSystem          ← Inventory, socket slots, per-hit bonuses
│   ├── SkillSystem         ← RS-style XP/level; 12 skills; save/load
│   └── RegenSystem         ← Passive HP/SP regen; sit bonus
│
├── EnemyController (per enemy)
│   ├── EnemyData (SO)      ← Stats, AI config, loot table
│   └── StatusEffectSystem  ← Same system on enemies
│
└── World objects
    ├── LootPickup          ← Ground items (Zeny, Cards, CraftingItems)
    ├── WarpPortal          ← RO-style zone transitions
    ├── ShopNPC             ← Zeny-based buy/sell merchant
    ├── GatheringInteractable ← Mining/Fishing/etc. XP nodes
    └── CraftingStation     ← Skill-gated crafting bench
```

---

## Quick Start

### 1. Unity project
New project → **2D (URP)** template → Unity 2022.3 LTS.

### 2. Packages
Window → Package Manager → install:
- **TextMeshPro** (click "Import TMP Essentials" when prompted)
- **AI Navigation** (`com.unity.ai.navigation`)

### 3. Scripts
Drop `Assets/Scripts/` into your project.

### 4. Player prefab

| Component | Settings |
|-----------|----------|
| `Rigidbody2D` | Gravity Scale = 0, Freeze Z rotation |
| `CircleCollider2D` | radius 0.4, tag the GO as `Player` |
| `CombatManager` | drag CardSystem + SkillSystem refs |
| `CardSystem` | (auto-seeds 4 starter cards in Editor) |
| `SkillSystem` | — |
| `StatusEffectSystem` | — |
| `RegenSystem` | — |
| `PlayerController` | set Enemy Layer mask, move speed 4 |

### 5. Enemy prefab

| Component | Notes |
|-----------|-------|
| `NavMeshAgent` | Base Offset = 0, Stopping Distance = 1 |
| `CircleCollider2D` | Layer = `Enemy` |
| `EnemyController` | assign EnemyData SO |
| `StatusEffectSystem` | — |

### 6. Bake NavMesh
Window → AI → Navigation → mark ground tiles as **Navigation Static** → **Bake**.

### 7. GameManager
Empty GameObject → `GameManager` → assign Player/Enemy prefabs and SpawnEntries.

### 8. World objects

**Warp portal** — add `WarpPortal` + Collider2D (trigger) to any GO. Set `ActivateOnEnter = true` and assign a `Destination` transform (same scene) or `DestinationScene` (cross-scene).

**Shop NPC** — add `ShopNPC` + Collider2D (trigger). Fill `Stock` in the Inspector. Subscribe to `OnShopOpened` from your UI to render the shop panel.

**Loot drops** — create a prefab with `LootPickup` + `SpriteRenderer` + Collider2D (trigger). Drag it into `EnemyData.LootDropPrefab` (or call `LootPickup.SpawnZeny/SpawnCard` from code).

**Gathering nodes** — add `GatheringInteractable` + Collider2D (trigger). Set `Skill`, `XpReward`, and `RequiredLevel`.

---

## Controls

| Input | Action |
|-------|--------|
| WASD / arrows | Move |
| Left-click enemy | Target → begin auto-attacking |
| `1` | Fire spell (15 SP) |
| `2` | Water spell (15 SP) |
| `3` | Holy spell (20 SP) |
| `4` | Wind spell (15 SP) |
| `R` | Ranged shot (5 SP) |
| `E` | Interact / pick up loot / use gathering node / craft |
| `F` | Talk to NPC / enter warp (when not auto-activating) |

---

## Status Effects

| Effect | Behaviour |
|--------|-----------|
| Poison | 1% max HP per tick; cannot kill |
| Stun | Cannot act |
| Freeze | Cannot act; +extra damage from Fire/Wind |
| Sleep | Cannot act; next hit ×2 damage + wakes |
| Silence | Cannot cast spells |
| Blind | HIT −50% |
| Slow | ASPD −50%, FLEE −50% |
| Curse | STR/DEX/AGI ÷2, LUK = 0 |
| Bleeding | Flat HP loss per tick |
| Bless (buff) | STR/INT/DEX +10 |
| Agi (buff) | AGI/FLEE +20, ASPD +0.3 |

Apply via `StatusEffectSystem.ApplyPoison()`, `.ApplyStun()`, etc.
Resist % can be added via `CardEffect.ResistStatus` cards.

---

## Element Chart (excerpt)

| Attacker → Defender | Fire | Water | Wind | Earth | Holy | Undead |
|---------------------|------|-------|------|-------|------|--------|
| Fire | ×0.25 | ×0.5 | — | ×1.75 | — | ×1.25 |
| Water | ×1.75 | ×0.25 | ×0.5 | — | — | ×0.75 |
| Holy | — | — | — | — | — | ×2.0 |
| Ghost | ×0.25 | — | — | — | — | ×0.75 |

Full 10×10 matrix in `Element.cs → ElementChart`.

---

## CI / CD

`.github/workflows/unity-ci.yml` runs on every push/PR:
1. **Edit-mode tests** and **play-mode tests** (parallel)
2. **Builds** for Linux64, Windows64, WebGL (main branch only, after tests pass)
3. **WebGL deploy** to GitHub Pages (main branch only)

### Secrets required
Add these in **Settings → Secrets → Actions**:

| Secret | Where to get it |
|--------|----------------|
| `UNITY_LICENSE` | Run `game-ci/unity-activate` locally or use a Personal license XML |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

See [game.ci/docs](https://game.ci/docs/github/getting-started) for the full license setup guide.

---

## Roadmap

- [ ] Sprite sheets & Animator controllers (RO-style 8-dir sprites)
- [ ] Tilemap world + Tiled importer
- [ ] Party system (share XP, revive)
- [ ] Boss enemies with multi-phase AI
- [ ] Card album UI (collection tracker)
- [ ] Auction House / player-to-player trade
- [ ] Mobile virtual joystick input layer
- [ ] Sound & music (SFX on hit, level-up jingle)

---

## License

MIT — see `LICENSE`.
