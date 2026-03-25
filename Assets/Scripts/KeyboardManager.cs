using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardManager : MonoBehaviour
{
    InputAction cancelAction;
    InputAction saveAction;
    public ItemBrowser itemBrowser;
    public GameObject Inspector;
    public LevelManager levelManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cancelAction = InputSystem.actions.FindAction("Cancel");
        saveAction = InputSystem.actions.FindAction("Save");
    }

    // Update is called once per frame
    void Update()
    {
        if (cancelAction.IsPressed())
        {
            itemBrowser.Deselect();
            Inspector.SetActive(false);
            EditorManager.Instance.type = ItemBrowser.Type.None;
        }
        if (saveAction.IsPressed())
        {
            levelManager.SaveLevel(levelManager.CurrentLevelPath);
        }
    }
}
