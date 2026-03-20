using UnityEditor;
using UnityEngine;
using System.IO;
public class SpritePixelPivotImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;

        // Force the importer to get the actual texture dimensions
        int width = 0;
        int height = 0;
        textureImporter.GetSourceTextureWidthAndHeight(out width, out height);

        // CHECK: If the meta file already exists, it's not a new import.
        // We stop here so we don't overwrite your manual changes!
        string metaPath = assetPath + ".meta";
        if (File.Exists(metaPath)) 
        {
            return; 
        }

        Vector2 normalizedPivot = new Vector2(0.5f, 35.0f/height);

        // Apply settings
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Single;
        textureImporter.filterMode = FilterMode.Point;

        TextureImporterSettings settings = new TextureImporterSettings();
        textureImporter.ReadTextureSettings(settings);
        
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        textureImporter.SetTextureSettings(settings);

        textureImporter.spritePivot = normalizedPivot;
    }
}
