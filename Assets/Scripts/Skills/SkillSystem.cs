using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Combat;

namespace RagnaRune.Skills
{
    public enum SkillType
    {
        // Combat — gain XP in combat automatically
        Attack,
        Strength,
        Defense,
        Hitpoints,
        Magic,
        Ranged,

        // Gathering / crafting — gain XP via world interaction
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
        public int  Level = 1;
        public long XP    = 0;

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

        public long XPForLevel(int level) =>
            level <= 1 ? 0 : MinXpPerLevel[Mathf.Clamp(level, 0, MaxLevel)];

        public long XPToNextLevel =>
            ExperienceCurve.XpToNextLevel(XP, Level, MinXpPerLevel, MaxLevel);

        public float LevelProgress =>
            ExperienceCurve.LevelProgress(XP, Level, MinXpPerLevel, MaxLevel);

        public bool AddXP(long amount)
        {
            int prev = Level;
            XP   += amount;
            Level = Mathf.Min(ExperienceCurve.LevelFromTotalXp(XP, MinXpPerLevel, MaxLevel), MaxLevel);
            return Level > prev;
        }

        public void SetTotalXpFromSave(long totalXp)
        {
            XP    = Math.Max(0, totalXp);
            Level = Mathf.Min(ExperienceCurve.LevelFromTotalXp(XP, MinXpPerLevel, MaxLevel), MaxLevel);
        }
    }

    /// <summary>
    /// Manages all skills for a single character. Attach to the Player GameObject.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        public event Action<SkillType, int>  OnLevelUp;        // skill, newLevel
        public event Action<SkillType, long> OnXPGain;         // skill, xpGained
        public event Action                  OnSkillsRestored; // after save restore

        private Dictionary<SkillType, Skill> _skills = new();

        private void Awake()
        {
            foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
                _skills[type] = new Skill { Type = type };
        }

        public Skill GetSkill(SkillType type) => _skills[type];
        public int   GetLevel(SkillType type)  => _skills[type].Level;

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

        // ── Combat XP helpers ─────────────────────────────────────────────────

        public void OnMeleeHit(int damage)
        {
            AwardXP(SkillType.Attack,    (long)(damage * 4));
            AwardXP(SkillType.Strength,  (long)(damage * 4));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1.33));
        }

        public void OnRangedHit(int damage)
        {
            AwardXP(SkillType.Ranged,    (long)(damage * 4));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1.33));
        }

        public void OnMagicHit(int damage)
        {
            AwardXP(SkillType.Magic,     (long)(damage * 5));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1.33));
        }

        public void OnDamageTaken(int damage)
        {
            AwardXP(SkillType.Defense,   (long)(damage * 3));
            AwardXP(SkillType.Hitpoints, (long)(damage * 1));
        }

        // ── Stat bonuses from skills ──────────────────────────────────────────

        public int GetAttackBonus()   => GetLevel(SkillType.Attack)    / 5;
        public int GetStrengthBonus() => GetLevel(SkillType.Strength)  / 4;
        public int GetDefenseBonus()  => GetLevel(SkillType.Defense)   / 5;
        public int GetHPBonus()       => GetLevel(SkillType.Hitpoints) * 2;
        public int GetMagicBonus()    => GetLevel(SkillType.Magic)     / 4;
        public int GetRangedBonus()   => GetLevel(SkillType.Ranged)    / 5;

        public void ApplySkillBonusesToStats(Core.CharacterStats stats)
        {
            stats.BonusATK   += GetAttackBonus() + GetStrengthBonus();
            stats.BonusDEF   += GetDefenseBonus();
            stats.BonusMaxHP += GetHPBonus();
            stats.BonusHIT   += GetRangedBonus(); // Ranged level improves accuracy
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        public SkillsSaveData BuildSaveSnapshot()
        {
            var values = (SkillType[])Enum.GetValues(typeof(SkillType));
            var xp     = new long[values.Length];
            for (int i = 0; i < values.Length; i++)
                xp[i] = _skills[values[i]].XP;
            return new SkillsSaveData
            {
                FormatVersion       = SkillsSaveData.CurrentFormatVersion,
                TotalXpBySkillIndex = xp,
            };
        }

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
            GetComponent<CombatManager>()?.RebuildStats();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Print All Skill Levels")]
        public void DebugPrintSkills()
        {
            foreach (var kv in _skills)
                Debug.Log($"  {kv.Key}: Lvl {kv.Value.Level} | {kv.Value.XP} xp | {kv.Value.LevelProgress * 100:F1}% to next");
        }
#endif
    }
}
