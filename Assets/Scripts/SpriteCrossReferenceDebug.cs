// Written by Claude
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Attach to any GameObject in the scene. Once EditorManager.AssetsLoaded becomes true,
// this script iterates all spawn and tile entries, checks direct sprite links and per-tileset
// sprite links, then writes one CSV to the project root. Runs only once per session.
//
// Output: <ProjectRoot>/sprite_cross_reference.csv
// Three sections: SPAWNS (ID/Name/Direct Sprite), TILES (same),
// STATIC OBJECTS (+ one column per tileset — every tileset column should be filled)
public class SpriteCrossReferenceDebug : MonoBehaviour
{
    // Drag in via Inspector (same pattern as LevelEntity/ItemBrowser)
    public SpriteLibrary spriteLibrary;
    public EntityDictionary ED;

    // How often (seconds) to poll until AssetsLoaded is true
    [Tooltip("Poll interval in seconds while waiting for assets to load")]
    public float pollInterval = 0.5f;

    // Guard so the export runs exactly once even if LoadToolbox fires multiple times
    private bool _exported = false;

    private void Start()
    {
        StartCoroutine(WaitThenExport());
    }

    // Written by Claude
    // Polls EditorManager.AssetsLoaded (set when both entities and images finish loading),
    // then triggers the export exactly once.
    private IEnumerator WaitThenExport()
    {
        while (EditorManager.Instance == null || !EditorManager.Instance.AssetsLoaded)
            yield return new WaitForSeconds(pollInterval);

        if (_exported) yield break;
        _exported = true;

        try
        {
            ExportCSV();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpriteCrossReferenceDebug.WaitThenExport] Export failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Written by Claude
    // Builds the CSV in memory then writes it to disk.
    // Columns: Type, ID, Name, Direct Sprite, plus one column per tileset (spawns only).
    private void ExportCSV()
    {
        string outputPath = Path.Combine(Application.dataPath, "..", "sprite_cross_reference.csv");

        // Collect tileset names once so column order is stable
        List<string> tilesetNames = TilesetLibrary.Instance != null
            ? new List<string>(TilesetLibrary.Instance.TilesetNames)
            : new List<string>();

        var sb = new StringBuilder();

        // Split spawns by name: entries starting with "Static" are tileset-driven assets,
        // everything else is a standard spawn looked up by direct sprite ID.
        var standardSpawns = new List<KeyValuePair<int, string>>();
        var staticSpawns   = new List<KeyValuePair<int, string>>();

        foreach (KeyValuePair<int, string> entry in ED.spawns)
        {
            if (entry.Key < 0) continue; // skip -1 Random, -2 Unset

            if (entry.Value.StartsWith("Static"))
                staticSpawns.Add(entry);
            else
                standardSpawns.Add(entry);
        }

        // --- SPAWNS section ---
        sb.AppendLine("SPAWNS");
        sb.AppendLine("ID,Name,Direct Sprite");
        foreach (KeyValuePair<int, string> entry in standardSpawns)
        {
            string directSprite = ResolveSpawnSpriteName(entry.Key, entry.Value);
            sb.AppendLine($"{entry.Key},{EscapeCsv(entry.Value)},{EscapeCsv(directSprite)}");
        }

        // --- TILES section ---
        sb.AppendLine();
        sb.AppendLine("TILES");
        sb.AppendLine("ID,Name,Direct Sprite");
        foreach (KeyValuePair<int, string> entry in ED.tiles)
        {
            string directSprite = ResolveTileSpriteName(entry.Key);
            sb.AppendLine($"{entry.Key},{EscapeCsv(entry.Value)},{EscapeCsv(directSprite)}");
        }

        // --- STATIC OBJECTS section (all tileset columns should be filled — gaps = missing assets) ---
        sb.AppendLine();
        sb.AppendLine("STATIC OBJECTS");
        sb.Append("ID,Name,Direct Sprite");
        foreach (string ts in tilesetNames)
            sb.Append($",{EscapeCsv(ts)}");
        sb.AppendLine();

        foreach (KeyValuePair<int, string> entry in staticSpawns)
        {
            string directSprite = ResolveSpawnSpriteName(entry.Key, entry.Value);
            sb.Append($"{entry.Key},{EscapeCsv(entry.Value)},{EscapeCsv(directSprite)}");
            foreach (string ts in tilesetNames)
                sb.Append($",{EscapeCsv(ResolveTilesetSpriteName(entry.Key, ts))}");
            sb.AppendLine();
        }

        try
        {
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[SpriteCrossReferenceDebug] CSV written to: {Path.GetFullPath(outputPath)} " +
                      $"({ED.spawns.Count} spawns, {ED.tiles.Count} tiles, {tilesetNames.Count} tilesets)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpriteCrossReferenceDebug.ExportCSV] Failed to write file: {ex.Message}");
        }
    }

    // Written by Claude
    // Returns the directly-linked sprite name (with .png) for a spawn, or empty if none.
    // Uses spawnImgExists to avoid treating the misc fallback sprite as a real match.
    private string ResolveSpawnSpriteName(int id, string entityName)
    {
        try
        {
            if (!spriteLibrary.spawnImgExists(id)) return string.Empty;
            Sprite sprite = spriteLibrary.findSpawnByID(id);
            return sprite != null ? sprite.name + ".png" : string.Empty;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SpriteCrossReferenceDebug.ResolveSpawnSpriteName] ID={id} name={entityName}: {ex.Message}");
            return string.Empty;
        }
    }

    // Written by Claude
    // Returns the tileset-specific asset name (with .png) for a spawn in the given tileset,
    // or empty if this entity has no slot in that biome.
    // Prefixes "MISSING: " if the asset name is defined but the PNG wasn't loaded from StreamingAssets.
    private string ResolveTilesetSpriteName(int id, string tilesetName)
    {
        if (TilesetLibrary.Instance == null) return string.Empty;
        try
        {
            string assetName = TilesetLibrary.Instance.GetStaticAssetName(id, tilesetName);
            if (string.IsNullOrEmpty(assetName)) return string.Empty;

            Sprite sprite = spriteLibrary.FindSpawnByName(assetName);
            return sprite != null ? assetName + ".png" : "MISSING: " + assetName + ".png";
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SpriteCrossReferenceDebug.ResolveTilesetSpriteName] ID={id} tileset={tilesetName}: {ex.Message}");
            return string.Empty;
        }
    }

    // Written by Claude
    // Returns the directly-linked sprite name (with .png) for a tile, or empty if none.
    private string ResolveTileSpriteName(int id)
    {
        try
        {
            if (!spriteLibrary.tileImgExists(id)) return string.Empty;
            Sprite sprite = spriteLibrary.findTileByID(id);
            return sprite != null ? sprite.name + ".png" : string.Empty;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SpriteCrossReferenceDebug.ResolveTileSpriteName] ID={id}: {ex.Message}");
            return string.Empty;
        }
    }

    // Wraps a CSV field in quotes if it contains a comma or double-quote.
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(",") || value.Contains("\""))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
