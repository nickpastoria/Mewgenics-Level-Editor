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

    // Written by Claude
    // Stored so RefreshSprite() can update the sprite without recreating the entity
    private int storedLayer;
    private int storedImageID;

    public void Create(int layer, int imageID, LevelManager.Spawn spawn)
    {
        string name = "null";
        entity = spawn;
        if (layer == 0)
        {
            if (spriteLibrary.tileImgExists(imageID)) label.enabled = false;
            if (imageID == 3) layer++;
            newSprite = spriteLibrary.findTileByID(imageID);
            name = ED.tiles[imageID];
        }
        else
        {
            if (spriteLibrary.spawnImgExists(imageID)) label.enabled = false;

            // Written by Claude
            // For static objects, try to find the biome-specific sprite first.
            // If no tileset is selected or the sprite doesn't exist in StreamingAssets,
            // fall back to the normal ID-based lookup.
            newSprite = TryGetTilesetSprite(imageID) ?? spriteLibrary.findSpawnByID(imageID);

            if (imageID > 0)
                name = ED.spawns[imageID];
            else
                name = "Random";
        }

        storedLayer = layer;
        storedImageID = imageID;

        spriteRenderer = new SpriteRenderer();
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = layer;
        spriteRenderer.sprite = newSprite;
        label.text = $"{imageID}\n{name}";
    }

    // Written by Claude
    // Updates only the sprite of a static entity in-place when the tileset changes.
    // Tiles (layer 0) and non-static spawns are left untouched.
    public void RefreshSprite()
    {
        if (spriteRenderer == null || storedLayer == 0) return;
        Sprite tilesetSprite = TryGetTilesetSprite(storedImageID);
        if (tilesetSprite != null)
            spriteRenderer.sprite = tilesetSprite;
    }

    // Written by Claude
    // Attempts to find the tileset-appropriate sprite for a static entity.
    // Returns null if: TilesetLibrary isn't loaded, the uid isn't a static object,
    // no tileset is selected, or the tileset's asset sprite doesn't exist in StreamingAssets.
    private Sprite TryGetTilesetSprite(int uid)
    {
        if (TilesetLibrary.Instance == null) return null;
        if (!TilesetLibrary.Instance.IsStaticObject(uid)) return null;

        string assetName = TilesetLibrary.Instance.GetStaticAssetName(uid);
        if (string.IsNullOrEmpty(assetName)) return null;

        return spriteLibrary.FindSpawnByName(assetName);
    }
}
