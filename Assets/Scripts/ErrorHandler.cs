using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ErrorHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TMP_Text ErrorDialogue;
    public Button button;
    public void DisplayError(string error_message)
    {
        EditorManager.Instance.mouseEnabled = false;
        gameObject.SetActive(true);
        ErrorDialogue.text = error_message;
        button.onClick.AddListener(() => CloseError());
    }
    public void CloseError()
    {
        gameObject.SetActive(false);
        EditorManager.Instance.mouseEnabled = false;
    }
}
