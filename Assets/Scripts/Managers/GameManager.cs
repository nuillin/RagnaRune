using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RagnaRune.Enemy;
using RagnaRune.Combat;
using RagnaRune.Cards;
using RagnaRune.Crafting;
using RagnaRune.Skills;

namespace RagnaRune.Managers
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public EnemyData Data;
        public Transform SpawnPoint;
        public int Count = 3;
    }

    /// <summary>
    /// Singleton game manager. Place one in the root of your scene.
    /// Drag spawn entries and player references into the Inspector.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Player")]
        public GameObject PlayerPrefab;
        public Transform  PlayerSpawnPoint;

        [Header("Enemy Spawning")]
        public GameObject EnemyPrefab;
        public List<EnemySpawnEntry> SpawnEntries = new();
        public float RespawnDelay = 10f;

        [Header("Zone Info")]
        public string ZoneName = "Prontera Field";

        [Header("Save")]
        [Tooltip("Load skills + items after the player spawns; save on quit / pause (mobile).")]
        public bool PersistSkills = true;

        [Tooltip("Maps saved item ids to CraftingItem assets when loading inventory.")]
        public CraftingCatalog ItemCatalog;

        // ── Runtime ───────────────────────────────────────────────────────────
        private GameObject     _playerGO;
        private CombatManager  _playerCombat;
        private SkillSystem    _playerSkills;
        private ItemInventory  _playerInventory;
        private Transform      _playerTransform;

        private List<EnemyController> _activeEnemies = new();
        private int _zeny = 0;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SpawnPlayer();
            if (PersistSkills && _playerGO != null)
            {
                _playerGO.TryGetComponent(out _playerSkills);
                _playerGO.TryGetComponent(out _playerInventory);
                if (_playerSkills != null)
                    SkillPersistence.TryLoad(_playerSkills, _playerInventory, ItemCatalog);
            }
            SpawnAllEnemies();
        }

        private void OnApplicationQuit() => SaveSkillsIfNeeded();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveSkillsIfNeeded();
        }

        private void OnDestroy()
        {
            // Editor / domain reload: ensures a write when OnApplicationQuit is unreliable.
            SaveSkillsIfNeeded();
        }

        private void SaveSkillsIfNeeded()
        {
            if (!PersistSkills || _playerSkills == null) return;
            SkillPersistence.Save(_playerSkills, _playerInventory);
        }

        // ── Player ────────────────────────────────────────────────────────────

        private void SpawnPlayer()
        {
            if (PlayerPrefab == null) { Debug.LogError("[GameManager] PlayerPrefab not assigned!"); return; }
            Vector3 pos = PlayerSpawnPoint ? PlayerSpawnPoint.position : Vector3.zero;
            _playerGO = Instantiate(PlayerPrefab, pos, Quaternion.identity);
            _playerTransform = _playerGO.transform;
            _playerCombat    = _playerGO.GetComponent<CombatManager>();
            _playerSkills    = _playerGO.GetComponent<SkillSystem>();
            _playerInventory = _playerGO.GetComponent<ItemInventory>();
        }

        // ── Enemy Spawning ────────────────────────────────────────────────────

        private void SpawnAllEnemies()
        {
            foreach (var entry in SpawnEntries)
                for (int i = 0; i < entry.Count; i++)
                    SpawnEnemy(entry.Data, entry.SpawnPoint?.position ?? RandomOffset());
        }

        private void SpawnEnemy(EnemyData data, Vector3 position)
        {
            if (EnemyPrefab == null || data == null) return;

            // Scatter within a small radius around the spawn point
            Vector2 rand = Random.insideUnitCircle * 2f;
            position += new Vector3(rand.x, rand.y, 0);

            var go     = Instantiate(EnemyPrefab, position, Quaternion.identity);
            var ctrl   = go.GetComponent<EnemyController>();
            if (ctrl == null) { Destroy(go); return; }

            ctrl.Initialise(data, _playerTransform, _playerCombat);
            ctrl.OnDied += OnEnemyDied;
            _activeEnemies.Add(ctrl);
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            _activeEnemies.Remove(enemy);

            // Award Zeny
            if (enemy.Data != null)
            {
                int zeny = Random.Range(enemy.Data.BaseZenyDrop, enemy.Data.MaxZenyDrop + 1);
                AddZeny(zeny);
                Debug.Log($"[GameManager] +{zeny} Zeny (total: {_zeny})");
            }

            // Schedule respawn
            if (enemy.Data != null)
                StartCoroutine(RespawnAfterDelay(enemy.Data, enemy.transform.position, RespawnDelay));
        }

        private IEnumerator RespawnAfterDelay(EnemyData data, Vector3 pos, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnEnemy(data, pos);
        }

        // ── Zeny (currency) ───────────────────────────────────────────────────

        public void AddZeny(int amount)  { _zeny += amount; }
        public bool SpendZeny(int amount)
        {
            if (_zeny < amount) return false;
            _zeny -= amount;
            return true;
        }
        public int GetZeny() => _zeny;

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector3 RandomOffset() =>
            new(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0);
    }
}
