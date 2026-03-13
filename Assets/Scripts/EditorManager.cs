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
    public ItemBrowser.Type type;

    [SerializeField]
    private GameObject itemBrowser;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Optional: if needed across scenes
    }
}
