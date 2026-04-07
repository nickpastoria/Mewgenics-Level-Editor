using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// Written by Claude
// Loads tilesets.gon and the object-type field from spawns.gon at startup.
// Exposes GetStaticAssetName(uid) so other systems can look up the correct
// biome-specific asset name for a static entity in the current tileset.
//
// Add this MonoBehaviour to a scene GameObject, then reference it from scripts
// that need tileset-aware sprite lookup (e.g. SpriteLibrary, LevelEntity).
//
// To switch biomes at runtime, call SetTileset("boneyard") or bind
// TilesetNames to a UI dropdown and call SetTileset on value change.
public class TilesetLibrary : MonoBehaviour
{
    public static TilesetLibrary Instance;

    // All tilesets parsed from data/tilesets.gon
    public Dictionary<string, TilesetData> Tilesets { get; private set; }

    // Sorted list of tileset names — use to populate a UI dropdown
    public List<string> TilesetNames { get; private set; }

    // The default tileset declared in the tilesets.gon header ("default_tileset <name>")
    public string DefaultTileset { get; private set; }

    // The currently selected tileset — change via SetTileset()
    public string CurrentTileset { get; private set; }

    // Fires whenever the tileset changes — subscribe to keep UI in sync
    public event Action<string> OnTilesetChanged;

    // uid → object type string (e.g. 5003 → "StaticTallA"), parsed from spawns.gon
    private Dictionary<int, string> staticObjectTypes;

    private void Awake()
    {
        // Singleton setup — only one TilesetLibrary should exist
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Written by Claude
        // Load data in Awake (not Start) so that TilesetNames and CurrentTileset
        // are ready by the time other scripts' Start() methods run (e.g. TilesetDropdown).
        LoadTilesets();
        LoadObjectTypes();

        if (!string.IsNullOrEmpty(DefaultTileset))
            SetTileset(DefaultTileset);
    }

    void Start() { }

    // Parses data/tilesets.gon and populates Tilesets and TilesetNames
    private void LoadTilesets()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "data/tilesets.gon").Replace('\\', '/');

        if (!File.Exists(path))
        {
            Debug.LogWarning("[TilesetLibrary] data/tilesets.gon not found — tileset preview unavailable.");
            Tilesets = new Dictionary<string, TilesetData>(StringComparer.OrdinalIgnoreCase);
            TilesetNames = new List<string>();
            return;
        }

        try
        {
            string content = File.ReadAllText(path);

            // Parse the "default_tileset <name>" header line if present
            var defaultMatch = Regex.Match(content, @"^\s*default_tileset\s+(\w+)", RegexOptions.Multiline);
            if (defaultMatch.Success)
                DefaultTileset = defaultMatch.Groups[1].Value;

            Tilesets = LevelDataParser.ParseTilesets(content);

            TilesetNames = new List<string>(Tilesets.Keys);
            TilesetNames.Sort();

            Debug.Log($"[TilesetLibrary] Loaded {Tilesets.Count} tilesets. Default: '{DefaultTileset}'");
        }
        catch (Exception ex)
        {
            string msg = $"Failed to load tilesets: {ex.Message}";
            Debug.LogError($"[TilesetLibrary] {msg}");
            EditorManager.Instance.errorHandler.DisplayError(msg);
            Tilesets = new Dictionary<string, TilesetData>(StringComparer.OrdinalIgnoreCase);
            TilesetNames = new List<string>();
        }
    }

    // Parses the "object" type field from each entity in data/spawns.gon
    private void LoadObjectTypes()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "data/spawns.gon").Replace('\\', '/');

        if (!File.Exists(path))
        {
            Debug.LogWarning("[TilesetLibrary] spawns.gon not found — static object type lookup unavailable.");
            staticObjectTypes = new Dictionary<int, string>();
            return;
        }

        try
        {
            string content = File.ReadAllText(path);
            staticObjectTypes = LevelDataParser.ExtractObjectTypes(content);
            Debug.Log($"[TilesetLibrary] Found {staticObjectTypes.Count} entity-to-object-type mappings.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilesetLibrary] Failed to extract object types: {ex.Message}");
            staticObjectTypes = new Dictionary<int, string>();
        }
    }

    // Sets the active tileset by name and syncs the value to EditorManager.
    // Call this when the user picks a biome from the dropdown.
    public void SetTileset(string tilesetName)
    {
        if (Tilesets.ContainsKey(tilesetName))
        {
            CurrentTileset = tilesetName;
            EditorManager.Instance.CurrentTileset = tilesetName;
            OnTilesetChanged?.Invoke(tilesetName);
            Debug.Log($"[TilesetLibrary] Tileset changed to: {tilesetName}");
        }
        else
        {
            Debug.LogWarning($"[TilesetLibrary] Unknown tileset: '{tilesetName}'");
        }
    }

    // Returns the asset name for a static entity in the current tileset.
    // e.g. uid 5003 (StaticTallA) in "boneyard" → "TallGraveRocks1"
    // Returns null if: no tileset is selected, uid isn't a static object,
    // or no matching slot is found in the current tileset.
    public string GetStaticAssetName(int uid)
    {
        if (string.IsNullOrEmpty(CurrentTileset)) return null;
        if (!Tilesets.TryGetValue(CurrentTileset, out TilesetData tileset)) return null;
        if (!staticObjectTypes.TryGetValue(uid, out string objectType)) return null;

        // Normalize both sides by stripping underscores and lowercasing so that
        // "StaticTallA" and "static_tall_a" both become "statictalla" and match correctly
        string normalizedType = objectType.Replace("_", "").ToLower();

        foreach (var slot in tileset.Slots)
        {
            if (slot.Key.Replace("_", "").ToLower() == normalizedType)
                return slot.Value;
        }

        return null;
    }

    // Returns true if the given uid is a static object (has an "object" type mapping)
    public bool IsStaticObject(int uid)
    {
        return staticObjectTypes != null && staticObjectTypes.ContainsKey(uid);
    }

    // Written by Claude
    // Returns the combat_background asset name for the current tileset
    // (e.g. "infiniteBG"), or null if none is set.
    public string GetCombatBackground()
    {
        if (string.IsNullOrEmpty(CurrentTileset)) return null;
        if (!Tilesets.TryGetValue(CurrentTileset, out TilesetData tileset)) return null;
        tileset.Slots.TryGetValue("combat_background", out string bg);
        return bg;
    }
}
