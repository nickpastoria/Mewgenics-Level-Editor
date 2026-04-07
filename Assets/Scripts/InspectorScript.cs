using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectorScript : MonoBehaviour
{
    public LevelManager.Spawn Spawn;
    // public TMP_Text Name;
    // public Image image;
    // public TMP_Text Position;
    // public TMP_Text UID;
    public SpriteLibrary spritelibrary;
    public LevelManager levelManager;
    public EntityDictionary ED;
    public GameObject RandomList;
    public GameObject RandomItemPrefab;
    public Button AddItemButton;

    // The maximum number of entries allowed in a random spawn list.
    // Adjust this value if the game engine cap is discovered to be different.
    private const int MAX_RANDOM_SPAWNS = 3;

    // Tracks which randomSpawn row is currently awaiting a browser selection
    private LevelManager.randomSpawn activeSpawn = null;
    private System.Collections.Generic.List<GameObject> spawnRows = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        AddItemButton.onClick.AddListener(() => AddItem(new LevelManager.randomSpawn()));
    }

    // Written by Claude
    // Clears the active row highlight when the user leaves Select mode
    void Update()
    {
        if (activeSpawn != null && EditorManager.Instance.type != ItemBrowser.Type.Select)
        {
            activeSpawn = null;
            UpdateDisplay();
        }
    }

    void OnEnable()
    {
        UpdateDisplay();
        EditorManager.Instance.mouseEnabled = false;
    }

    void OnDisable()
    {
        EditorManager.Instance.mouseEnabled = true;
    }

    public void UpdateDisplay()
    {
        // Destroy existing rows before rebuilding
        GameObject[] childrenList = GameObject.FindGameObjectsWithTag("RandomItem");
        foreach (GameObject child in childrenList)
        {
            Destroy(child);
        }
        spawnRows.Clear();

        // Disable the add button when the cap is reached
        AddItemButton.interactable = (Spawn.randomCount < MAX_RANDOM_SPAWNS);

        if (Spawn.randomCount > 0)
        {
            foreach (LevelManager.randomSpawn randomspawn in Spawn.spawns)
            {
                GameObject newItem = GameObject.Instantiate(RandomItemPrefab, RandomList.transform);
                RandomItem randomItem = newItem.GetComponent<RandomItem>();
                randomItem.SetImage(spritelibrary.findSpawnByID(randomspawn.uid));

                // Highlight the row that is currently awaiting a browser selection
                randomItem.SetHighlight(randomspawn == activeSpawn);

                Button deleteButton = randomItem.GetDeleteButton();
                deleteButton.onClick.AddListener(() => DeleteItem(randomspawn));
                Button selectButton = randomItem.GetSelectButton();
                selectButton.onClick.AddListener(() => SelectItem(randomspawn));

                spawnRows.Add(newItem);
            }
        }
    }

    public void UpdateInfo(LevelManager.Spawn spawn)
    {
        Spawn = spawn;
        UpdateDisplay();
    }

    public void DeleteItem(LevelManager.randomSpawn excludedSpawn)
    {
        LevelManager.randomSpawn[] newList = new LevelManager.randomSpawn[Spawn.randomCount - 1];
        int j = 0;
        for (int i = 0; i < Spawn.randomCount; i++)
        {
            if (!Spawn.spawns[i].Equals(excludedSpawn))
            {
                newList[j] = Spawn.spawns[i];
                j++;
            }
        }
        Spawn.randomCount--;
        Spawn.spawns = newList;
        levelManager.updateLevel();
        UpdateDisplay();
    }

    public void AddItem(LevelManager.randomSpawn newSpawn)
    {
        LevelManager.randomSpawn[] newList = new LevelManager.randomSpawn[Spawn.randomCount + 1];
        // New entry goes at index 0; existing entries shift up by one
        newList[0] = newSpawn;
        for (int i = 0; i < Spawn.randomCount; i++)
        {
            newList[i + 1] = Spawn.spawns[i];
        }
        Spawn.randomCount++;
        Spawn.spawns = newList;
        levelManager.updateLevel();
        UpdateDisplay();

        // Written by Claude
        // Immediately enter Select mode for the new entry so the user can
        // pick a spawn from the browser without needing an extra click
        SelectItem(newList[0]);
    }

    // Written by Claude
    // Enters Select mode and tracks which row is awaiting an assignment,
    // so it can be highlighted in UpdateDisplay.
    public void SelectItem(LevelManager.randomSpawn newSelection)
    {
        activeSpawn = newSelection;
        EditorManager.Instance.type = ItemBrowser.Type.Select;
        EditorManager.Instance.selectedSpawn = newSelection;
        UpdateDisplay(); // Refresh row highlights immediately
    }

    // Written by Claude
    // Clears the active row highlight after a spawn has been confirmed.
    // Call this from ItemBrowser once the selection is applied.
    public void ClearActiveSelection()
    {
        activeSpawn = null;
        UpdateDisplay();
    }
}
