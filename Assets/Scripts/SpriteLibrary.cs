using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SpriteLibrary : MonoBehaviour
{
    public struct spriteImage
    {
        int ID;
        Sprite sprite;
    }
    
    public Sprite[] tileLibrary;
    public Sprite[] spawnLibrary;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Sprite findSpawnByID(int ID)
    {
        foreach (Sprite spawn in spawnLibrary)
        {
            if (spawn.name == $"{ID}_0")
            {
                return spawn;
            }
        }
        return spawnLibrary[0];
    }
}
