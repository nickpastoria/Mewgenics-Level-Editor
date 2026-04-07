using System.Collections.Generic;

// Written by Claude
// Plain data class representing one biome/tileset from tilesets.gon.
// Slots maps the slot key (e.g. "static_tall_a") to the asset name
// used in the current tileset (e.g. "TallGraveRocks1").
[System.Serializable]
public class TilesetData
{
    public string Name;
    public Dictionary<string, string> Slots = new Dictionary<string, string>();
}
