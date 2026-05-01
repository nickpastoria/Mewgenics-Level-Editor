using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class InventoryObject : MonoBehaviour
{

    public string ItemName;
    public int UID;
    public SpriteLibrary SPL;
    private TMP_Text textLabel;
    private Image buttonImage;
    private GameObject ImageButton;

    // Written by Claude — stored so RefreshSprite() can update without rebuilding
    private ItemBrowser.Type storedType;

    public void Make(ItemBrowser.Type type, int u, string n, SpriteLibrary spl)
    {
        Image SpriteImage;
        ItemName = n;
        UID = u;
        SPL = spl;
        storedType = type;
        textLabel = GetComponentInChildren<TMP_Text>();
        textLabel.text = ItemName;
        ImageButton = FindChildWithTag(this.gameObject, "Toolbox Image");
        SpriteImage = ImageButton.GetComponent<Image>();

        if (type == ItemBrowser.Type.Spawn)
        {
            // Written by Claude
            // For static objects, try the tileset-specific sprite first.
            // Falls back to the normal ID-based lookup if none is found.
            Sprite sprite = null;
            if (TilesetLibrary.Instance != null && TilesetLibrary.Instance.IsStaticObject(UID))
            {
                string assetName = TilesetLibrary.Instance.GetStaticAssetName(UID);
                if (!string.IsNullOrEmpty(assetName))
                    sprite = SPL.FindSpawnByName(assetName);
            }
            if (sprite == null && SPL.spawnImgExists(UID))
                sprite = SPL.findSpawnByID(UID);
            if (sprite != null)
                SpriteImage.sprite = sprite;
        }
        if(type == ItemBrowser.Type.Tile)
        {
            if(SPL.tileImgExists(UID))
            {
                SpriteImage.sprite = SPL.findTileByID(UID);
            }
        }
    }
    // Written by Claude
    // Updates only the sprite of a static toolbox button in-place when the tileset changes.
    // Non-static and tile buttons are left untouched.
    public void RefreshSprite()
    {
        if (storedType != ItemBrowser.Type.Spawn) return;
        if (TilesetLibrary.Instance == null || !TilesetLibrary.Instance.IsStaticObject(UID)) return;

        string assetName = TilesetLibrary.Instance.GetStaticAssetName(UID);
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(assetName))
            sprite = SPL.FindSpawnByName(assetName);
        if (sprite == null && SPL.spawnImgExists(UID))
            sprite = SPL.findSpawnByID(UID);

        if (sprite != null)
        {
            GameObject imageObj = FindChildWithTag(gameObject, "Toolbox Image");
            if (imageObj != null)
                imageObj.GetComponent<Image>().sprite = sprite;
        }
    }

    GameObject FindChildWithTag(GameObject parent, string tag) {
        GameObject child = null;

        foreach(Transform transform in parent.transform) {
            if(transform.CompareTag(tag)) {
                child = transform.gameObject;
                break;
            }
        }

        return child;
    }

}
