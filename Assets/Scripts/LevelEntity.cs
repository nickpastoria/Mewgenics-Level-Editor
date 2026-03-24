using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class LevelEntity : MonoBehaviour
{
    public SpriteLibrary spriteLibrary;
    public TMP_Text label;
    private SpriteRenderer spriteRenderer;
    private Sprite newSprite;
    public EntityDictionary ED;

    public LevelManager.Spawn entity;
    
    public void Create(int layer, int imageID, LevelManager.Spawn spawn)
    {
        string name = "null";
        entity = spawn;
        if (layer == 0)
        {
            if (spriteLibrary.tileImgExists(imageID)) label.enabled = false;
            if (imageID == 3) layer ++;
            newSprite = spriteLibrary.findTileByID(imageID); // Replace 0 with the actual tile ID
            name = ED.tiles[imageID];
        }
        else{
            if (spriteLibrary.spawnImgExists(imageID)) label.enabled = false;
            newSprite = spriteLibrary.findSpawnByID(imageID);
            if (imageID > 0)
            {
                name = ED.spawns[imageID];
            }else
            {
                name = "Random";
            }
        }

        spriteRenderer = new SpriteRenderer();
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = layer;
        spriteRenderer.sprite = newSprite;
        label.text = $"{imageID}\n{name}";
    }
}
