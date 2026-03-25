using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class EntityDictionary : MonoBehaviour
{
    public Dictionary<int, string> spawns;
    public Dictionary<int, string> tiles;
    void Start()
    {
        spawns = LoadFromStream("spawns.gon");
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

    public Dictionary<int, string> LoadFromStream(string name)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, name);
        filePath = filePath.Replace('\\', '/');

        // Use File.ReadAllText for text files, or File.ReadAllBytes for raw data
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            return LevelDataParser.ExtractNames(jsonText);
        }
        else
        {
            Debug.LogError("File not found at: " + filePath);
            return null;
        }
    }
}
