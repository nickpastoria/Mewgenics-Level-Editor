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

    public void Make(ItemBrowser.Type type, int u, string n, SpriteLibrary spl)
    {
        name = n;
        UID = u;
        SPL = spl;
        textLabel = GetComponentInChildren<TMP_Text>();
        textLabel.text = name;
        if (type == ItemBrowser.Type.Spawn)
        {
            if(SPL.spawnImgExists(UID))
            {
                buttonImage = GetComponentInChildren<Image>();
                buttonImage.sprite = SPL.findSpawnByID(UID);
            }
        }
        if(type == ItemBrowser.Type.Tile)
        {
            if(SPL.tileImgExists(UID))
            {
                buttonImage = GetComponentInChildren<Image>();
                buttonImage.sprite = SPL.findTileByID(UID);
            }
        }
    }

}
