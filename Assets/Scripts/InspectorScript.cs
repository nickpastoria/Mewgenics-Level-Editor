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
    public EntityDictionary ED;

    void UpdateDisplay()
    {
        Name.text = ED.spawns[Spawn.uid];
        image.sprite = spritelibrary.findSpawnByID(Spawn.uid);
        Position.text = $"Position: ( {Spawn.x}, {Spawn.y} )";
        UID.text = $"UID: {Spawn.uid}";
    }

    public void UpdateInfo(LevelManager.Spawn spawn)
    {
        Spawn = spawn;
        UpdateDisplay();
    }
}
