using UnityEngine;
using System.IO;

[System.Serializable]
public static class SaveSystem
{
    public static void SaveSettings (PersistentVariables data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/settings.json", json);
        Debug.Log("Settings Saved");
    }

    public static PersistentVariables LoadSettings()
    {   
        string path = Application.persistentDataPath + "/settings.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PersistentVariables data = JsonUtility.FromJson<PersistentVariables>(json);
            Debug.Log("Settings Loaded!");
            return data;
        }
        else
        {
            PersistentVariables data = new PersistentVariables();
            Debug.LogError("Save file not found!");
            return data;
        }
    }
}

