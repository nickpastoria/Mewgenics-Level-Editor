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
}
