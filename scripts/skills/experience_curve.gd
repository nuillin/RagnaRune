## experience_curve.gd
## RuneScape-style geometric XP curve.
## Formula: xp(L) = floor(1/4 * sum_{n=1}^{L-1} floor(n + 300 * 2^(n/7)))
class_name ExperienceCurve
extends RefCounted

const DEFAULT_MAX_LEVEL: int  = 99
const EXTENDED_MAX_LEVEL: int = 120
const DISPLAY_XP_CAP: int     = 200_000_000

## Returns array[level] = minimum total XP to be at that level.
## Indices 0 and 1 are both 0; valid levels are 1..max_level.
static func build_min_total_xp_per_level(max_level: int) -> Array[int]:
	if max_level < 1: max_level = 1
	var t: Array[int] = []
	t.resize(max_level + 1)
	t[0] = 0
	t[1] = 0
	var sum: int = 0
	for n in range(1, max_level):
		sum += int(floor(n + 300.0 * pow(2.0, n / 7.0)))
		t[n + 1] = sum / 4
	return t

static func xp_to_next_level(current_xp: int, current_level: int,
		table: Array[int], max_level: int) -> int:
	if current_level >= max_level: return 0
	var next: int = table[clampi(current_level + 1, 0, max_level)]
	return maxi(0, next - current_xp)

static func level_progress(current_xp: int, current_level: int,
		table: Array[int], max_level: int) -> float:
	if current_level >= max_level: return 1.0
	var floor_xp: int = table[current_level]
	var ceil_xp:  int = table[current_level + 1]
	var span:     int = ceil_xp - floor_xp
	if span <= 0: return 1.0
	return clampf(float(current_xp - floor_xp) / span, 0.0, 1.0)

static func level_from_total_xp(total_xp: int, table: Array[int], max_level: int) -> int:
	for lvl in range(max_level, 0, -1):
		if total_xp >= table[lvl]:
			return lvl
	return 1
