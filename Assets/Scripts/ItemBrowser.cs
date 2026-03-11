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

    private void OnEnable()
    {
        foreach(KeyValuePair<int, string> entry in ED.spawns)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent.transform);
            newButton.GetComponent<InventoryObject>().Make(entry.Key, entry.Value, SPL);
            newButton.GetComponent<Button>().onClick.AddListener(() => SelectItem(entry.Key));
        }
    }

    private void SelectItem(int UID)
    {
        Debug.Log("Selected Item: " + UID);
        EditorManager.Instance.CurrentUID = UID;
    }
}
