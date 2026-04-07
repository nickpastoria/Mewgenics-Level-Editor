using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

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

    // Written by Claude
    // The currently selected biome/tileset — set by the tileset dropdown UI.
    // Other systems (e.g. SpriteLibrary) read this to pick biome-appropriate assets.
    public string CurrentTileset = "";

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

    // Written by Claude
    // Starts coroutines to refresh static toolbox sprites across multiple frames,
    // keeping the editor responsive during tileset switches.
    public void ReloadToolbox()
    {
        StartCoroutine(ReloadToolboxCoroutine());
    }

    private IEnumerator ReloadToolboxCoroutine()
    {
        // Start both browser refreshes simultaneously so they interleave across frames
        Coroutine spawnRefresh = StartCoroutine(SpawnsBrowser.GetComponent<ItemBrowser>().RefreshStaticSpritesCoroutine());
        Coroutine tileRefresh  = StartCoroutine(TilesBrowser.GetComponent<ItemBrowser>().RefreshStaticSpritesCoroutine());
        yield return spawnRefresh;
        yield return tileRefresh;
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
