using System.IO;
using UnityEngine;

namespace Lab6
{
    public static class Lab6SaveSystem
    {
        private const string FileName = "lab6_save.json";

        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(Path)) return new SaveData();
                string json = File.ReadAllText(Path);
                if (string.IsNullOrWhiteSpace(json)) return new SaveData();
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Lab6] Failed to load save: {e.Message}");
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(Path, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Lab6] Failed to save: {e.Message}");
            }
        }

        public static string GetSavePath() => Path;
    }
}
