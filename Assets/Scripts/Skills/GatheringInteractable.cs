using UnityEngine;

namespace RagnaRune.Skills
{
    /// <summary>
    /// Example world object: stand in trigger, press InteractKey, gain skill XP if level requirement is met.
    /// Add a CircleCollider2D (Is Trigger) and assign the skill + amounts in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GatheringInteractable : MonoBehaviour
    {
        [Header("Skill")]
        public SkillType Skill;
        public long XpReward = 25;
        [Tooltip("Minimum skill level to use this node.")]
        public int RequiredLevel = 1;

        [Header("Input")]
        public KeyCode InteractKey = KeyCode.E;
        [Tooltip("Seconds before this node can be used again.")]
        public float CooldownSeconds = 2f;

        private float _cooldownEnd;
        private SkillSystem _playerSkills;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<SkillSystem>(out var ss)) _playerSkills = ss;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<SkillSystem>() == _playerSkills) _playerSkills = null;
        }

        private void Update()
        {
            if (_playerSkills == null || Time.time < _cooldownEnd) return;
            if (!Input.GetKeyDown(InteractKey)) return;
            if (_playerSkills.GetLevel(Skill) < RequiredLevel)
            {
                Debug.Log($"[Gathering] Need {Skill} level {RequiredLevel}.");
                return;
            }
            _playerSkills.AwardXP(Skill, XpReward);
            _cooldownEnd = Time.time + CooldownSeconds;
        }
    }
}
