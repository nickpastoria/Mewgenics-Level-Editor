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
        spawns = LoadFromStream("spawns.gon", true);
        tiles = LoadFromStream("tiles.gon");
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

    public Dictionary<int, string> LoadFromStream(string name, bool addRandom = false)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, name);
        filePath = filePath.Replace('\\', '/');

        // Use File.ReadAllText for text files, or File.ReadAllBytes for raw data
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            Dictionary<int, string> names = new Dictionary<int, string>();
            if (addRandom) names.Add(-1, "Random");
            Dictionary<int, string> gon = LevelDataParser.ExtractNames(jsonText.ToString());
            foreach (KeyValuePair<int, string> data in gon)
            {
                names.Add(data.Key, data.Value);
            }

            return names;
        }
        else
        {
            Debug.LogError("File not found at: " + filePath);
            return null;
        }
    }
}
