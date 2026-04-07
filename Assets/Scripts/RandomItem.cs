using UnityEngine;
using UnityEngine.UI;

public class RandomItem : MonoBehaviour
{
    public Image randomImage;
    public Button DeleteButton;
    public Button selectButton;

    // Written by Claude
    // Wire this to the background Image inside the RandomItem prefab.
    // Tag it "SelectHighlight" in Unity — this field is found automatically on Start.
    private Image background;
    private Color defaultBgColor;

    // Whether this row is currently the active selection target
    private bool isHighlighted = false;

    // Pulse effect settings
    // Color the background pulses to when highlighted
    private readonly Color highlightColor = new Color(1f, 0.6f, 0f, 1f); // orange
    // Speed of the pulse oscillation (higher = faster)
    private const float PULSE_SPEED = 2f;
    private float pulseT = 0f;
    private bool pulseGrowing = true;

    void Start()
    {
        // Find the background image within this prefab's children by tag
        GameObject bgObj = FindTagInChildren("SelectHighlight");
        if (bgObj != null)
        {
            background = bgObj.GetComponent<Image>();
            defaultBgColor = background.color;
        }
    }

    // Written by Claude
    // Pulses the row background orange while this row is the active selection target.
    void Update()
    {
        if (background == null) return;

        if (isHighlighted)
        {
            // Ping-pong pulseT between 0 and 1
            pulseT += (pulseGrowing ? 1f : -1f) * PULSE_SPEED * Time.deltaTime;
            if (pulseT >= 1f) { pulseT = 1f; pulseGrowing = false; }
            if (pulseT <= 0f) { pulseT = 0f; pulseGrowing = true; }
            background.color = Color.Lerp(defaultBgColor, highlightColor, pulseT);
        }
        else
        {
            // Restore default and reset pulse state
            pulseT = 0f;
            pulseGrowing = true;
            background.color = defaultBgColor;
        }
    }

    // Written by Claude
    // Searches only this GameObject's children for a tag,
    // since Unity's FindWithTag searches the whole scene.
    private GameObject FindTagInChildren(string tag)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
                return child.gameObject;
        }
        return null;
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

    // Written by Claude
    // Call this from InspectorScript to mark this row as the active selection target.
    public void SetHighlight(bool active)
    {
        isHighlighted = active;
    }
}
