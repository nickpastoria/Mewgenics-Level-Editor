using UnityEngine;
using UnityEngine.UI;

public class RandomItem : MonoBehaviour
{
    public Image randomImage;
    public Button DeleteButton;
    public Button selectButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetImage(Sprite image)
    {
        randomImage.sprite = image;
    }
    public Button GetSelectButton()
    {
        return selectButton;
    }
    public Button GetDeleteButton()
    {
        return DeleteButton;
    }

}
