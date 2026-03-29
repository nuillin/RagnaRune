using UnityEngine;

namespace RagnaRune.Core
{
    public enum Element
    {
        Neutral = 0,
        Water   = 1,
        Earth   = 2,
        Fire    = 3,
        Wind    = 4,
        Poison  = 5,
        Holy    = 6,
        Shadow  = 7,
        Ghost   = 8,
        Undead  = 9
    }

    /// <summary>
    /// RO-style element damage chart.
    /// chart[attacker, defender] = damage multiplier in percent (100 = normal damage).
    /// </summary>
    public static class ElementChart
    {
        // Rows = attacker element, Columns = defender element
        // Order: Neutral, Water, Earth, Fire, Wind, Poison, Holy, Shadow, Ghost, Undead
        private static readonly int[,] Chart = new int[10, 10]
        {
            //        Neut  Watr  Erth  Fire  Wind  Posn  Holy  Shdw  Ghst  Undd
            /* Neutral */ { 100,  100,  100,  100,  100,  100,   75,  100,   50,  100 },
            /* Water   */ { 100,   25,  100,  175,   50,  100,  100,  100,   50,   75 },
            /* Earth   */ { 100,  100,   25,  100,  175,  100,  100,  100,   50,   75 },
            /* Fire    */ { 100,   50,  175,   25,  100,  100,  100,  100,   50,  125 },
            /* Wind    */ { 100,  175,   50,  100,   25,  100,  100,  100,   50,   75 },
            /* Poison  */ { 100,  100,  100,  100,  100,    0,  100,  100,   50,  100 },
            /* Holy    */ { 100,  100,  100,  100,  100,  100,  100,  175,   75,  200 },
            /* Shadow  */ { 100,  100,  100,  100,  100,   50,  175,  100,   75,  100 },
            /* Ghost   */ {  25,  100,  100,  100,  100,  100,  100,  100,  175,   75 },
            /* Undead  */ { 100,  100,  100,  125,  100,  100,  200,  100,   75,    0 },
        };

        public static float GetMultiplier(Element attacker, Element defender)
        {
            return Chart[(int)attacker, (int)defender] / 100f;
        }

        public static string GetElementColor(Element element)
        {
            return element switch
            {
                Element.Fire    => "#FF4500",
                Element.Water   => "#1E90FF",
                Element.Wind    => "#7CFC00",
                Element.Earth   => "#8B4513",
                Element.Holy    => "#FFD700",
                Element.Shadow  => "#800080",
                Element.Poison  => "#32CD32",
                Element.Ghost   => "#708090",
                Element.Undead  => "#2F2F2F",
                _               => "#FFFFFF",
            };
        }
    }
}
