using UnityEngine;
using System.Collections.Generic;

public class LevelEntity : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public SpriteLibrary spriteLibrary;
    private Sprite newSprite;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Create(int layer, int imageID)
    {
        if (layer == 0)
        {
            newSprite = spriteLibrary.tileLibrary[0]; // Replace 0 with the actual tile ID
        }
        else{
            newSprite = spriteLibrary.findSpawnByID(imageID);
        }

        spriteRenderer = new SpriteRenderer();
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = layer;
        spriteRenderer.sprite = newSprite;
    }
}
