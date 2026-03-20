using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardManager : MonoBehaviour
{
    InputAction cancelAction;
    public ItemBrowser itemBrowser;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cancelAction = InputSystem.actions.FindAction("Cancel");
    }

    // Update is called once per frame
    void Update()
    {
        if (cancelAction.IsPressed())
        {
            itemBrowser.Deselect();
        }
    }
}
