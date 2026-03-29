# RagnaRune — Unity Prototype

A hybrid Ragnarok Online × RuneScape combat prototype.

---

## Requirements

| Tool | Version |
|------|---------|
| Unity | 2022.3 LTS or newer |
| Render Pipeline | URP (Universal) or Built-in |
| TextMeshPro | via Package Manager |
| AI Navigation | via Package Manager (for NavMesh) |

---

## Quick Start (5 minutes)

### 1. Create a new Unity 2D project
Use the **2D (URP)** template.

### 2. Install packages
Window → Package Manager → Install:
- **TextMeshPro** (Essential Resources when prompted)
- **AI Navigation** (com.unity.ai.navigation)

### 3. Copy scripts
Drop the `Assets/Scripts/` folder into your Unity project's `Assets/` folder.

### 4. Create the Player prefab

1. **GameObject → 2D Object → Sprite** → name it `Player`
2. Add components:
   - `Rigidbody2D` (Gravity Scale = 0, Freeze Z rotation)
   - `CircleCollider2D` (radius 0.4)
   - `RagnaRune.Combat.CombatManager`
   - `RagnaRune.Cards.CardSystem`
   - `RagnaRune.Skills.SkillSystem`
   - `RagnaRune.Player.PlayerController`
3. In **CombatManager** Inspector:
   - Drag `CardSystem` and `SkillSystem` references
   - Fill in `PlayerStats` STR/AGI/VIT/INT/DEX/LUK (or leave defaults)
4. In **PlayerController** Inspector:
   - Set `Enemy Layer` to your enemy layer (see step 6)
   - Move speed: `4`
5. Save as Prefab

### 5. Create the Enemy prefab

1. **GameObject → 2D Object → Sprite** → name it `Enemy`
2. Add components:
   - `NavMeshAgent` (set Steering → Base Offset = 0)
   - `CircleCollider2D`
   - `RagnaRune.Enemy.EnemyController`
3. Assign an `EnemyData` ScriptableObject (see step 7)
4. Set the GameObject's **Layer** to `Enemy`
5. Save as Prefab

### 6. Layers
Edit → Project Settings → Tags and Layers:
- Add layer `Enemy`
- Set all enemy GameObjects to use it

### 7. Create ScriptableObjects

Right-click in the Project panel:
- **Create → RagnaRune → EnemyData** → fill in stats
- **Create → RagnaRune → Card** → fill in effect

Two code-created presets are available:
```csharp
EnemyData.MakePoring()
EnemyData.MakeSwordfish()
CardData.MakePoring()
CardData.MakeOrcLord()
```

### 8. Bake a NavMesh

1. **Window → AI → Navigation** → open the panel
2. Mark your ground/floor objects as **Navigation Static**
3. Click **Bake**

### 9. Create the GameManager

1. **GameObject → Create Empty** → name `GameManager`
2. Add `RagnaRune.Managers.GameManager`
3. Assign:
   - `PlayerPrefab` → your Player prefab
   - `EnemyPrefab` → your Enemy prefab
   - `SpawnEntries` → add rows: Data + SpawnPoint + Count

### 10. Wire the HUD

1. Create a UI Canvas (World Space canvas for damage numbers, Screen Space for vitals)
2. Add `RagnaRune.UI.CombatHUD` to any GameObject
3. Drag `CombatManager` and `SkillSystem` refs in
4. Connect Sliders, TMP_Text fields as labelled in the Inspector

---

## Controls

| Input | Action |
|-------|--------|
| WASD / Arrow keys | Move |
| Left-click on enemy | Target and start auto-attacking |
| `1` | Cast Fire spell on target (15 SP) |
| `2` | Cast Water spell on target (15 SP) |
| `3` | Cast Holy spell on target (20 SP) |

---

## Architecture Overview

```
GameManager          ← Singleton: spawns player + enemies, manages Zeny
├─ PlayerController  ← Input, movement, click-to-target
│  ├─ CombatManager  ← Auto-attack loop, spell casting, death handling
│  │  └─ CombatCalculator (static) ← RO damage math, element chart
│  ├─ CardSystem     ← Card inventory, socket slots, stat bonuses
│  └─ SkillSystem    ← RS-style XP/level tracking, stat feedback
└─ EnemyController   ← AI state machine (Idle/Wander/Chase/Attack/Dead)
   └─ EnemyData (SO) ← Stats, AI config, loot table
```

---

## Element Chart (simplified)

```
Fire    → Water   : ×0.5   Fire    → Earth  : ×1.75
Water   → Fire    : ×1.75  Wind    → Water  : ×1.75
Holy    → Undead  : ×2.0   Shadow  → Holy   : ×1.75
Ghost   → Ghost   : ×1.75  Neutral → Ghost  : ×0.5
```
Full 10×10 matrix lives in `Element.cs → ElementChart.GetMultiplier()`.

---

## Next Steps

1. **Sprites & Animations** — drop RO-style sprite sheets and wire up `Animator`
2. **Map** — import a Tiled map or build one with Unity Tilemaps
3. **Item system** — extend `CardData` or add `ItemData` SO for weapons/armor
4. **More skills** — add Mining/Fishing interactions that award XP to gathering skills
5. **Shop NPC** — spend Zeny on stat resets or card slots
6. **Save/Load** — serialize `SkillSystem` and `CardSystem` with `JsonUtility`
