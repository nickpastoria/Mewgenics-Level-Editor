using UnityEngine;
using System;
using System.IO;

// Written by Claude
// Loads and displays the combat background image for the current tileset.
// The background is loaded from StreamingAssets/bgs/{combat_background_name}/
// and applied to a SpriteRenderer on this GameObject.
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

    // Cached texture so we can clean it up when switching backgrounds
    private Texture2D currentTexture;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (TilesetLibrary.Instance != null)
        {
            TilesetLibrary.Instance.OnTilesetChanged += OnTilesetChanged;
            LoadBackground(TilesetLibrary.Instance.GetCombatBackground());
        }
        else
        {
            Debug.LogWarning("[BackgroundManager] TilesetLibrary not found.");
        }
    }

    void OnDestroy()
    {
        if (TilesetLibrary.Instance != null)
            TilesetLibrary.Instance.OnTilesetChanged -= OnTilesetChanged;

        // Clean up the loaded texture to avoid memory leaks
        if (currentTexture != null)
            Destroy(currentTexture);
    }

    private void OnTilesetChanged(string newTileset)
    {
        LoadBackground(TilesetLibrary.Instance.GetCombatBackground());
    }

    // Written by Claude
    // Looks for a PNG inside StreamingAssets/bgs/{bgName}/ and applies it
    // as the SpriteRenderer's sprite. Falls back gracefully if folder or
    // file doesn't exist.
    private void LoadBackground(string bgName)
    {
        if (string.IsNullOrEmpty(bgName))
        {
            spriteRenderer.sprite = null;
            return;
        }

        string folderPath = Path.Combine(Application.streamingAssetsPath, "bgs", bgName)
                                .Replace('\\', '/');

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[BackgroundManager] Background folder not found: {folderPath}");
            spriteRenderer.sprite = null;
            return;
        }

        // Find the first PNG in the folder — each bg folder contains exactly one image
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
        if (pngFiles.Length == 0)
        {
            Debug.LogWarning($"[BackgroundManager] No PNG found in: {folderPath}");
            spriteRenderer.sprite = null;
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(pngFiles[0]);

            // Destroy the previous texture before creating a new one
            if (currentTexture != null)
                Destroy(currentTexture);

            currentTexture = new Texture2D(2, 2);
            currentTexture.LoadImage(bytes);
            currentTexture.filterMode = FilterMode.Point;
            currentTexture.wrapMode = TextureWrapMode.Clamp;

            Rect rect = new Rect(0, 0, currentTexture.width, currentTexture.height);
            // Pivot at centre so the background is easy to position
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite newSprite = Sprite.Create(currentTexture, rect, pivot, 100f);

            spriteRenderer.sprite = newSprite;
            Debug.Log($"[BackgroundManager] Loaded background: {Path.GetFileName(pngFiles[0])}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BackgroundManager] Failed to load background '{bgName}': {ex.Message}");
            spriteRenderer.sprite = null;
        }
    }
}
