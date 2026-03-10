using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SpriteLibrary : MonoBehaviour
{
    public Sprite[] tileLibrary;
    public Sprite[] spawnLibrary;
    public Sprite[] misc;

    void Start()
    {
        spawnLibrary = Resources.LoadAll<Sprite>("spawns");
        tileLibrary = Resources.LoadAll<Sprite>("tiles");
        misc = Resources.LoadAll<Sprite>("unkown");
    }
    

    public Sprite findSpawnByID(int ID)
    {
        foreach (Sprite spawn in spawnLibrary)
        {
            if (spawn.name == $"{ID}")
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
            if (spawn.name == $"{ID}")
            {
                return true;
            }
        }
        return false;
    }
}
