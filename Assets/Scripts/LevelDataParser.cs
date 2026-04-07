using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class LevelDataParser
{
    // Parses entity names and IDs from spawns.gon / tiles.gon.
    // Matches entries of the form:
    //   123 { editor { name "Some Name" } }
    public static Dictionary<int, string> ExtractNames(string fileContent)
    {
        var entityDictionary = new Dictionary<int, string>();

        // ^(-?\d+)             : Group 1 — ID at the start of a line
        // \s*\{\s*editor\s*\{  : opening braces for the main block and 'editor' sub-block
        // [^}]*?               : anything inside editor (non-greedy, stays inside)
        // name\s+""([^""]+)"" : Group 2 — the name value
        string pattern = @"^(-?\d+)\s*\{\s*editor\s*\{[^}]*?name\s+""([^""]+)""";

        MatchCollection matches = Regex.Matches(fileContent, pattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                entityDictionary[id] = match.Groups[2].Value;
        }
        return entityDictionary;
    }

    // Written by Claude
    // Strips // line comments so the parsers below don't misinterpret them.
    private static string StripLineComments(string content)
    {
        return Regex.Replace(content, @"//[^\n]*", "");
    }

    // Written by Claude
    // Strips all content inside nested {} blocks, leaving only the top-level text.
    // Used to isolate simple key-value pairs from blocks that also contain sub-blocks.
    private static string RemoveNestedBlocks(string content)
    {
        var sb = new StringBuilder();
        int depth = 0;
        foreach (char c in content)
        {
            if (c == '{') { depth++; continue; }
            if (c == '}') { depth--; continue; }
            if (depth == 0) sb.Append(c);
        }
        return sb.ToString();
    }

    // Written by Claude
    // Extracts top-level named blocks from a .gon file into a dictionary of
    // block name → block content (the text between { and its matching }).
    // Lines that are plain "key value" pairs without a block are skipped.
    // Used to parse the tileset blocks from tilesets.gon.
    private static Dictionary<string, string> ExtractTopLevelBlocks(string content)
    {
        content = StripLineComments(content);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int i = 0;

        while (i < content.Length)
        {
            // Skip whitespace
            while (i < content.Length && char.IsWhiteSpace(content[i])) i++;
            if (i >= content.Length) break;

            // Read an identifier (tileset name)
            int nameStart = i;
            while (i < content.Length && (char.IsLetterOrDigit(content[i]) || content[i] == '_'))
                i++;

            if (i == nameStart) { i++; continue; } // non-identifier character — skip

            string blockName = content.Substring(nameStart, i - nameStart);

            // Skip whitespace
            while (i < content.Length && char.IsWhiteSpace(content[i])) i++;
            if (i >= content.Length) break;

            if (content[i] != '{')
            {
                // Plain "key value" line (e.g. "default_tileset alley") — skip to end of line
                while (i < content.Length && content[i] != '\n') i++;
                continue;
            }

            // Track braces to find the matching closing brace
            int depth = 1;
            int blockStart = i + 1;
            i++;
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}') depth--;
                i++;
            }

            result[blockName] = content.Substring(blockStart, i - blockStart - 1);
        }

        return result;
    }

    // Written by Claude
    // Parses tilesets.gon into a dictionary of tileset name → TilesetData.
    // Each TilesetData.Slots contains the simple key-value pairs from that tileset block
    // (e.g. "static_tall_a" → "TallGraveRocks1"), with nested sub-blocks stripped out.
    public static Dictionary<string, TilesetData> ParseTilesets(string content)
    {
        var result = new Dictionary<string, TilesetData>(StringComparer.OrdinalIgnoreCase);

        // Only match simple "key singleWord" lines — lines with brackets or no value are skipped
        var kvPattern = new Regex(@"^\s*(\w+)\s+(\w+)\s*$", RegexOptions.Multiline);

        foreach (var block in ExtractTopLevelBlocks(content))
        {
            // Strip nested sub-blocks (global_objects {}, reverb {}, etc.)
            // so we only match top-level key-value pairs like "static_tall_a PowerPole"
            string topLevelContent = RemoveNestedBlocks(block.Value);

            var slots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in kvPattern.Matches(topLevelContent))
                slots[m.Groups[1].Value] = m.Groups[2].Value;

            result[block.Key] = new TilesetData { Name = block.Key, Slots = slots };
        }

        return result;
    }

    // Written by Claude
    // Extracts the "object" type for each entity in spawns.gon.
    // Returns uid → object type string (e.g. 5003 → "StaticTallA").
    // Only entities that have an "object" field at the top level of their block are included
    // (i.e. excludes entities whose object field is inside editor {}).
    public static Dictionary<int, string> ExtractObjectTypes(string content)
    {
        content = StripLineComments(content);
        var result = new Dictionary<int, string>();
        int i = 0;

        while (i < content.Length)
        {
            // Skip whitespace
            while (i < content.Length && char.IsWhiteSpace(content[i])) i++;
            if (i >= content.Length) break;

            // Try to read an integer ID
            if (!char.IsDigit(content[i])) { i++; continue; }

            int numStart = i;
            while (i < content.Length && char.IsDigit(content[i])) i++;
            if (!int.TryParse(content.Substring(numStart, i - numStart), out int id)) continue;

            // Skip whitespace; expect '{'
            while (i < content.Length && char.IsWhiteSpace(content[i])) i++;
            if (i >= content.Length || content[i] != '{') continue;

            // Extract the full entity block content
            int depth = 1;
            int blockStart = i + 1;
            i++;
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}') depth--;
                i++;
            }
            string blockContent = content.Substring(blockStart, i - blockStart - 1);

            // Strip nested sub-blocks (editor {}) so we only search the outer level
            string topLevel = RemoveNestedBlocks(blockContent);

            var objMatch = Regex.Match(topLevel, @"^\s*object\s+(\w+)", RegexOptions.Multiline);
            if (objMatch.Success)
                result[id] = objMatch.Groups[1].Value;
        }

        return result;
    }
}
