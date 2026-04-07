using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class EntityDictionary : MonoBehaviour
{
    public Dictionary<int, string> spawns;
    public Dictionary<int, string> tiles;
    void Start()
    {
        spawns = LoadFromStream("data/spawns.gon", true);
        tiles = LoadFromStream("data/tiles.gon");
        EditorManager.Instance.EntitiesLoaded = true;
        EditorManager.Instance.LoadToolbox();
        spawns.Add(-2, "Unset");
    }

    Dictionary<int, string> LoadFromFile(string loc)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(loc);

        if (textAsset != null)
        {
            return LevelDataParser.ExtractNames(textAsset.ToString());
        }
        else
        {
            Debug.LogError("Text file not found in Resources folder!");
            return null;
        }
    }

    // Written by Claude
    // Loads a .gon entity definition file from StreamingAssets, then checks for a
    // matching .append file (e.g. spawns.gon.append) and merges any additional entries.
    // Users can place .append files in StreamingAssets to add custom entities without
    // modifying the base game files. Custom sprites should be placed in the matching
    // subfolder (e.g. StreamingAssets/spawns/) — they are picked up automatically.
    public Dictionary<int, string> LoadFromStream(string name, bool addRandom = false)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, name);
        filePath = filePath.Replace('\\', '/');

        if (!File.Exists(filePath))
        {
            string msg = $"Could not find required data file: {name}";
            Debug.LogError($"[EntityDictionary.LoadFromStream] {msg}");
            EditorManager.Instance.errorHandler.DisplayError(msg);
            return null;
        }

        Dictionary<int, string> names = new Dictionary<int, string>();

        // Load base file
        try
        {
            if (addRandom) names.Add(-1, "Random");
            string fileText = File.ReadAllText(filePath);
            Dictionary<int, string> baseData = LevelDataParser.ExtractNames(fileText);
            foreach (KeyValuePair<int, string> entry in baseData)
                names.Add(entry.Key, entry.Value);

            Debug.Log($"[EntityDictionary] Loaded {baseData.Count} entries from {name}");
        }
        catch (Exception ex)
        {
            string msg = $"Failed to read '{name}': {ex.Message}";
            Debug.LogError($"[EntityDictionary.LoadFromStream] {msg}");
            EditorManager.Instance.errorHandler.DisplayError(msg);
            return names; // Return whatever we managed to load
        }

        // Check for a .append file and merge its entries in.
        // Conflicting IDs are skipped with a warning so the base game is never overwritten.
        string appendPath = filePath + ".append";
        if (File.Exists(appendPath))
        {
            try
            {
                string appendText = File.ReadAllText(appendPath);
                Dictionary<int, string> appendData = LevelDataParser.ExtractNames(appendText);
                int added = 0;

                foreach (KeyValuePair<int, string> entry in appendData)
                {
                    if (!names.ContainsKey(entry.Key))
                    {
                        names.Add(entry.Key, entry.Value);
                        added++;
                    }
                    else
                    {
                        // ID collision — skip so base game entries are never silently replaced
                        Debug.LogWarning($"[EntityDictionary] Append ID {entry.Key} ('{entry.Value}') conflicts with existing entry '{names[entry.Key]}' — skipped.");
                    }
                }

                Debug.Log($"[EntityDictionary] Merged {added} custom entries from {Path.GetFileName(appendPath)}");
            }
            catch (Exception ex)
            {
                string msg = $"Failed to read append file '{Path.GetFileName(appendPath)}': {ex.Message}";
                Debug.LogError($"[EntityDictionary.LoadFromStream] {msg}");
                EditorManager.Instance.errorHandler.DisplayError(msg);
            }
        }

        return names;
    }
}
