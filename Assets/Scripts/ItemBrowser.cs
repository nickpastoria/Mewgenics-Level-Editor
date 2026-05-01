using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class ItemBrowser : MonoBehaviour
{
    public GameObject buttonPrefab;
    public GameObject buttonParent;
    public EntityDictionary ED;
    public SpriteLibrary SPL;
    public SpriteRenderer PreviewImage;

    public ItemBrowser.Type LoaderType;
    public InspectorScript inspectorScript;

    private List<GameObject> SpawnItemList;

    public enum Type
    {
        Tile,
        Spawn,
        None,
        Select
    }

    void Awake()
    {
        EditorManager.Instance.type = LoaderType;
    }

    public void Create()
    {
        SpawnItemList = new List<GameObject>();
        Dictionary<int, string> loadtype = null;
        if (LoaderType == ItemBrowser.Type.Tile) loadtype = ED.tiles;
        if (LoaderType == ItemBrowser.Type.Spawn) loadtype = ED.spawns;
        foreach(KeyValuePair<int, string> entry in loadtype)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent.transform);
            newButton.GetComponent<InventoryObject>().Make(LoaderType, entry.Key, entry.Value, SPL);
            newButton.GetComponent<Button>().onClick.AddListener(() => SelectItem(LoaderType, entry.Key));
            SpawnItemList.Add(newButton);
        }
    }

    // Written by Claude
    // Destroys all existing toolbox buttons and recreates them.
    public void Rebuild()
    {
        foreach (GameObject button in SpawnItemList)
            Destroy(button);
        Create();
    }

    // Written by Claude
    // Updates only static item sprites in-place — no destroy/recreate.
    // Much faster than Rebuild() when only the tileset has changed.
    public void RefreshStaticSprites()
    {
        foreach (GameObject button in SpawnItemList)
        {
            if (button == null) continue;
            button.GetComponent<InventoryObject>()?.RefreshSprite();
        }
    }

    // Written by Claude
    // Same as RefreshStaticSprites() but spread across frames (batchSize items per frame)
    // so the UI stays responsive during large tileset switches.
    public IEnumerator RefreshStaticSpritesCoroutine(int batchSize = 20)
    {
        int count = 0;
        foreach (GameObject button in SpawnItemList)
        {
            if (button == null) continue;
            button.GetComponent<InventoryObject>()?.RefreshSprite();
            if (++count % batchSize == 0) yield return null;
        }
    }

    private void SelectItem(ItemBrowser.Type type, int UID)
    {
        Debug.Log("Selected Item: " + UID);
        EditorManager.Instance.CurrentUID = UID;
        if (EditorManager.Instance.type == Type.Select)
        {
            EditorManager.Instance.selectedSpawn.uid = UID;
            // Written by Claude
            // Clear the row highlight in the inspector once the selection is confirmed
            inspectorScript.ClearActiveSelection();
        } else
        {
            EditorManager.Instance.type = type;
            if (type == ItemBrowser.Type.Spawn)
            {
                // Written by Claude
                // Use the tileset-appropriate sprite for static objects if available,
                // otherwise fall back to the standard ID-based lookup.
                Sprite preview = null;
                if (TilesetLibrary.Instance != null && TilesetLibrary.Instance.IsStaticObject(UID))
                {
                    string assetName = TilesetLibrary.Instance.GetStaticAssetName(UID);
                    if (!string.IsNullOrEmpty(assetName))
                        preview = SPL.FindSpawnByName(assetName);
                }
                PreviewImage.sprite = preview ?? SPL.findSpawnByID(UID);
            }
            if(type == ItemBrowser.Type.Tile) PreviewImage.sprite = SPL.findTileByID(UID);
        }
    }

    public void FilterStatics()
    {
        Filter(5000,6000);
    }
    public void FilterEnemies()
    {
        Filter(-1,1000);
    }
    public void FilterElites()
    {
        Filter(1001,2000);
    }
    public void FilterBosses()
    {
        Filter(2000,3000);
    }
    public void FilterSearch(string name)
    {
        Filter(name);
    }

    public void Filter(int min, int max)
    {
        foreach(GameObject button in SpawnItemList)
        {
            if (button.GetComponent<InventoryObject>().UID >= min && button.GetComponent<InventoryObject>().UID <= max)
            {
                button.SetActive(true);
            }
            else
            {
                button.SetActive(false);
            }
        }
    }
    public void Filter(string name)
    {
        foreach(GameObject button in SpawnItemList)
        {
            // if (!button.activeInHierarchy) continue;
            if (button.GetComponent<InventoryObject>().ItemName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                button.SetActive(true);
            }
            else
            {
                button.SetActive(false);
            }
        }
    }

    public void Deselect()
    {
        Debug.Log("Deselected Item");
        EditorManager.Instance.CurrentUID = 0;
        EditorManager.Instance.type = Type.None;
        PreviewImage.sprite = null;
    }
}
