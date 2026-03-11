using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Code Referenced From https://www.youtube.com/watch?v=8bMzz-nSIwg
public class EditorManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static EditorManager Instance;
    public int EditorState;
    public int CurrentUID;
    
    [SerializeField]
    private GameObject itemBrowser;

    public void showItems()
    {
        
    }
}
