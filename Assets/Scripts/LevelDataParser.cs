using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LevelDataParser
{
    public static Dictionary<int, string> ExtractNames(string fileContent)
    {
        var entityDictionary = new Dictionary<int, string>();

        // The Regex pattern
        // ^(\d+)            : Group 1 - Matches the ID at the start of a line
        // \s*\{\s*editor\s*\{ : Matches the opening braces for the main block and the 'editor' block
        // [^}]*?            : Lazily matches anything EXCEPT a closing brace (keeps us inside the editor block)
        // name\s+""([^""]+)"" : Group 2 - Matches 'name', whitespace, and captures the text inside the quotes
        string pattern = @"^(-?\d+)\s*\{\s*editor\s*\{[^}]*?name\s+""([^""]+)""";

        // RegexOptions.Multiline lets '^' match the beginning of each line, not just the whole string.
        MatchCollection matches = Regex.Matches(fileContent, pattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                // Parse the ID (Group 1)
                if (int.TryParse(match.Groups[1].Value, out int id))
                {
                    // Grab the Name (Group 2)
                    string name = match.Groups[2].Value;
                    
                    // Add to dictionary
                    entityDictionary[id] = name;
                }
            }
        }
        return entityDictionary;
    }
}