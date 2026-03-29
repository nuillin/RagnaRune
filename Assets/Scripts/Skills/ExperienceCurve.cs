using System;
using UnityEngine;

namespace RagnaRune.Skills
{
    /// <summary>
    /// RuneScape-style geometric XP curve (Old School / pre-2007 style table).
    /// <para>
    /// Exact total XP required to <b>reach</b> skill level L (L ≥ 2):
    /// <c>xp(L) = floor( 1/4 * sum_{n=1}^{L-1} floor( n + 300 * 2^(n/7) ) )</c>
    /// </para>
    /// <para>
    /// Approximation (wiki, ~&lt;1% error at high levels):
    /// <c>xp(L) ≈ 720 * 2^(L/7) + L(L-1)/8 - 795</c>
    /// </para>
    /// </summary>
    public static class ExperienceCurve
    {
        /// <summary>Classic skill cap.</summary>
        public const int DefaultMaxLevel = 99;

        /// <summary>Post-99 extension often cited on the wiki (~104M total XP).</summary>
        public const int ExtendedMaxLevel = 120;

        /// <summary>In-game display cap for total XP in RS.</summary>
        public const long DisplayXpCap = 200_000_000;

        /// <summary>
        /// <paramref name="minTotalXpForLevel"/>[level] = minimum total XP to be <b>at</b> that level.
        /// Indices 0 and 1 are both 0; valid levels are 1 … <paramref name="maxLevel"/>.
        /// </summary>
        public static long[] BuildMinTotalXpPerLevel(int maxLevel)
        {
            if (maxLevel < 1) maxLevel = 1;
            var t = new long[maxLevel + 1];
            t[0] = 0;
            t[1] = 0;
            long sum = 0;
            for (int n = 1; n < maxLevel; n++)
            {
                sum += (long)Math.Floor(n + 300.0 * Math.Pow(2, n / 7.0));
                t[n + 1] = sum / 4;
            }
            return t;
        }

        public static long XpToNextLevel(long currentTotalXp, int currentLevel, long[] minTotalXpForLevel, int maxLevel)
        {
            if (currentLevel >= maxLevel) return 0;
            long nextThreshold = minTotalXpForLevel[Mathf.Clamp(currentLevel + 1, 0, maxLevel)];
            return Math.Max(0, nextThreshold - currentTotalXp);
        }

        public static float LevelProgress(long currentTotalXp, int currentLevel, long[] minTotalXpForLevel, int maxLevel)
        {
            if (currentLevel >= maxLevel) return 1f;
            long floor = minTotalXpForLevel[currentLevel];
            long ceil = minTotalXpForLevel[currentLevel + 1];
            long span = ceil - floor;
            if (span <= 0) return 1f;
            return Mathf.Clamp01((float)(currentTotalXp - floor) / span);
        }

        public static int LevelFromTotalXp(long totalXp, long[] minTotalXpForLevel, int maxLevel)
        {
            for (int lvl = maxLevel; lvl >= 1; lvl--)
            {
                if (totalXp >= minTotalXpForLevel[lvl]) return lvl;
            }
            return 1;
        }

        /// <summary>Wiki approximation for total XP to reach level L (fast, no table).</summary>
        public static double ApproxMinTotalXpForLevel(int level)
        {
            if (level <= 1) return 0;
            return 720.0 * Math.Pow(2, level / 7.0) + (level * (level - 1)) / 8.0 - 795.0;
        }

        /// <summary>Wiki: incremental XP from level L to L+1 ≈ 75 * 2^(L/7).</summary>
        public static double ApproxXpForNextLevel(int currentLevel)
        {
            return 75.0 * Math.Pow(2, currentLevel / 7.0);
        }
    }
}
