using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Combat;

namespace RagnaRune.Skills
{
    public enum SkillType
    {
        // Combat skills (gain XP automatically in combat)
        Attack,
        Strength,
        Defense,
        Hitpoints,
        Magic,
        Ranged,

        // Gathering / crafting (gain XP by interacting with world)
        Mining,
        Fishing,
        Woodcutting,
        Crafting,
        Smithing,
        Cooking,
    }

    [Serializable]
    public class Skill
    {
        public SkillType Type;
        public int Level = 1;
        public long XP = 0;

        public const int MaxLevel = ExperienceCurve.DefaultMaxLevel;

        private static long[] _minXpPerLevel;

        private static long[] MinXpPerLevel
        {
            get
            {
                if (_minXpPerLevel != null) return _minXpPerLevel;
                _minXpPerLevel = ExperienceCurve.BuildMinTotalXpPerLevel(MaxLevel);
                return _minXpPerLevel;
            }
        }

        /// <summary>Minimum total XP required to be at <paramref name="level"/>.</summary>
        public long XPForLevel(int level) =>
            level <= 1 ? 0 : MinXpPerLevel[Mathf.Clamp(level, 0, MaxLevel)];

        public long XPToNextLevel =>
            ExperienceCurve.XpToNextLevel(XP, Level, MinXpPerLevel, MaxLevel);

        public float LevelProgress =>
            ExperienceCurve.LevelProgress(XP, Level, MinXpPerLevel, MaxLevel);

        /// <summary>Add XP; returns true if the skill levelled up.</summary>
        public bool AddXP(long amount)
        {
            int prev = Level;
            XP += amount;
            Level = Mathf.Min(ExperienceCurve.LevelFromTotalXp(XP, MinXpPerLevel, MaxLevel), MaxLevel);
            return Level > prev;
        }

        /// <summary>Replace total XP from save; does not fire XP gain / level-up events.</summary>
        public void SetTotalXpFromSave(long totalXp)
        {
            XP = Math.Max(0, totalXp);
            Level = Mathf.Min(ExperienceCurve.LevelFromTotalXp(XP, MinXpPerLevel, MaxLevel), MaxLevel);
        }
    }

    /// <summary>
    /// Manages all skills for a single character. Attach to the Player GameObject.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        public event Action<SkillType, int> OnLevelUp;   // skill, newLevel
        public event Action<SkillType, long> OnXPGain;   // skill, xpGained
        /// <summary>Fired after <see cref="ApplySaveSnapshot"/> finishes (refresh UI / stats).</summary>
        public event Action OnSkillsRestored;

        private Dictionary<SkillType, Skill> _skills = new();

        private void Awake()
        {
            foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
                _skills[type] = new Skill { Type = type };
        }

        public Skill GetSkill(SkillType type) => _skills[type];
        public int GetLevel(SkillType type)    => _skills[type].Level;

        /// <summary>Award XP to a skill. Fires events on level-up.</summary>
        public void AwardXP(SkillType type, long amount)
        {
            if (amount <= 0) return;
            var skill = _skills[type];
            OnXPGain?.Invoke(type, amount);
            if (skill.AddXP(amount))
            {
                OnLevelUp?.Invoke(type, skill.Level);
                Debug.Log($"[SkillSystem] {type} reached level {skill.Level}!");
            }
        }

        // ── Combat XP Helpers ─────────────────────────────────────────────────

        /// <summary>Call after the player lands a melee hit.</summary>
        public void OnMeleeHit(int damage)
        {
            AwardXP(SkillType.Attack,   (long)(damage * 4));
            AwardXP(SkillType.Strength, (long)(damage * 4));
            AwardXP(SkillType.Hitpoints,(long)(damage * 1.33));
        }

        /// <summary>Call when the player takes damage.</summary>
        public void OnDamageTaken(int damage)
        {
            AwardXP(SkillType.Defense,   (long)(damage * 3));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1));
        }

        /// <summary>Call after a magic spell hits.</summary>
        public void OnMagicHit(int damage)
        {
            AwardXP(SkillType.Magic,     (long)(damage * 5));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1.33));
        }

        // ── Stat Bonuses from Skills ──────────────────────────────────────────
        // Skills feed back into derived stats (like RS where Strength affects max hit).

        public int GetAttackBonus()   => GetLevel(SkillType.Attack)   / 5;
        public int GetStrengthBonus() => GetLevel(SkillType.Strength) / 4;
        public int GetDefenseBonus()  => GetLevel(SkillType.Defense)  / 5;
        public int GetHPBonus()       => GetLevel(SkillType.Hitpoints) * 2;
        public int GetMagicBonus()    => GetLevel(SkillType.Magic)     / 4;

        public void ApplySkillBonusesToStats(Core.CharacterStats stats)
        {
            stats.BonusATK   += GetAttackBonus() + GetStrengthBonus();
            stats.BonusDEF   += GetDefenseBonus();
            stats.BonusMaxHP += GetHPBonus();
        }

        // ── Save / load ─────────────────────────────────────────────────────

        public SkillsSaveData BuildSaveSnapshot()
        {
            var values = (SkillType[])Enum.GetValues(typeof(SkillType));
            var xp = new long[values.Length];
            for (int i = 0; i < values.Length; i++)
                xp[i] = _skills[values[i]].XP;
            return new SkillsSaveData
            {
                FormatVersion = SkillsSaveData.CurrentFormatVersion,
                TotalXpBySkillIndex = xp
            };
        }

        /// <summary>Restores totals from disk; does not fire <see cref="OnXPGain"/> / <see cref="OnLevelUp"/>.</summary>
        public void ApplySaveSnapshot(SkillsSaveData data)
        {
            if (data?.TotalXpBySkillIndex == null) return;
            var values = (SkillType[])Enum.GetValues(typeof(SkillType));
            for (int i = 0; i < values.Length; i++)
            {
                long xp = i < data.TotalXpBySkillIndex.Length ? data.TotalXpBySkillIndex[i] : 0;
                _skills[values[i]].SetTotalXpFromSave(xp);
            }

            OnSkillsRestored?.Invoke();

            var combat = GetComponent<CombatManager>();
            if (combat != null) combat.RebuildStats();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Print All Skill Levels")]
        public void DebugPrintSkills()
        {
            foreach (var kv in _skills)
                Debug.Log($"  {kv.Key}: Lvl {kv.Value.Level} | {kv.Value.XP} xp");
        }
#endif
    }
}
