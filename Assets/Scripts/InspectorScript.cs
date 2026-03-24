using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectorScript : MonoBehaviour
{
    public LevelManager.Spawn Spawn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TMP_Text Name;
    public Image image;
    public TMP_Text Position;
    public TMP_Text UID;
    public SpriteLibrary spritelibrary;
    public LevelManager levelManager;
    public EntityDictionary ED;
    public GameObject RandomList;
    public GameObject RandomItemPrefab;
    public Button AddItemButton;

    void Start()
    {
        AddItemButton.onClick.AddListener(() => AddItem(new LevelManager.randomSpawn()));
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

    void UpdateDisplay()
    {
        GameObject[] childrenList = GameObject.FindGameObjectsWithTag("RandomItem");
        foreach(GameObject child in childrenList)
        {
            Destroy(child);
        }

        Name.text = ED.spawns[Spawn.uid];
        image.sprite = spritelibrary.findSpawnByID(Spawn.uid);
        Position.text = $"Position: ( {Spawn.x}, {Spawn.y} )";
        UID.text = $"UID: {Spawn.uid}";
        if (Spawn.randomCount > 0)
        {
            foreach (LevelManager.randomSpawn randomspawn in Spawn.spawns)
            {
                GameObject newItem = GameObject.Instantiate(RandomItemPrefab, RandomList.transform);
                newItem.GetComponent<RandomItem>().SetImage(spritelibrary.findSpawnByID(randomspawn.uid));
                Button newbutton = newItem.GetComponent<RandomItem>().GetDeleteButton();
                newbutton.onClick.AddListener(() => DeleteItem(randomspawn));
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
        LevelManager.randomSpawn[] newList = new LevelManager.randomSpawn[Spawn.randomCount-1];
        int j = 0;
        for (int i = 0; i < Spawn.randomCount; i++)
        {
            if (Spawn.spawns[i].uid != excludedSpawn.uid)
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
        LevelManager.randomSpawn[] newList = new LevelManager.randomSpawn[Spawn.randomCount+1];
        newList[0] = newSpawn;
        for (int i = 1; i < Spawn.randomCount; i++)
        {
            newList[i] = Spawn.spawns[i];
        }
        Spawn.randomCount++;
        Spawn.spawns = newList;
        levelManager.updateLevel();
        UpdateDisplay();
    }
}
