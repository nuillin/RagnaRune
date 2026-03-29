using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RagnaRune.Combat;
using RagnaRune.Core;

namespace RagnaRune.UI
{
    /// <summary>
    /// Wires combat events to the HUD.
    /// Skill XP / levels: use <see cref="SkillsHUD"/> (subscribes to SkillSystem events).
    /// Requires TextMeshPro (install via Package Manager).
    ///
    /// Scene setup:
    ///   Canvas
    ///     └─ HUD
    ///          ├─ HPBar   (Slider)
    ///          ├─ SPBar   (Slider)
    ///          ├─ HPText  (TMP_Text)
    ///          ├─ SPText  (TMP_Text)
    ///          ├─ DamageNumberPrefab  (TMP_Text, spawned at world pos)
    ///          ├─ TargetPanel
    ///          │    ├─ TargetNameText
    ///          │    ├─ TargetHPBar
    ///          │    └─ TargetElementText
    ///          └─ ZenyText
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Player Vitals")]
        public Slider    HPBar;
        public Slider    SPBar;
        public TMP_Text  HPText;
        public TMP_Text  SPText;

        [Header("Target Panel")]
        public GameObject TargetPanel;
        public TMP_Text   TargetNameText;
        public Slider     TargetHPBar;
        public TMP_Text   TargetElementText;

        [Header("Currency")]
        public TMP_Text ZenyText;

        [Header("Damage Numbers")]
        public GameObject DamageNumberPrefab;   // TMP_Text with float-up animation
        public Canvas     WorldCanvas;           // overlay canvas in world space

        [Header("Notifications")]
        public TMP_Text   NotificationText;      // combat messages (enemy defeated, death)

        // ── Refs ──────────────────────────────────────────────────────────────
        private CombatManager _combat;

        // ─────────────────────────────────────────────────────────────────────

        public void Initialise(CombatManager combat)
        {
            _combat = combat;

            _combat.OnPlayerDamageDealt   += OnPlayerDamageDealt;
            _combat.OnPlayerDamageTaken   += OnPlayerDamageTaken;
            _combat.OnEnemyDefeated       += OnEnemyDefeated;
            _combat.OnPlayerDefeated      += OnPlayerDefeated;
            _combat.OnStateChanged        += OnCombatStateChanged;

            UpdateVitals();
            HideTargetPanel();
        }

        // ── Vitals ────────────────────────────────────────────────────────────

        private void Update()
        {
            UpdateVitals();

            // Live-update target HP if in combat
            if (_combat.CurrentTarget != null && _combat.CurrentTarget.IsAlive())
            {
                var stats = _combat.CurrentTarget.Stats;
                TargetHPBar.value = (float)stats.CurrentHP / stats.MaxHP;
            }

            if (ZenyText != null)
                ZenyText.text = $"Z {Managers.GameManager.Instance?.GetZeny():N0}";
        }

        private void UpdateVitals()
        {
            if (_combat?.PlayerStats == null) return;
            var s = _combat.PlayerStats;
            if (HPBar) HPBar.value = (float)s.CurrentHP / Mathf.Max(s.MaxHP, 1);
            if (SPBar) SPBar.value = (float)s.CurrentSP / Mathf.Max(s.MaxSP, 1);
            if (HPText) HPText.text = $"{s.CurrentHP} / {s.MaxHP}";
            if (SPText) SPText.text = $"{s.CurrentSP} / {s.MaxSP}";
        }

        // ── Damage Numbers ────────────────────────────────────────────────────

        private void OnPlayerDamageDealt(DamageResult result)
        {
            if (result.FinalDamage <= 0) { SpawnText(_combat.CurrentTarget?.transform, "MISS", Color.white); return; }

            Color col = result.IsCritical ? Color.yellow : Color.red;
            string txt = result.IsCritical ? $"CRIT! {result.FinalDamage}" : result.FinalDamage.ToString();

            if (_combat.CurrentTarget != null)
                SpawnText(_combat.CurrentTarget.transform, txt, col);
        }

        private void OnPlayerDamageTaken(DamageResult result)
        {
            if (result.FinalDamage <= 0) return;
            SpawnText(null, $"-{result.FinalDamage}", new Color(1f, 0.5f, 0f));
        }

        private void SpawnText(Transform worldTarget, string text, Color color)
        {
            if (DamageNumberPrefab == null || WorldCanvas == null) return;

            Vector3 worldPos = worldTarget ? worldTarget.position + Vector3.up * 0.5f
                                           : _combat.transform.position + Vector3.up * 0.5f;

            var go  = Instantiate(DamageNumberPrefab, worldPos, Quaternion.identity, WorldCanvas.transform);
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp) { tmp.text = text; tmp.color = color; }

            StartCoroutine(FloatAndFade(go));
        }

        private IEnumerator FloatAndFade(GameObject go)
        {
            var tmp = go.GetComponent<TMP_Text>();
            float t = 0f;
            Vector3 start = go.transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                go.transform.position = start + Vector3.up * (t * 1.2f);
                if (tmp) tmp.alpha = 1f - t;
                yield return null;
            }
            Destroy(go);
        }

        // ── Target Panel ──────────────────────────────────────────────────────

        private void OnCombatStateChanged(CombatState state)
        {
            if (state == CombatState.InCombat && _combat.CurrentTarget != null)
            {
                ShowTargetPanel(_combat.CurrentTarget);
            }
            else HideTargetPanel();
        }

        private void ShowTargetPanel(Enemy.EnemyController enemy)
        {
            if (TargetPanel) TargetPanel.SetActive(true);
            if (TargetNameText)    TargetNameText.text    = enemy.Data?.EnemyName ?? "Unknown";
            if (TargetElementText) TargetElementText.text = enemy.Stats.BodyElement.ToString();
        }

        private void HideTargetPanel()
        {
            if (TargetPanel) TargetPanel.SetActive(false);
        }

        // ── Notifications ─────────────────────────────────────────────────────

        private void ShowNotification(string message)
        {
            if (NotificationText == null) return;
            StopAllCoroutines();
            NotificationText.text  = message;
            NotificationText.alpha = 1f;
            StartCoroutine(FadeNotification());
        }

        private IEnumerator FadeNotification()
        {
            yield return new WaitForSeconds(2f);
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime; NotificationText.alpha = 1f - t; yield return null; }
        }

        private void OnEnemyDefeated(Enemy.EnemyController enemy)
        {
            HideTargetPanel();
            ShowNotification($"{enemy.Data?.EnemyName ?? "Enemy"} defeated!");
        }

        private void OnPlayerDefeated() => ShowNotification("You died... respawning.");

        // ── Cleanup ───────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (_combat == null) return;
            _combat.OnPlayerDamageDealt -= OnPlayerDamageDealt;
            _combat.OnPlayerDamageTaken -= OnPlayerDamageTaken;
            _combat.OnEnemyDefeated     -= OnEnemyDefeated;
            _combat.OnPlayerDefeated    -= OnPlayerDefeated;
            _combat.OnStateChanged      -= OnCombatStateChanged;
        }
    }
}
