using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;
using UnityEngine.UIElements;

public class SpriteLibrary : MonoBehaviour
{
    public Sprite[] tileLibrary;
    public Sprite[] spawnLibrary;
    public Sprite[] misc;
    public EntityDictionary ED;
    public String SpawnsFileLocation = "spawns/";
    public String TilesFileLocation = "tiles/";

    void Start()
    {
        spawnLibrary = LoadAssets(SpawnsFileLocation);
        tileLibrary = LoadAssets(TilesFileLocation);
        misc = Resources.LoadAll<Sprite>("unkown");
        EditorManager.Instance.ImagesLoaded = true;
        EditorManager.Instance.LoadToolbox();
    }
    Sprite[] LoadAssets(string location)
    {
        
        string filePath = Path.Combine(Application.streamingAssetsPath, location);
        filePath = filePath.Replace('\\', '/');
        Debug.Log($"Loading Image Assets From: {filePath}");
        string[] allPngFiles = Directory.GetFiles(filePath, "*.png", SearchOption.AllDirectories);

        List<Sprite> sprites = new List<Sprite>();
        foreach (var image in allPngFiles)
        {
            sprites.Add(LoadImageFromBytes(image));
        }
        return sprites.ToArray();
    }

    // Written by Claude
    // Finds a spawn sprite by its asset name (e.g. "TallGraveRocks1").
    // Normalizes both sides by stripping spaces and lowercasing so that
    // "TallCaveRock2" matches a file named "Tall Cave Rock 2.png".
    // Returns null if not found so the caller can fall back to a default.
    public Sprite FindSpawnByName(string assetName)
    {
        string normalizedTarget = assetName.Replace(" ", "").ToLower();
        foreach (Sprite spawn in spawnLibrary)
        {
            if (spawn.name.Replace(" ", "").ToLower() == normalizedTarget)
                return spawn;
        }
        return null;
    }

    public Sprite findSpawnByID(int ID)
    {
        foreach (Sprite spawn in spawnLibrary)
        {
            // Check for ID'd images first so we can separate items like harpoons that all have the same name
            if (spawn.name == $"{ID}") return spawn;
            else if (spawn.name == $"{ED.spawns[ID]}" || ED.spawns[ID].Contains($"({spawn.name})") || spawn.name == $"{ED.spawns[ID]} Portrait")
            {
                return spawn;
            }
        }
        return misc[0];
    }

    public Sprite findTileByID(int ID)
    {
        foreach (Sprite tile in tileLibrary)
        {
            if (tile.name == $"{ID}")
            {
                return tile;
            }
        }
        return misc[0];
    }

    public bool tileImgExists(int ID)
    {
        foreach (Sprite tile in tileLibrary)
        {
            if (tile.name == $"{ID}")
            {
                return true;
            }
        }
        return false;
    }

    public bool spawnImgExists(int ID)
    {
        foreach (Sprite spawn in spawnLibrary)
        {
            // Check for ID'd images first so we can separate items like harpoons that all have the same name
            if (spawn.name == $"{ID}") return spawn;
            else if (spawn.name == $"{ED.spawns[ID]}" || ED.spawns[ID].Contains($"({spawn.name})") || spawn.name == $"{ED.spawns[ID]} Portrait")
            {
                return true;
            }
        }
        return false;
    }
    Sprite LoadImageFromBytes(string relativePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

        // This approach works on platforms that allow synchronous File I/O
        if (File.Exists(fullPath))
        {
            byte[] fileBytes = File.ReadAllBytes(fullPath);
            List<float> pivotList = ReadPivotPoints(fullPath);

            Texture2D tex = new Texture2D(2, 2); // Dimensions are automatically adjusted by LoadImage
            tex.LoadImage(fileBytes); // LoadImage handles the byte-array conversion

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Rect rect = new Rect(0.0f, 0.0f, tex.width, tex.height);
            Vector2 pivot = new Vector2(pivotList[0], pivotList[1]);
            
            float pixelsPerUnit = 100.0f;
            Sprite newSprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit);
            string name =  Path.GetFileName(fullPath);
            newSprite.name = name.Remove(name.Length - 4);
            return newSprite;
        }else
        {
            return null;
        }
    }
    public static List<float> ReadPivotPoints(string imagePath)
    {
        string txtPath = Path.ChangeExtension(imagePath, ".txt");
        List<float> pivots = new List<float>();

        if (!File.Exists(txtPath))
        {
            Debug.LogWarning($"Pivot file not found: {txtPath}");
            return new List<float> {0.5f, 0.5f};
        }

        string[] tokens = File.ReadAllText(txtPath).Trim().Split(' ');

        foreach (string token in tokens)
        {
            if (float.TryParse(token, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                pivots.Add(value / 100f);  // convert % to 0-1 range
            }
        }

        return pivots;
    }
}
