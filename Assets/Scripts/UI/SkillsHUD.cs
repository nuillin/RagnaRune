using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RagnaRune.Skills;

namespace RagnaRune.UI
{
    /// <summary>
    /// Compact skills list driven by <see cref="SkillSystem.OnXPGain"/> and <see cref="SkillSystem.OnLevelUp"/>.
    /// Scene: Canvas → SkillsHUD → RowParent + optional banners.
    /// Row prefab: <see cref="SkillRowView"/> with Name / Level / Total XP / Slider assigned.
    /// </summary>
    public class SkillsHUD : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Leave empty to find a SkillSystem in the scene at runtime.")]
        public SkillSystem Skills;

        [Header("List")]
        public Transform RowParent;
        public GameObject RowPrefab;

        [Header("Level-up banner")]
        public TMP_Text LevelUpText;
        public float LevelUpVisibleSeconds = 2.5f;

        [Header("XP gain flash")]
        public Color XpFlashColor = new(1f, 0.92f, 0.4f, 0.45f);
        public float XpFlashHoldSeconds = 0.12f;
        public float XpFlashFadeSeconds = 0.35f;

        private readonly Dictionary<SkillType, SkillRowView> _rows = new();
        private readonly Dictionary<SkillType, Coroutine> _flashRoutines = new();
        private Coroutine _levelUpBannerRoutine;

        private void Start()
        {
            if (Skills == null)
                Skills = FindObjectOfType<SkillSystem>();

            if (Skills == null)
            {
                Debug.LogWarning("[SkillsHUD] No SkillSystem found.");
                return;
            }

            Skills.OnXPGain += HandleXPGain;
            Skills.OnLevelUp += HandleLevelUp;
            Skills.OnSkillsRestored += HandleSkillsRestored;
            BuildRows();
        }

        /// <summary>Call if the player / SkillSystem is spawned after this HUD (e.g. from pool).</summary>
        public void Bind(SkillSystem skills)
        {
            if (Skills == skills) return;
            Unsubscribe();
            Skills = skills;
            if (Skills == null) return;
            Skills.OnXPGain += HandleXPGain;
            Skills.OnLevelUp += HandleLevelUp;
            Skills.OnSkillsRestored += HandleSkillsRestored;
            if (_rows.Count == 0 && RowParent != null && RowPrefab != null)
                BuildRows();
            else
            {
                foreach (var kv in _rows)
                    kv.Value.Refresh(Skills.GetSkill(kv.Key));
            }
        }

        private void BuildRows()
        {
            if (RowParent == null || RowPrefab == null || Skills == null) return;
            foreach (Transform c in RowParent)
                Destroy(c.gameObject);
            _rows.Clear();

            foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
            {
                var go = Instantiate(RowPrefab, RowParent);
                var view = go.GetComponent<SkillRowView>();
                if (view == null)
                {
                    Debug.LogError("[SkillsHUD] RowPrefab must have a SkillRowView component.");
                    Destroy(go);
                    continue;
                }
                view.Initialise(type);
                view.Refresh(Skills.GetSkill(type));
                _rows[type] = view;
            }

            if (LevelUpText)
            {
                LevelUpText.alpha = 0f;
                LevelUpText.gameObject.SetActive(false);
            }
        }

        private void HandleXPGain(SkillType type, long amount)
        {
            if (!_rows.TryGetValue(type, out var row) || Skills == null) return;
            row.Refresh(Skills.GetSkill(type));
            FlashRow(type, row);
        }

        private void HandleLevelUp(SkillType type, int newLevel)
        {
            if (!_rows.TryGetValue(type, out var row) || Skills == null) return;
            row.Refresh(Skills.GetSkill(type));
            ShowLevelUp($"{type} level up! → {newLevel}");
        }

        private void HandleSkillsRestored()
        {
            if (Skills == null) return;
            foreach (var kv in _rows)
                kv.Value.Refresh(Skills.GetSkill(kv.Key));
        }

        private void FlashRow(SkillType type, SkillRowView row)
        {
            if (row.HighlightGraphic == null) return;
            if (_flashRoutines.TryGetValue(type, out var running) && running != null)
                StopCoroutine(running);
            _flashRoutines[type] = StartCoroutine(FlashRoutine(row));
        }

        private IEnumerator FlashRoutine(SkillRowView row)
        {
            row.SetHighlight(XpFlashColor);
            yield return new WaitForSeconds(XpFlashHoldSeconds);
            float t = 0f;
            Color start = row.HighlightGraphic.color;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, XpFlashFadeSeconds);
                var c = start;
                c.a = Mathf.Lerp(start.a, 0f, t);
                row.SetHighlight(c);
                yield return null;
            }
            row.SetHighlight(new Color(0, 0, 0, 0));
        }

        private void ShowLevelUp(string message)
        {
            if (LevelUpText == null) return;
            if (_levelUpBannerRoutine != null)
                StopCoroutine(_levelUpBannerRoutine);
            LevelUpText.gameObject.SetActive(true);
            LevelUpText.text = message;
            LevelUpText.alpha = 1f;
            _levelUpBannerRoutine = StartCoroutine(LevelUpBannerRoutine());
        }

        private IEnumerator LevelUpBannerRoutine()
        {
            yield return new WaitForSeconds(LevelUpVisibleSeconds);
            float t = 0f;
            while (t < 1f && LevelUpText != null)
            {
                t += Time.unscaledDeltaTime * 2f;
                LevelUpText.alpha = 1f - t;
                yield return null;
            }
            if (LevelUpText != null)
            {
                LevelUpText.alpha = 0f;
                LevelUpText.gameObject.SetActive(false);
            }
            _levelUpBannerRoutine = null;
        }

        private void Unsubscribe()
        {
            if (Skills == null) return;
            Skills.OnXPGain -= HandleXPGain;
            Skills.OnLevelUp -= HandleLevelUp;
            Skills.OnSkillsRestored -= HandleSkillsRestored;
        }

        private void OnDestroy() => Unsubscribe();
    }
}
