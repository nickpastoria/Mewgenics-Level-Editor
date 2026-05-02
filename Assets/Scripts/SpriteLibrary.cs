using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SpriteLibrary : MonoBehaviour
{
    public Sprite[] tileLibrary;
    public Sprite[] spawnLibrary;
    public Sprite[] misc;
    public EntityDictionary ED;
    public String SpawnsFileLocation = "spawns/";
    public String TilesFileLocation = "tiles/";

    // Written by Claude
    // Normalized-name lookup for FindSpawnByName — built once after loading,
    // avoids a linear scan on every static sprite lookup.
    // Key: sprite name with spaces stripped and lowercased (e.g. "tallcaverock2")
    private Dictionary<string, Sprite> spawnByNormalizedName;

    // Written by Claude
    // async void Start: both spawn and tile directories are read in parallel on background
    // threads, then Texture2D/Sprite objects are created on the main thread (Unity requirement).
    async void Start()
    {
        // Kick off both loads simultaneously — they run in parallel on the thread pool
        Task<Sprite[]> spawnTask = LoadAssetsAsync(SpawnsFileLocation);
        Task<Sprite[]> tileTask = LoadAssetsAsync(TilesFileLocation);

        await Task.WhenAll(spawnTask, tileTask);

        // Back on main thread: assign results
        spawnLibrary = spawnTask.Result;
        tileLibrary = tileTask.Result;
        misc = Resources.LoadAll<Sprite>("unkown");

        // Build the normalized-name dictionary now that spawnLibrary is populated
        BuildSpawnNameCache();

        EditorManager.Instance.ImagesLoaded = true;
        EditorManager.Instance.LoadToolbox();
    }

    // Written by Claude
    // Builds a dictionary keyed by normalized sprite name (spaces stripped, lowercased)
    // so FindSpawnByName is O(1) instead of O(n).
    private void BuildSpawnNameCache()
    {
        spawnByNormalizedName = new Dictionary<string, Sprite>();
        foreach (Sprite spawn in spawnLibrary)
        {
            if (spawn == null) continue;
            string key = spawn.name.Replace(" ", "").ToLower();
            // Last writer wins if two sprites normalize to the same key
            spawnByNormalizedName[key] = spawn;
        }
        Debug.Log($"[SpriteLibrary] Built name cache with {spawnByNormalizedName.Count} entries.");
    }

    // Written by Claude
    // Reads all PNG files from the given folder in parallel on background threads,
    // then creates Texture2D and Sprite objects on the main thread (Unity API requirement).
    // Each file read also reads the matching .txt pivot file on the same background thread.
    private async Task<Sprite[]> LoadAssetsAsync(string location)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, location);
        filePath = filePath.Replace('\\', '/');
        Debug.Log($"[SpriteLibrary] Loading image assets from: {filePath}");

        if (!Directory.Exists(filePath))
        {
            Debug.LogWarning($"[SpriteLibrary] Folder not found: {filePath}");
            return Array.Empty<Sprite>();
        }

        string[] allPngFiles = Directory.GetFiles(filePath, "*.png", SearchOption.AllDirectories);

        // Kick off all file reads in parallel — each returns (path, pngBytes, pivotPoints)
        var readTasks = allPngFiles.Select(imagePath => Task.Run<(string path, byte[] bytes, List<float> pivots)>(() =>
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            List<float> pivots = ReadPivotPoints(imagePath); // also file I/O — safe on thread pool
            return (imagePath, bytes, pivots);
        })).ToArray();

        // Wait for all reads to finish (main thread stays free during this)
        (string path, byte[] bytes, List<float> pivots)[] results = await Task.WhenAll(readTasks);

        // Back on main thread: create Unity objects from the loaded bytes
        List<Sprite> sprites = new List<Sprite>();
        foreach (var (path, bytes, pivots) in results)
        {
            if (bytes == null) continue;
            try
            {
                Texture2D tex = new Texture2D(2, 2); // dimensions auto-adjusted by LoadImage
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                Rect rect = new Rect(0f, 0f, tex.width, tex.height);
                Vector2 pivot = new Vector2(pivots[0], pivots[1]);
                Sprite sprite = Sprite.Create(tex, rect, pivot, 100f);

                string name = Path.GetFileName(path);
                sprite.name = name.Remove(name.Length - 4); // strip .png extension
                sprites.Add(sprite);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteLibrary] Failed to create sprite for '{path}': {ex.Message}");
            }
        }

        Debug.Log($"[SpriteLibrary] Loaded {sprites.Count} sprites from {location}.");
        return sprites.ToArray();
    }

    // Written by Claude
    // Finds a spawn sprite by its asset name (e.g. "TallGraveRocks1").
    // Uses a pre-built normalized dictionary (O(1)) so that
    // "TallCaveRock2" matches a file named "Tall Cave Rock 2.png".
    // Returns null if not found so the caller can fall back to a default.
    public Sprite FindSpawnByName(string assetName)
    {
        if (spawnByNormalizedName == null) return null;
        string key = assetName.Replace(" ", "").ToLower();
        spawnByNormalizedName.TryGetValue(key, out Sprite sprite);
        return sprite;
    }

    public Sprite findSpawnByID(int ID)
    {
        string spawnName = ED.spawns[ID];
        string normalSpawnName = spawnName.Replace(" ", "").ToLower();
        foreach (Sprite spawn in spawnLibrary)
        {
            // Check for ID'd images first so we can separate items like harpoons that all have the same name
            if (spawn.name == $"{ID}") return spawn;
            string normalSpriteName = spawn.name.Replace(" ", "").ToLower();
            if (normalSpriteName == normalSpawnName ||
                normalSpawnName.Contains($"({normalSpriteName})") ||
                normalSpriteName == normalSpawnName + "portrait")
            {
                return spawn;
            }
        }
        return misc[0];
    }

    public Sprite findTileByID(int ID)
    {
        foreach (Sprite tile in tileLibrary)
        {
            if (tile.name == $"{ID}")
            {
                return tile;
            }
        }
        return misc[0];
    }

    public bool tileImgExists(int ID)
    {
        foreach (Sprite tile in tileLibrary)
        {
            if (tile.name == $"{ID}")
            {
                return true;
            }
        }
        return false;
    }

    public bool spawnImgExists(int ID)
    {
        string spawnName = ED.spawns[ID];
        string normalSpawnName = spawnName.Replace(" ", "").ToLower();
        foreach (Sprite spawn in spawnLibrary)
        {
            // Check for ID'd images first so we can separate items like harpoons that all have the same name
            if (spawn.name == $"{ID}") return true;
            string normalSpriteName = spawn.name.Replace(" ", "").ToLower();
            if (normalSpriteName == normalSpawnName ||
                normalSpawnName.Contains($"({normalSpriteName})") ||
                normalSpriteName == normalSpawnName + "portrait")
            {
                return true;
            }
        }
        return false;
    }

    public static List<float> ReadPivotPoints(string imagePath)
    {
        string txtPath = Path.ChangeExtension(imagePath, ".txt");

        if (!File.Exists(txtPath))
        {
            Debug.LogWarning($"Pivot file not found: {txtPath}");
            return new List<float> {0.5f, 0.5f};
        }

        string[] tokens = File.ReadAllText(txtPath).Trim().Split(' ');
        List<float> pivots = new List<float>();

        foreach (string token in tokens)
        {
            if (float.TryParse(token, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                pivots.Add(value / 100f);  // convert % to 0-1 range
            }
        }

        return pivots;
    }
}
