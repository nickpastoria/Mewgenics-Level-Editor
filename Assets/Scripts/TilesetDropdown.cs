using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Written by Claude
// Attach this to a TMP_Dropdown GameObject in the scene.
// It populates itself from TilesetLibrary.TilesetNames on Start and calls
// TilesetLibrary.SetTileset() when the user changes the selection.
// It also reacts to external tileset changes (e.g. from other scripts) via
// the TilesetLibrary.OnTilesetChanged event so the visual stays in sync.
//
// Setup:
//   1. Add a TMP_Dropdown to your UI canvas.
//   2. Attach this script to the same GameObject.
//   3. The dropdown populates automatically at runtime — no manual wiring needed.
[RequireComponent(typeof(TMP_Dropdown))]
public class TilesetDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private List<string> tilesetNames;

    // Written by Claude
    // Disables mouse placement while the dropdown is open so the user
    // can't accidentally place tiles while browsing tilesets.
    void Update()
    {
        if (dropdown != null && EditorManager.Instance != null)
            EditorManager.Instance.mouseEnabled = !dropdown.IsExpanded;
    }

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        PopulateDropdown();

        // Listen for user interaction
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        // Listen for external tileset changes so the visual stays in sync
        if (TilesetLibrary.Instance != null)
            TilesetLibrary.Instance.OnTilesetChanged += OnExternalTilesetChanged;
    }

    // Written by Claude
    // Fills the dropdown options from TilesetLibrary and pre-selects the current tileset.
    private void PopulateDropdown()
    {
        if (TilesetLibrary.Instance == null)
        {
            Debug.LogWarning("[TilesetDropdown] TilesetLibrary not found — dropdown will be empty.");
            return;
        }

        tilesetNames = TilesetLibrary.Instance.TilesetNames;
        if (tilesetNames == null || tilesetNames.Count == 0)
        {
            Debug.LogWarning("[TilesetDropdown] No tilesets loaded.");
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(tilesetNames);

        // Pre-select whichever tileset is currently active
        SyncVisualToCurrentTileset();

        Debug.Log($"[TilesetDropdown] Populated with {tilesetNames.Count} tilesets.");
    }

    // Written by Claude
    // Updates the dropdown visual to match TilesetLibrary.CurrentTileset
    // without firing the onValueChanged callback (to avoid infinite loops).
    private void SyncVisualToCurrentTileset()
    {
        if (tilesetNames == null || dropdown == null) return;

        string current = TilesetLibrary.Instance.CurrentTileset;
        int index = tilesetNames.IndexOf(current);
        if (index >= 0)
        {
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue(); // Forces the caption label to update visually
        }
    }

    // Called when the user picks a new value from the dropdown popup
    private void OnDropdownChanged(int index)
    {
        if (tilesetNames == null || index < 0 || index >= tilesetNames.Count) return;
        TilesetLibrary.Instance.SetTileset(tilesetNames[index]);
    }

    // Called when something else changes the tileset externally
    private void OnExternalTilesetChanged(string newTileset)
    {
        SyncVisualToCurrentTileset();
    }

    void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);

        if (TilesetLibrary.Instance != null)
            TilesetLibrary.Instance.OnTilesetChanged -= OnExternalTilesetChanged;
    }
}
