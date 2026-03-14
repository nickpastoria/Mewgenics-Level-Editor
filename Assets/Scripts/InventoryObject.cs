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

    public void Make(ItemBrowser.Type type, int u, string n, SpriteLibrary spl)
    {
        Image SpriteImage;
        name = n;
        UID = u;
        SPL = spl;
        textLabel = GetComponentInChildren<TMP_Text>();
        textLabel.text = name;
        ImageButton = FindChildWithTag(this.gameObject, "Toolbox Image");
        SpriteImage = ImageButton.GetComponent<Image>();

        if (type == ItemBrowser.Type.Spawn)
        {
            if(SPL.spawnImgExists(UID))
            {
                SpriteImage.sprite = SPL.findSpawnByID(UID);
            }
        }
        if(type == ItemBrowser.Type.Tile)
        {
            if(SPL.tileImgExists(UID))
            {
                SpriteImage.sprite = SPL.findTileByID(UID);
            }
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
