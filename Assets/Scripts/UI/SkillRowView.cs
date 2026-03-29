using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RagnaRune.Skills;

namespace RagnaRune.UI
{
    /// <summary>
    /// One row in <see cref="SkillsHUD"/>. Assign references on the row prefab (clearer than relying on child order).
    /// </summary>
    public class SkillRowView : MonoBehaviour
    {
        public TMP_Text NameText;
        public TMP_Text LevelText;
        public TMP_Text TotalXpText;
        public Slider   ProgressSlider;

        [Tooltip("Optional: tinted briefly on XP gain.")]
        public Graphic HighlightGraphic;

        public SkillType SkillType { get; private set; }

        public void Initialise(SkillType type)
        {
            SkillType = type;
            if (NameText) NameText.text = type.ToString();
        }

        public void Refresh(Skill skill)
        {
            if (LevelText) LevelText.text = skill.Level.ToString();
            if (TotalXpText) TotalXpText.text = skill.XP.ToString("N0");
            if (ProgressSlider)
            {
                ProgressSlider.value = skill.LevelProgress;
                ProgressSlider.interactable = false;
            }
        }

        public void SetHighlight(Color c)
        {
            if (HighlightGraphic == null) return;
            HighlightGraphic.color = c;
            HighlightGraphic.gameObject.SetActive(c.a > 0.01f);
        }
    }
}
