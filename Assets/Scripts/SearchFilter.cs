using UnityEngine;
using TMPro;

public class SearchFilter : MonoBehaviour
{
    public TMP_InputField inputField;
    public ItemBrowser tilesBrowser;
    public ItemBrowser spawnBrowser;

    void Start()
    {
        inputField = gameObject.GetComponent<TMP_InputField>();
    }
    public void TextChanged()
    {
        spawnBrowser.Filter(inputField.text);
        tilesBrowser.Filter(inputField.text);
    }

    public void clearText()
    {
        inputField.text = "";
    }
}
