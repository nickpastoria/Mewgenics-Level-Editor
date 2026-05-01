using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Written by Claude
// Loads and displays the combat background image for the current tileset.
// All backgrounds are pre-loaded at startup in parallel so tileset switching is instant.
//
// Setup:
//   1. Add this script to a GameObject in the scene (e.g. "Background").
//   2. Make sure it has a SpriteRenderer component — set its sorting order
//      low (e.g. -100) so it renders behind everything else.
//   3. No manual wiring needed — it finds TilesetLibrary automatically.
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundManager : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // All backgrounds keyed by folder name (e.g. "infiniteBG" → Sprite).
    // Pre-loaded at startup so switching tilesets requires only a dictionary lookup.
    private Dictionary<string, Sprite> backgroundCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    // Written by Claude
    // async void Start: file reads run in parallel on background threads,
    // texture/sprite creation finishes on the main thread before ApplyBackground is called.
    async void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (TilesetLibrary.Instance != null)
            TilesetLibrary.Instance.OnTilesetChanged += OnTilesetChanged;
        else
            Debug.LogWarning("[BackgroundManager] TilesetLibrary not found.");

        await PreloadAllBackgroundsAsync();

        // Apply current background now that the cache is fully populated
        if (TilesetLibrary.Instance != null)
            ApplyBackground(TilesetLibrary.Instance.GetCombatBackground());
    }

    void OnDestroy()
    {
        if (TilesetLibrary.Instance != null)
            TilesetLibrary.Instance.OnTilesetChanged -= OnTilesetChanged;

        foreach (var sprite in backgroundCache.Values)
        {
            if (sprite != null) Destroy(sprite.texture);
        }
    }

    // Written by Claude
    // Reads all PNG files from StreamingAssets/bgs/ in parallel on background threads,
    // then creates Texture2D and Sprite objects on the main thread (Unity API requirement).
    private async Task PreloadAllBackgroundsAsync()
    {
        string bgsRoot = Path.Combine(Application.streamingAssetsPath, "bgs").Replace('\\', '/');

        if (!Directory.Exists(bgsRoot))
        {
            Debug.LogWarning($"[BackgroundManager] bgs folder not found: {bgsRoot}");
            return;
        }

        string[] bgFolders = Directory.GetDirectories(bgsRoot);

        // Kick off all file reads in parallel — each returns (folderKey, fileBytes)
        var readTasks = bgFolders.Select(folder =>
        {
            string[] pngs = Directory.GetFiles(folder, "*.png");
            if (pngs.Length == 0) return Task.FromResult<(string, byte[])>((null, null));
            string key = Path.GetFileName(folder);
            string pngPath = pngs[0];
            return Task.Run<(string, byte[])>(() => (key, File.ReadAllBytes(pngPath)));
        }).ToArray();

        // Wait for all reads to complete (runs on thread pool, main thread stays free)
        (string key, byte[] bytes)[] results = await Task.WhenAll(readTasks);

        // Back on main thread: create Unity objects from the loaded bytes
        int loaded = 0;
        foreach (var (key, bytes) in results)
        {
            if (key == null || bytes == null) continue;
            try
            {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                backgroundCache[key] = sprite;
                loaded++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackgroundManager] Failed to create sprite for '{key}': {ex.Message}");
            }
        }

        Debug.Log($"[BackgroundManager] Pre-loaded {loaded} backgrounds.");
    }

    private void OnTilesetChanged(string newTileset)
    {
        ApplyBackground(TilesetLibrary.Instance.GetCombatBackground());
    }

    // Dictionary lookup only — no disk I/O
    private void ApplyBackground(string bgName)
    {
        if (string.IsNullOrEmpty(bgName)) { spriteRenderer.sprite = null; return; }

        if (backgroundCache.TryGetValue(bgName, out Sprite sprite))
            spriteRenderer.sprite = sprite;
        else
        {
            Debug.LogWarning($"[BackgroundManager] No cached background for '{bgName}'.");
            spriteRenderer.sprite = null;
        }
    }
}
