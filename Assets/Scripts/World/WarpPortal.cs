using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RagnaRune.World
{
    public enum WarpType
    {
        SameScene,      // Teleport to a position in the current scene
        LoadScene,      // Load a new scene by name
        LoadSceneAsync, // Async scene load with loading screen support
    }

    /// <summary>
    /// RO-style warp portal. Player walks into the trigger to activate.
    /// Supports same-scene teleport and cross-scene loading.
    ///
    /// Scene setup:
    ///   - Add a Collider2D (Is Trigger) to define the warp zone
    ///   - Optionally assign a sprite for the portal visual
    ///   - Tag the player GameObject as "Player"
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WarpPortal : MonoBehaviour
    {
        [Header("Warp Type")]
        public WarpType Type = WarpType.SameScene;

        [Header("Same-Scene Destination")]
        [Tooltip("The spawn point to teleport to (used when Type = SameScene).")]
        public Transform Destination;

        [Header("Scene Load")]
        [Tooltip("Scene name to load (used when Type = LoadScene / LoadSceneAsync).")]
        public string DestinationScene;
        [Tooltip("PlayerSpawnId in the destination scene's ZoneManager (matched by name).")]
        public string DestinationSpawnId;

        [Header("Activation")]
        [Tooltip("Walk-in activates immediately. If false, player must press InteractKey.")]
        public bool ActivateOnEnter = true;
        public KeyCode InteractKey = KeyCode.F;
        [Tooltip("Seconds of fade-out before loading.")]
        public float FadeOutSeconds = 0.4f;

        [Header("Display")]
        public string PortalLabel = "";

        // ── Private ───────────────────────────────────────────────────────────
        private bool _playerInside;
        private Transform _playerTransform;
        private bool _activated;

        // ─────────────────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerTransform = other.transform;
            _playerInside    = true;

            if (ActivateOnEnter) Activate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside || ActivateOnEnter || _activated) return;
            if (Input.GetKeyDown(InteractKey)) Activate();
        }

        // ── Warp Logic ────────────────────────────────────────────────────────

        private void Activate()
        {
            if (_activated) return;
            _activated = true;
            StartCoroutine(WarpRoutine());
        }

        private IEnumerator WarpRoutine()
        {
            // Freeze player movement before warp
            var rb = _playerTransform?.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Optional: hook into a ScreenFader singleton here
            yield return new WaitForSeconds(FadeOutSeconds);

            switch (Type)
            {
                case WarpType.SameScene:
                    DoSameSceneWarp();
                    break;

                case WarpType.LoadScene:
                    StoreDestination();
                    SceneManager.LoadScene(DestinationScene);
                    break;

                case WarpType.LoadSceneAsync:
                    StoreDestination();
                    yield return SceneManager.LoadSceneAsync(DestinationScene);
                    break;
            }

            _activated = false;
        }

        private void DoSameSceneWarp()
        {
            if (_playerTransform == null || Destination == null) return;
            _playerTransform.position = Destination.position;
            Debug.Log($"[Warp] Teleported to {Destination.name}");
        }

        private void StoreDestination()
        {
            // Pass the spawn ID to the destination scene via a static/DontDestroyOnLoad carrier.
            ZoneManager.PendingSpawnId = DestinationSpawnId;
            Debug.Log($"[Warp] Loading scene '{DestinationScene}' → spawn '{DestinationSpawnId}'");
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.2f, 1f, 0.35f);
            var col = GetComponent<Collider2D>();
            if (col != null) Gizmos.DrawCube(col.bounds.center, col.bounds.size);

            if (Destination != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, Destination.position);
                Gizmos.DrawWireSphere(Destination.position, 0.3f);
            }
        }
#endif
    }

    // ── Zone Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight static carrier for cross-scene spawn data.
    /// ZoneManager in each scene reads PendingSpawnId on Start.
    /// </summary>
    public static class ZoneManager
    {
        public static string PendingSpawnId;
    }

    /// <summary>
    /// Place one per scene. On Start, finds the matching spawn point
    /// and moves the player there (reading ZoneManager.PendingSpawnId).
    /// </summary>
    public class SceneSpawnController : MonoBehaviour
    {
        [Tooltip("Maps spawn IDs to spawn point transforms.")]
        public SpawnPoint[] SpawnPoints;

        private void Start()
        {
            if (string.IsNullOrEmpty(ZoneManager.PendingSpawnId)) return;
            foreach (var sp in SpawnPoints)
            {
                if (sp.Id != ZoneManager.PendingSpawnId) continue;
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) player.transform.position = sp.Position.position;
                break;
            }
            ZoneManager.PendingSpawnId = null;
        }
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public string    Id;
        public Transform Position;
    }
}
