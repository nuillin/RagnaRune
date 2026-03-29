using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Core;
using RagnaRune.Cards;

namespace RagnaRune.Enemy
{
    [System.Serializable]
    public class CardDrop
    {
        public CardData Card;
        [Range(0f, 1f)] public float DropRate = 0.01f;
    }

    [System.Serializable]
    public class ItemDrop
    {
        public string ItemName;
        public int ZenyValue = 10;
        [Range(0f, 1f)] public float DropRate = 0.3f;
    }

    /// <summary>
    /// Defines a monster type. Attach to a ScriptableObject or embed directly.
    /// Create via: Assets > Create > RagnaRune > EnemyData
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "RagnaRune/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyName = "Poring";
        public Sprite Sprite;

        [Header("Stats")]
        public CharacterStats BaseStats = new CharacterStats
        {
            STR = 4, AGI = 4, VIT = 5, INT = 0, DEX = 4, LUK = 30,
            BaseLevel = 1, Size = 1,
            BodyElement = Element.Water,
            WeaponElement = Element.Neutral,
        };

        [Header("AI")]
        public float AggroRange    = 4f;   // metres
        public float AttackRange   = 1.2f;
        public float WanderRadius  = 5f;
        public float MoveSpeed     = 1.5f;

        [Header("Loot")]
        public int BaseZenyDrop   = 5;
        public int MaxZenyDrop    = 20;
        public List<CardDrop>  CardDrops  = new();
        public List<ItemDrop>  ItemDrops  = new();

        [Header("Respawn")]
        public float RespawnDelay = 10f;

        // ── Built-in presets ──────────────────────────────────────────────────

        public static EnemyData MakePoring()
        {
            var d = CreateInstance<EnemyData>();
            d.EnemyName    = "Poring";
            d.BaseStats    = new CharacterStats
            {
                STR = 4, AGI = 4, VIT = 5, INT = 0, DEX = 4, LUK = 30,
                BaseLevel = 1, Size = 1,
                BodyElement = Element.Water,
            };
            d.AggroRange   = 3f;
            d.MoveSpeed    = 1.5f;
            d.BaseZenyDrop = 5;
            d.MaxZenyDrop  = 20;
            d.CardDrops.Add(new CardDrop { Card = CardData.MakePoring(), DropRate = 0.01f });
            return d;
        }

        public static EnemyData MakeSwordfish()
        {
            var d = CreateInstance<EnemyData>();
            d.EnemyName    = "Swordfish";
            d.BaseStats    = new CharacterStats
            {
                STR = 10, AGI = 10, VIT = 10, INT = 0, DEX = 10, LUK = 5,
                BaseLevel = 10, Size = 1,
                BodyElement = Element.Water,
            };
            d.AggroRange   = 6f;
            d.MoveSpeed    = 2.5f;
            d.BaseZenyDrop = 50;
            d.MaxZenyDrop  = 120;
            d.CardDrops.Add(new CardDrop { Card = CardData.MakeSwordfish(), DropRate = 0.05f });
            return d;
        }
    }
}
