## element.gd
## Element type definitions and the full RO 10×10 damage multiplier chart.
## Access via: Element.Type.FIRE, Element.get_multiplier(atk, def)
class_name Element
extends RefCounted

enum Type {
	NEUTRAL = 0,
	WATER   = 1,
	EARTH   = 2,
	FIRE    = 3,
	WIND    = 4,
	POISON  = 5,
	HOLY    = 6,
	SHADOW  = 7,
	GHOST   = 8,
	UNDEAD  = 9,
}

## Rows = attacker element, Columns = defender element.
## Values are percentage multipliers (100 = normal damage).
const CHART: Array = [
	#        Neut  Watr  Erth  Fire  Wind  Posn  Holy  Shdw  Ghst  Undd
	[        100,  100,  100,  100,  100,  100,   75,  100,   50,  100],  # Neutral
	[        100,   25,  100,  175,   50,  100,  100,  100,   50,   75],  # Water
	[        100,  100,   25,  100,  175,  100,  100,  100,   50,   75],  # Earth
	[        100,   50,  175,   25,  100,  100,  100,  100,   50,  125],  # Fire
	[        100,  175,   50,  100,   25,  100,  100,  100,   50,   75],  # Wind
	[        100,  100,  100,  100,  100,    0,  100,  100,   50,  100],  # Poison
	[        100,  100,  100,  100,  100,  100,  100,  175,   75,  200],  # Holy
	[        100,  100,  100,  100,  100,   50,  175,  100,   75,  100],  # Shadow
	[         25,  100,  100,  100,  100,  100,  100,  100,  175,   75],  # Ghost
	[        100,  100,  100,  125,  100,  100,  200,  100,   75,    0],  # Undead
]

static func get_multiplier(attacker: int, defender: int) -> float:
	return CHART[attacker][defender] / 100.0

static func get_color(element: int) -> Color:
	match element:
		Type.FIRE:   return Color("#FF4500")
		Type.WATER:  return Color("#1E90FF")
		Type.WIND:   return Color("#7CFC00")
		Type.EARTH:  return Color("#8B4513")
		Type.HOLY:   return Color("#FFD700")
		Type.SHADOW: return Color("#800080")
		Type.POISON: return Color("#32CD32")
		Type.GHOST:  return Color("#708090")
		Type.UNDEAD: return Color("#2F2F2F")
		_:           return Color.WHITE

static func get_name(element: int) -> String:
	return Type.keys()[element] if element >= 0 and element < Type.size() else "Unknown"
