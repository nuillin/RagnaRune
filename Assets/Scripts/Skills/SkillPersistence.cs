using System;
using System.IO;
using UnityEngine;
using RagnaRune.Crafting;

namespace RagnaRune.Skills
{
    /// <summary>
    /// Writes / reads <see cref="SkillsSaveData"/> (skills + optional items) as JSON under <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static class SkillPersistence
    {
        public const string DefaultFileName = "player_skills.json";

        public static string DefaultPath =>
            Path.Combine(Application.persistentDataPath, DefaultFileName);

        public static void Save(SkillSystem skills, ItemInventory inventory = null, string path = null)
        {
            if (skills == null) return;
            path ??= DefaultPath;
            try
            {
                var data = skills.BuildSaveSnapshot();
                data.FormatVersion = SkillsSaveData.CurrentFormatVersion;
                if (inventory != null)
                    data.Items = inventory.BuildSaveEntries();
                else
                    data.Items = null;

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkillPersistence] Save failed: {e.Message}");
            }
        }

        /// <returns>True if a file existed and skills were applied.</returns>
        public static bool TryLoad(
            SkillSystem skills,
            ItemInventory inventory = null,
            CraftingCatalog catalog = null,
            string path = null)
        {
            if (skills == null) return false;
            path ??= DefaultPath;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SkillsSaveData>(json);
                if (data == null) return false;
                skills.ApplySaveSnapshot(data);

                if (inventory != null && data.Items != null)
                {
                    if (catalog == null && data.Items.Length > 0)
                    {
                        Debug.LogWarning("[SkillPersistence] Save file contains items but CraftingCatalog is not assigned; clearing inventory.");
                        inventory.ApplySaveEntries(System.Array.Empty<ItemStackSaveEntry>(), catalog);
                    }
                    else
                        inventory.ApplySaveEntries(data.Items, catalog);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkillPersistence] Load failed: {e.Message}");
                return false;
            }
        }

        public static void DeleteSave(string path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkillPersistence] Delete failed: {e.Message}");
            }
        }
    }
}
