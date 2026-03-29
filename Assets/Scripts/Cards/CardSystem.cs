using System;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Core;

namespace RagnaRune.Cards
{
    [Serializable]
    public class EquipmentSlot
    {
        public string SlotName;
        public int SlotMask;         // bitmask matching CardData.AllowedSlotMask
        public CardData[] Cards;     // socketed cards (array size = socket count)
        public int Sockets = 1;

        public EquipmentSlot(string name, int mask, int sockets = 1)
        {
            SlotName = name;
            SlotMask = mask;
            Sockets  = sockets;
            Cards    = new CardData[sockets];
        }

        public bool TryInsert(CardData card, int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= Sockets) return false;
            if (Cards[socketIndex] != null) return false;           // already occupied
            if ((card.AllowedSlotMask & SlotMask) == 0) return false; // wrong slot type
            Cards[socketIndex] = card;
            return true;
        }

        public CardData Remove(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= Sockets) return null;
            var c = Cards[socketIndex];
            Cards[socketIndex] = null;
            return c;
        }
    }

    /// <summary>
    /// Card inventory and equipment system. Attach to Player.
    /// Applies card bonuses to CharacterStats on demand.
    /// </summary>
    public class CardSystem : MonoBehaviour
    {
        public event Action OnCardsChanged;

        [Header("Equipment Slots")]
        public EquipmentSlot WeaponSlot    = new("Weapon",    0b0001, 4);
        public EquipmentSlot ArmorSlot     = new("Armor",     0b0010, 4);
        public EquipmentSlot AccessorySlot = new("Accessory", 0b0100, 2);

        [Header("Card Inventory")]
        public List<CardData> CardInventory = new();

        private List<EquipmentSlot> _allSlots;

        private void Awake()
        {
            _allSlots = new List<EquipmentSlot> { WeaponSlot, ArmorSlot, AccessorySlot };

            // Seed a few starter cards for quick prototyping
#if UNITY_EDITOR
            CardInventory.Add(CardData.MakePoring());
            CardInventory.Add(CardData.MakeSwordfish());
            CardInventory.Add(CardData.MakeOrcLord());
#endif
        }

        // ── Equipping ─────────────────────────────────────────────────────────

        public bool EquipCard(CardData card, EquipmentSlot slot, int socketIndex)
        {
            if (!CardInventory.Contains(card)) return false;
            if (slot.TryInsert(card, socketIndex))
            {
                CardInventory.Remove(card);
                OnCardsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void UnequipCard(EquipmentSlot slot, int socketIndex)
        {
            var card = slot.Remove(socketIndex);
            if (card != null)
            {
                CardInventory.Add(card);
                OnCardsChanged?.Invoke();
            }
        }

        public void AddToInventory(CardData card)
        {
            CardInventory.Add(card);
            Debug.Log($"[CardSystem] Picked up: {card.CardName}");
            OnCardsChanged?.Invoke();
        }

        // ── Stat Application ─────────────────────────────────────────────────

        /// <summary>
        /// Apply all equipped card bonuses to a CharacterStats block.
        /// Call after RecalcDerived to layer card stats on top.
        /// </summary>
        public void ApplyCardBonuses(CharacterStats stats)
        {
            foreach (var slot in _allSlots)
            {
                foreach (var card in slot.Cards)
                {
                    if (card == null) continue;
                    ApplySingleCard(card, stats);
                    if (card.HasSecondaryEffect)
                        ApplyEffect(card.SecondaryEffect, card.SecondaryValue, card.TargetElement, stats);
                }
            }
        }

        private void ApplySingleCard(CardData card, CharacterStats stats)
        {
            ApplyEffect(card.Effect, card.EffectValue, card.TargetElement, stats);
        }

        private void ApplyEffect(CardEffect effect, int value, Element targetElement, CharacterStats stats)
        {
            switch (effect)
            {
                case CardEffect.BonusATK:       stats.BonusATK   += value; break;
                case CardEffect.BonusDEF:       stats.BonusDEF   += value; break;
                case CardEffect.BonusMaxHP:     stats.BonusMaxHP += value; break;
                case CardEffect.BonusASPD:      stats.BonusASPD  += value * 0.01f; break;
                case CardEffect.BonusHIT:       stats.BonusHIT   += value; break;
                case CardEffect.BonusFLEE:      stats.BonusFLEE  += value; break;
                case CardEffect.ElementWeapon:  stats.WeaponElement = targetElement; break;
                case CardEffect.ElementBody:    stats.BodyElement   = targetElement; break;
                // BonusVsSmall/Medium/Large/Element handled in CombatCalculator
                default: break;
            }
        }

        // ── Per-hit bonus lookup (called by CombatCalculator) ─────────────────

        public int GetBonusDamageVsSize(int size)
        {
            int bonus = 0;
            foreach (var slot in _allSlots)
                foreach (var card in slot.Cards)
                {
                    if (card == null) continue;
                    if (size == 0 && card.Effect == CardEffect.BonusVsSmall)  bonus += card.EffectValue;
                    if (size == 1 && card.Effect == CardEffect.BonusVsMedium) bonus += card.EffectValue;
                    if (size == 2 && card.Effect == CardEffect.BonusVsLarge)  bonus += card.EffectValue;
                }
            return bonus;
        }

        public int GetIgnoreDefPercent()
        {
            int pct = 0;
            foreach (var slot in _allSlots)
                foreach (var card in slot.Cards)
                {
                    if (card == null) continue;
                    if (card.Effect == CardEffect.IgnoreDefPercent) pct += card.EffectValue;
                    if (card.HasSecondaryEffect && card.SecondaryEffect == CardEffect.IgnoreDefPercent)
                        pct += card.SecondaryValue;
                }
            return Mathf.Min(pct, 100);
        }
    }
}
