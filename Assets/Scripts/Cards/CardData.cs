using UnityEngine;

namespace RagnaRune.Cards
{
    public enum CardRarity { Common, Uncommon, Rare, Unique }
    public enum CardEffect
    {
        BonusATK,
        BonusDEF,
        BonusMaxHP,
        BonusASPD,
        BonusHIT,
        BonusFLEE,
        BonusCRIT,
        ElementWeapon,      // Changes weapon element
        ElementBody,        // Changes body element
        BonusVsSmall,
        BonusVsMedium,
        BonusVsLarge,
        BonusVsElement,     // targetElement field used
        SPRegen,
        HPRegen,
        IgnoreDefPercent,
    }

    /// <summary>
    /// Defines a monster drop card (e.g. "Poring Card", "Swordfish Card").
    /// Create via: Assets > Create > RagnaRune > Card
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "RagnaRune/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string CardName;
        [TextArea(2, 4)] public string Description;
        public Sprite Icon;
        public CardRarity Rarity = CardRarity.Common;

        [Header("Drop Source")]
        public string MonsterName;
        [Range(0f, 1f)] public float DropRate = 0.01f;   // 1% default

        [Header("Primary Effect")]
        public CardEffect Effect;
        public int EffectValue = 10;           // e.g. +10 ATK
        public Element TargetElement;          // used by BonusVsElement / ElementWeapon / ElementBody

        [Header("Secondary Effect (optional)")]
        public bool HasSecondaryEffect = false;
        public CardEffect SecondaryEffect;
        public int SecondaryValue = 0;

        [Header("Socket Restriction")]
        // 0 = any slot, 1 = weapon, 2 = armor, 3 = accessory
        public int AllowedSlotMask = 0b0111;

        // ── Built-in Presets (call from Editor or code) ───────────────────────

        public static CardData MakePoring()
        {
            var c = CreateInstance<CardData>();
            c.CardName    = "Poring Card";
            c.MonsterName = "Poring";
            c.DropRate    = 0.01f;
            c.Rarity      = CardRarity.Common;
            c.Effect       = CardEffect.BonusMaxHP;
            c.EffectValue  = 50;
            c.Description  = "Socketed into accessory. Max HP +50.";
            c.AllowedSlotMask = 0b0100;
            return c;
        }

        public static CardData MakeSwordfish()
        {
            var c = CreateInstance<CardData>();
            c.CardName    = "Swordfish Card";
            c.MonsterName = "Swordfish";
            c.DropRate    = 0.05f;
            c.Rarity      = CardRarity.Common;
            c.Effect       = CardEffect.ElementWeapon;
            c.TargetElement = Element.Water;
            c.Description  = "Socketed into weapon. Adds Water element to weapon.";
            c.AllowedSlotMask = 0b0001;
            return c;
        }

        public static CardData MakeOrcLord()
        {
            var c = CreateInstance<CardData>();
            c.CardName    = "Orc Lord Card";
            c.MonsterName = "Orc Lord";
            c.DropRate    = 0.002f;
            c.Rarity      = CardRarity.Rare;
            c.Effect       = CardEffect.BonusATK;
            c.EffectValue  = 25;
            c.HasSecondaryEffect = true;
            c.SecondaryEffect    = CardEffect.IgnoreDefPercent;
            c.SecondaryValue     = 20;
            c.Description  = "Socketed into weapon. ATK +25. Ignore 20% of DEF.";
            c.AllowedSlotMask = 0b0001;
            return c;
        }
    }
}
