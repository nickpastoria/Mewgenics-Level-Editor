using UnityEngine;
using System.Text;
using System.IO;
using System;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using SimpleFileBrowser;
using System.ComponentModel;
using System.Diagnostics;
using TMPro;


public class LevelManager : MonoBehaviour
{
    public  TMP_Text ProjectLabel;
    public TMP_Text LevelLabel;
    public Grid grid;
    public GameObject LevelEntity;
    public GameObject Inspector;
    private string fileDest;
    private Level level;
    private List<GameObject> UnityObjects = new List<GameObject>();

    private PersistentVariables sysVars;
    private string levelsLocation;
    private string CurrentLevel = "New Level";

    [System.Serializable]
    public struct randomSpawn
    {
        public int uid;
        public int weight;
    }

    [System.Serializable]
    public struct Spawn {
        public int x;
        public int y;
        public int uid;
        public int wave;
        //implement random elements in here
        public int randomCount;
        public int rollIndex;
        public randomSpawn[] spawns;
    }

    struct Level {
        public int version;
        public int width;
        public int height;
        public int layers;
        public int nSpawns;
        public int camx;
        public int camy;
        public int camw;
        public int camh;
        public string spawns;
        public string tiles;
        public List<List<int>> groundLayer;
        public List<Spawn> entityList;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateEmptyLevel();
        sysVars = EditorManager.Instance.SysVars;
        levelsLocation = $"{sysVars.defaultFileLocation}";
    }

    public void CreateEmptyLevel()
    {
        level = new Level();
        level.version = 2;
        level.width = 10;
        level.height = 10;
        level.layers = 1;
        level.nSpawns = 0;
        level.camx = 0;
        level.camy = 0;
        level.camw = 10;
        level.camh = 10;
        level.spawns = "spawns.gon";
        level.tiles = "tiles.gon";
        fillGround(0);
        level.entityList = new List<Spawn>();
        updateLevel();
        CurrentLevel = "New Level";
        EditorManager.Instance.UpdateLevelLabel(CurrentLevel);
    }

    private void fillGround(int value)
    {
        level.groundLayer = new List<List<int>>();
        for (int x = 0; x < 10; x++)
        {       
            List<int> currentRow = new List<int>(level.width);
            for (int i = 0; i < level.width; i++)
            {
                currentRow.Add(0);
            }
            level.groundLayer.Add(currentRow);
        } 
    }

    private void clearLevel()
    {
        foreach (GameObject obj in UnityObjects)
        {
            Destroy(obj);
        }
        UnityObjects.Clear();
    }

    public void updateTiles()
    {
        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                int tileId = level.groundLayer[y][x];
                if (tileId == 0) continue;
                GameObject tile = Instantiate(LevelEntity, grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                tile.GetComponent<LevelEntity>().Create(0, tileId);
                UnityEngine.Debug.Log($"Created tile at ({x}, {y}) with ID {tileId}");
                UnityObjects.Add(tile);
            }
        }
    }

    public void updateSpawns()
    {
        foreach (Spawn spawn in level.entityList)
        {
            GameObject s = Instantiate(LevelEntity, grid.GetCellCenterWorld(new Vector3Int(spawn.x, spawn.y, 0)), Quaternion.identity);
            s.GetComponent<LevelEntity>().Create(20 - (spawn.x + spawn.y), spawn.uid, spawn);
            UnityEngine.Debug.Log($"Created entity at ({spawn.x}, {spawn.y}) with ID {spawn.uid}");
            UnityObjects.Add(s);
        }
    }

    public bool DeleteSpawnAtLocation(int x, int y)
    {
        foreach(Spawn spawn in level.entityList)
        {
            if (spawn.x == x && spawn.y == y)
            {
                level.entityList.Remove(spawn);
                updateLevel();
                return true;
            }
        }
        return false;
    }

    public bool spawnLocFree(int x, int y)
    {
        foreach(Spawn spawn in level.entityList)
        {
            if (spawn.x == x && spawn.y == y)
            {
                return false;
            }
        }
        return true;
    }
    public Spawn GetSpawnAtLoc(int x, int y)
    {
        for(int i = 0; i < level.entityList.Count; i++)
        {
            Spawn spawn = level.entityList[i];
            if (spawn.x == x && spawn.y == y)
            {
                return spawn;
            }
        }
        return new Spawn();
    }

    public void setTile(int ID, Vector3Int position)
    {
        level.groundLayer[position.y][position.x] = ID;
        clearLevel();
        updateTiles();
        updateSpawns();
    }

    public void setSpawn(int ID, Vector3Int position)
    {
        if(spawnLocFree(position.x, position.y))
        {
            Spawn newEntity = new Spawn();
            newEntity.x = position.x;
            newEntity.y = position.y;
            newEntity.uid = ID;
            level.entityList.Add(newEntity);
            clearLevel();
            updateTiles();
            updateSpawns();
        }
    }
    

    public void updateLevel()
    {
        clearLevel();
        updateTiles();
        updateSpawns();
        
    }

    private void LoadLevel()
    {
        levelsLocation = $"{sysVars.defaultFileLocation}";
        StartCoroutine(LevelWindow());
    }

    public void SetProject()
    {
        levelsLocation = $"{sysVars.defaultFileLocation}";
        StartCoroutine(SetProjectWindow());
    }

    private void DecodeLevel(byte[] file)
    {
        //Written by Gemini
        level = new Level();

        using (MemoryStream ms = new MemoryStream(file))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            UnityEngine.Debug.Log($"--- START DECODE: File Size {file.Length} bytes ---");

            level.version = reader.ReadInt32();
            level.width = reader.ReadInt32();
            level.height = reader.ReadInt32();
            level.layers = reader.ReadInt32();
            level.nSpawns = reader.ReadInt32();
            level.camx = reader.ReadInt32();
            level.camy = reader.ReadInt32();
            level.camw = reader.ReadInt32();
            level.camh = reader.ReadInt32();

            UnityEngine.Debug.Log($"Header Parsed @ {reader.BaseStream.Position}: v{level.version}, Size:{level.width}x{level.height}, Layers:{level.layers}, Spawns:{level.nSpawns}, CamPos:({level.camx},{level.camy}), CamSize:{level.camw}x{level.camh}");

            // Read Spawns string
            int lenStr = reader.ReadInt32();
            UnityEngine.Debug.Log($"Spawns String Length: {lenStr} Parsed @ {reader.BaseStream.Position}");
            if (lenStr == 0) {
                level.spawns = "spawns.gon";
            } else {
                level.spawns = Encoding.UTF8.GetString(reader.ReadBytes(lenStr));
            }
            UnityEngine.Debug.Log($"Spawns String '{level.spawns}' Parsed @ {reader.BaseStream.Position}");

            // Read Tiles string
            lenStr = reader.ReadInt32();
            if (lenStr == 0) {
                level.tiles = "tiles.gon";
            } else {
                level.tiles = Encoding.UTF8.GetString(reader.ReadBytes(lenStr));
            }
            UnityEngine.Debug.Log($"Tiles String '{level.tiles}' Parsed @ {reader.BaseStream.Position}");

            // Skip 8 bytes
            reader.BaseStream.Position += 8;
            UnityEngine.Debug.Log($"Skipped 8 bytes. Now @ {reader.BaseStream.Position}");

            level.groundLayer = new List<List<int>>();

            // Decode tile layers (We must read all layers to keep stream aligned)
            for (int l = 0; l < level.layers; l++)
            {
                UnityEngine.Debug.Log($"Reading Layer {l} starting @ {reader.BaseStream.Position}...");
                bool isGroundLayer = (l == 0);

                for (int y = 0; y < level.height; y++)
                {
                    List<int> currentRow = new List<int>(level.width);

                    for (int x = 0; x < level.width; x++)
                    {
                        // Use UInt16 so 0xFFFF evaluates correctly!
                        int tileId = reader.ReadUInt16();

                        // Handle randomized tiles
                        if (tileId == 0xFFFF)
                        {
                            int numPossibilities = reader.ReadUInt16();
                            int rollIndex = reader.ReadUInt16();
                            
                            for (int n = 0; n < numPossibilities; n++)
                            {
                                int rId = reader.ReadUInt16();
                                int rWeight = reader.ReadUInt16();
                            }
                        }
                        if (isGroundLayer) currentRow.Add(tileId);
                    }

                    if (isGroundLayer) level.groundLayer.Add(currentRow);
                }
            }

            UnityEngine.Debug.Log($"Finished Layers. Starting Spawns @ {reader.BaseStream.Position}...");

            // Decode spawns
            level.entityList = new List<Spawn>(level.nSpawns);
            for (int i = 0; i < level.nSpawns; i++)
            {
                Spawn spawn = new Spawn();
                spawn.x = reader.ReadUInt16();
                spawn.y = reader.ReadUInt16();
                
                // Use UInt16 so 0xFFFF evaluates correctly!
                spawn.uid = reader.ReadUInt16(); 
                spawn.wave = reader.ReadUInt16(); 
                //reader.BaseStream.Position += 1; // skip(1) reserved byte

                UnityEngine.Debug.Log($"Parsed Spawn {i}: ({spawn.x}, {spawn.y}), UID: {spawn.uid}, Wave: {spawn.wave} @ {reader.BaseStream.Position}");

                if (spawn.uid < 11) 
                {
                    spawn.uid = 1;
                }

                if (spawn.uid == 0xFFFF)
                {
                    spawn.uid = -1;
                    spawn.randomCount = reader.ReadUInt16();
                    //spawn.rollIndex = reader.ReadUInt16();

                    randomSpawn[] rSpawn = new randomSpawn[spawn.randomCount];
                    
                    for (int n = 0; n < spawn.randomCount; n++)
                    {
                        randomSpawn newitem = new randomSpawn();
                        newitem.uid = reader.ReadUInt16();
                        newitem.weight = reader.ReadUInt16();
                        rSpawn[n] = newitem;
                    }
                    spawn.spawns = rSpawn;
                }
                level.entityList.Add(spawn);
            }

            UnityEngine.Debug.Log($"--- DECODE COMPLETE @ {reader.BaseStream.Position} ---");
        }

        // currentLevel = level;
        UnityEngine.Debug.Log("Level loaded successfully!");
        updateLevel();
        EditorManager.Instance.mouseEnabled = true;
    }

    IEnumerator LevelWindow()
    {
        EditorManager.Instance.mouseEnabled = false;
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Levels", ".lvl", ".lvl" ), new FileBrowser.Filter( "Text Files", ".txt", ".pdf" ) );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".lvl" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
        string defaultFileLocation = levelsLocation;
		FileBrowser.AddQuickLink("Users", defaultFileLocation, null );

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, defaultFileLocation, null, "Select Files", "Load" );

        if( FileBrowser.Success )
        {
            fileDest = FileBrowser.Result[0];
            byte[] file = System.IO.File.ReadAllBytes(fileDest);
            UnityEngine.Debug.Log("File destination: " + fileDest);
            DecodeLevel(file);
            CurrentLevel = fileDest.Split("\\")[^1];
            EditorManager.Instance.UpdateLevelLabel(CurrentLevel);
            
        }
        else
        {
            EditorManager.Instance.mouseEnabled = true;
        }
    }

    public void SaveWindow()
    {
        levelsLocation = $"{sysVars.defaultFileLocation}";
        EditorManager.Instance.mouseEnabled = false;
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Levels", ".lvl", ".lvl" ), new FileBrowser.Filter( "Text Files", ".txt", ".pdf" ) );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".lvl" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
		FileBrowser.AddQuickLink( "Users", SaveSystem.LoadSettings().defaultFileLocation, null );

        // Save file/folder: file, Allow multiple selection: false
		// Initial path: "C:\", Initial filename: "Screenshot.png"
		// Title: "Save As", Submit button text: "Save"
        string defaultFileLocation = levelsLocation;
		FileBrowser.ShowSaveDialog( ( paths ) => {
            UnityEngine.Debug.Log( "Selected: " + paths[0] );
            SaveLevel(paths[0]);
            CurrentLevel = paths[0].Split("\\")[^1];
            EditorManager.Instance.UpdateLevelLabel(CurrentLevel);
            }, () => { UnityEngine.Debug.Log( "Canceled" ); EditorManager.Instance.mouseEnabled = true;}, FileBrowser.PickMode.Files, false, defaultFileLocation, CurrentLevel, "Save As", "Save" );
    }
    
    private void SaveLevel(string filePath)
    {
        //Writen by Claude
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            UnityEngine.Debug.Log($"--- START ENCODE ---");

            // Header
            writer.Write(level.version);
            writer.Write(level.width);
            writer.Write(level.height);
            writer.Write(level.layers);
            writer.Write(level.entityList.Count);
            writer.Write(level.camx);
            writer.Write(level.camy);
            writer.Write(level.camw);
            writer.Write(level.camh);

            UnityEngine.Debug.Log($"Header written @ {ms.Position}");

            // Spawns string
            if (string.IsNullOrEmpty(level.spawns))
            {
                writer.Write("spawns.gon");
            }
            else
            {
                byte[] spawnsBytes = Encoding.UTF8.GetBytes(level.spawns);
                writer.Write(spawnsBytes.Length);
                writer.Write(spawnsBytes);
            }

            // Tiles string
            if (string.IsNullOrEmpty(level.tiles))
            {
                writer.Write("tiles.gon");
            }
            else
            {
                byte[] tilesBytes = Encoding.UTF8.GetBytes(level.tiles);
                writer.Write(tilesBytes.Length);
                writer.Write(tilesBytes);
            }

            UnityEngine.Debug.Log($"Strings written @ {ms.Position}");

            // 8 reserved bytes (skipped on decode)
            writer.Write(new byte[8]);

            UnityEngine.Debug.Log($"Reserved bytes written @ {ms.Position}");

            // Tile layers
            // NOTE: Only layer 0 (groundLayer) is stored. If you have multiple
            // layers, you'll need to store them separately on the Level struct.
            for (int l = 0; l < level.layers; l++)
            {
                bool isGroundLayer = (l == 0);

                for (int y = 0; y < level.height; y++)
                {
                    for (int x = 0; x < level.width; x++)
                    {
                        // Write a plain tile ID for now.
                        // Randomized tiles (0xFFFF) would need their own data structure.
                        int tileId = isGroundLayer ? level.groundLayer[y][x] : 0;
                        writer.Write((ushort)tileId);
                    }
                }

                UnityEngine.Debug.Log($"Layer {l} written @ {ms.Position}");
            }

            // Spawns
            foreach (Spawn spawn in level.entityList)
            {
                writer.Write((ushort)spawn.x);
                writer.Write((ushort)spawn.y);
                writer.Write((ushort)(spawn.uid == -1 ? 0xFFFF : spawn.uid));
                writer.Write((ushort)spawn.wave);

                if (spawn.uid == -1)
                {
                    writer.Write((ushort)spawn.randomCount);

                    foreach (randomSpawn rs in spawn.spawns)
                    {
                        writer.Write((ushort)rs.uid);
                        writer.Write((ushort)rs.weight);
                    }
                }

                UnityEngine.Debug.Log($"Spawn written: ({spawn.x}, {spawn.y}), UID: {spawn.uid}, Wave: {spawn.wave} @ {ms.Position}");
            }
            writer.Write(new byte[3]);

            UnityEngine.Debug.Log($"--- ENCODE COMPLETE @ {ms.Position} ---");

            System.IO.File.WriteAllBytes(filePath, ms.ToArray());
            UnityEngine.Debug.Log($"Level saved to: {filePath}");
        }
        EditorManager.Instance.mouseEnabled = true;
    }
    IEnumerator SetProjectWindow()
    {
        EditorManager.Instance.mouseEnabled = false;

		FileBrowser.AddQuickLink("Users", SaveSystem.LoadSettings().defaultFileLocation, null );
        
        string defaultFileLocation = sysVars.defaultFileLocation;

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, defaultFileLocation, null, "Select Folder", "Open" );

        if( FileBrowser.Success )
        {
            fileDest = $"{FileBrowser.Result[0]}";
            Directory.CreateDirectory($"{fileDest}\\levels");
            sysVars.defaultFileLocation = fileDest;
            SaveSystem.SaveSettings(sysVars);
            UnityEngine.Debug.Log("File destination: " + fileDest);
            EditorManager.Instance.UpdateProjectLabel();
        }
        else
        {
            EditorManager.Instance.mouseEnabled = true;
        }
    }

    public void RunGame()
    {
        Launch();
    }

    public void Launch()
    {
        string app = sysVars.MewgenicsDirectory;
        if (app == "" || app == null)
        {
            StartCoroutine(SetGameDir());
            UnityEngine.Debug.Log("File Location Not Found");
        }
        else
        {
            AttemptLaunch(sysVars.MewgenicsDirectory, sysVars.defaultFileLocation);
        }
    }

    IEnumerator SetGameDir()
    {
        EditorManager.Instance.mouseEnabled = false;
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Executable", ".exe"));

        FileBrowser.SetDefaultFilter( ".exe" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null , null, "Select Game Executable", "Select" );

        if( FileBrowser.Success )
        {
            string fileDest = FileBrowser.Result[0];
            UnityEngine.Debug.Log("File destination: " + fileDest);
            sysVars.MewgenicsDirectory = fileDest;
            SaveSystem.SaveSettings(sysVars);
            AttemptLaunch(sysVars.MewgenicsDirectory, sysVars.defaultFileLocation);
        }
        else
        {
            EditorManager.Instance.mouseEnabled = true;
        }
    }

    public void AttemptLaunch(string app, string project)
    {
        string arguments = $"-dev_mode true -enable_debugconsole true -modpaths \"{project}\"";
        // 2. Configure the ProcessStartInfo
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = app;
        // Arguments containing spaces should be wrapped in quotes if required by the target app
        startInfo.Arguments = arguments;
        
        // Optional: Other useful properties
        startInfo.UseShellExecute = true; // Use the OS shell to start (default is true)
        // startInfo.WorkingDirectory = @"C:\path\to\working\directory"; // Set the working directory

        // 3. Start the process
        try
        {
            Process myProcess = Process.Start(startInfo);
            
            // Optional: Wait for the process to exit
            // myProcess.WaitForExit(); 
            // Console.WriteLine("External process exited with code: " + myProcess.ExitCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    
    public void EnableInspector(Vector3Int position)
    {
        if (!spawnLocFree(position.x, position.y))
        {
            Spawn currentSpawn = GetSpawnAtLoc(position.x, position.y);
            Inspector.GetComponent<InspectorScript>().UpdateInfo(currentSpawn);
            Inspector.SetActive(true);
        }
    }
}

