using UnityEngine;
using System;
using System.Collections.Generic;

public class EntityDictionary : MonoBehaviour
{
    public Dictionary<int, string> spawns;
    public Dictionary<int, string> tiles;
    void Start()
    {
        spawns = LoadFromFile("references/spawns");
        tiles = LoadFromFile("references/tiles");
    }

    Dictionary<int, string> LoadFromFile(string loc)
    {
        // Load the text file named "dialogue" from the Resources folder
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
}
