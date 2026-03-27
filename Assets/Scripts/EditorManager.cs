using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Code Referenced From https://www.youtube.com/watch?v=8bMzz-nSIwg
public class EditorManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static EditorManager Instance;
    public PersistentVariables SysVars;
    public int EditorState;
    public int CurrentUID;
    public ItemBrowser.Type type;
    public LevelManager.randomSpawn selectedSpawn;

    public Sprite PreviewSprite;

    public bool mouseEnabled = true;

    public GameObject TilesBrowser;

    public GameObject SpawnsBrowser;
    public bool EntitiesLoaded = false;

    public bool ImagesLoaded = false;
    public TMP_Text ProjectLabel;
    public TMP_Text LevelLabel;
    public ErrorHandler errorHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Optional: if needed across scenes
        SysVars = SaveSystem.LoadSettings();
        UpdateProjectLabel();
        type = ItemBrowser.Type.None;
    }

    public void LoadToolbox()
    {
        if (EntitiesLoaded && ImagesLoaded)
        {
            SpawnsBrowser.GetComponent<ItemBrowser>().Create();
            TilesBrowser.GetComponent<ItemBrowser>().Create();
        }
    }
    public void UpdateProjectLabel()
    {
        if(SysVars.defaultFileLocation != "C:\\" && SysVars.defaultFileLocation != null)
        {
            string[] folders =  SysVars.defaultFileLocation.Split("\\");
            ProjectLabel.text = folders[^1];
        }

    }

    public void UpdateLevelLabel(string label)
    {
        LevelLabel.text = label;
    }
}
