using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class ItemBrowser : MonoBehaviour
{
    public GameObject buttonPrefab;
    public GameObject buttonParent;
    public EntityDictionary ED;
    public SpriteLibrary SPL;

    public ItemBrowser.Type LoaderType;

    public enum Type
    {
        Tile,
        Spawn
    }

    void Start()
    {
        Dictionary<int, string> loadtype;
        loadtype = null;
        if (LoaderType == ItemBrowser.Type.Tile) loadtype = ED.tiles;
        if (LoaderType == ItemBrowser.Type.Spawn) loadtype = ED.spawns;
        foreach(KeyValuePair<int, string> entry in loadtype)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent.transform);
            newButton.GetComponent<InventoryObject>().Make(LoaderType, entry.Key, entry.Value, SPL);
            newButton.GetComponent<Button>().onClick.AddListener(() => SelectItem(LoaderType, entry.Key));
        }
    }

    private void SelectItem(ItemBrowser.Type type, int UID)
    {
        Debug.Log("Selected Item: " + UID);
        EditorManager.Instance.CurrentUID = UID;
        EditorManager.Instance.type = type;
    }
}
