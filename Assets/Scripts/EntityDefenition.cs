using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EntityDefinition
{
    public int ID;
    public string Name;
    public int Category;
    public List<string> Images = new List<string>(); // Supports arrays like ["ground.png" "ground_spots.png"]
    
    // Helper to get the primary image
    public string PrimaryImage => Images.Count > 0 ? Images[0] : "empty.png";
}