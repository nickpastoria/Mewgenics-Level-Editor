using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;

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

    public Sprite findSpawnByID(int ID)
    {
        foreach (Sprite spawn in spawnLibrary)
        {
            if (spawn.name == $"{ED.spawns[ID]}" || ED.spawns[ID].Contains($"({spawn.name})") || spawn.name == $"{ED.spawns[ID]} Portrait")
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
            if (spawn.name == $"{ED.spawns[ID]}" || ED.spawns[ID].Contains($"({spawn.name})") || spawn.name == $"{ED.spawns[ID]} Portrait")
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
            Texture2D tex = new Texture2D(2, 2); // Dimensions are automatically adjusted by LoadImage
            tex.LoadImage(fileBytes); // LoadImage handles the byte-array conversion
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Rect rect = new Rect(0.0f, 0.0f, tex.width, tex.height);
            Vector2 pivot;
            
            if (relativePath.Contains("tiles"))
            {
                if (tex.height <= 75.0f) pivot = new Vector2(0.5f, 0.5f);
                else if (tex.height <= 100.0) pivot = new Vector2(0.5f, 0.4f);
                else pivot = new Vector2(0.5f, 36.0f/tex.height);
            }
            else if (tex.height < 30.0f) pivot = new Vector2(0.5f, 0.5f);
            else if (tex.height < 36.0f) pivot = new Vector2(0.5f, 2.0f/tex.height);
            else if (tex.height < 72.0f) pivot = new Vector2(0.5f, 36.0f/tex.height);
            else if (tex.height < 110.0f) pivot = new Vector2(0.5f, 0.2f);
            else
            {
                pivot = new Vector2(0.5f, 36.0f/tex.height);
            }
            
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
}
