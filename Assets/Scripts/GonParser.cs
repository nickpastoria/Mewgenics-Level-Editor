using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class GonParser
{
    public static Dictionary<int, EntityDefinition> Parse(string filePath)
    {
        Dictionary<int, EntityDefinition> dictionary = new Dictionary<int, EntityDefinition>();
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Could not find .gon file at: {filePath}");
            return dictionary;
        }

        string[] lines = File.ReadAllLines(filePath);
        EntityDefinition currentDef = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            // 1. Check for a new ID block (e.g., "11 {" or "11{")
            Match idMatch = Regex.Match(line, @"^(\d+)\s*\{");
            if (idMatch.Success)
            {
                currentDef = new EntityDefinition();
                currentDef.ID = int.Parse(idMatch.Groups[1].Value);
                dictionary[currentDef.ID] = currentDef;
                continue;
            }

            // 2. If we are currently inside an ID block, extract the properties
            if (currentDef != null)
            {
                // Extract Name: name "Rat"
                Match nameMatch = Regex.Match(line, @"name\s+""([^""]+)""");
                if (nameMatch.Success)
                {
                    currentDef.Name = nameMatch.Groups[1].Value;
                }

                // Extract Category: category 3 or category -100
                Match catMatch = Regex.Match(line, @"category\s+(-?\d+)");
                if (catMatch.Success)
                {
                    currentDef.Category = int.Parse(catMatch.Groups[1].Value);
                }

                // Extract Images: image "rat.png" OR image ["img1.png" "img2.png"]
                if (line.StartsWith("image "))
                {
                    // This regex finds anything ending in .png wrapped in quotes
                    MatchCollection imgMatches = Regex.Matches(line, @"""([^""]+\.png)""");
                    foreach (Match m in imgMatches)
                    {
                        currentDef.Images.Add(m.Groups[1].Value);
                    }
                }
            }
        }

        Debug.Log($"Parsed {dictionary.Count} definitions from .gon file.");
        return dictionary;
    }
}