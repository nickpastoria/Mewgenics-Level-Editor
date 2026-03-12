using UnityEngine;
using System.Text;
using System.IO;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using SimpleFileBrowser;

public class LevelManager : MonoBehaviour
{
    public Grid grid;
    public GameObject LevelEntity;
    private string fileDest;
    private Level level;
    private List<GameObject> UnityObjects = new List<GameObject>();

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
        level = new Level();
        level.groundLayer = new List<List<int>>();
        level.entityList = new List<Spawn>();
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
                Debug.Log($"Created tile at ({x}, {y}) with ID {tileId}");
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
            Debug.Log($"Created entity at ({spawn.x}, {spawn.y}) with ID {spawn.uid}");
            UnityObjects.Add(s);
        }
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
        Spawn newEntity = new Spawn();
        newEntity.x = position.x;
        newEntity.y = position.y;
        newEntity.uid = ID;
        level.entityList.Add(newEntity);
        clearLevel();
        updateTiles();
        updateSpawns();
    }
    

    public void updateLevel()
    {
        clearLevel();
        updateTiles();
        updateSpawns();
        
    }

    private void LoadLevel()
    {
        StartCoroutine(LevelWindow());
    }

    private void DecodeLevel(byte[] file)
    {
        level = new Level();

        using (MemoryStream ms = new MemoryStream(file))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            Debug.Log($"--- START DECODE: File Size {file.Length} bytes ---");

            level.version = reader.ReadInt32();
            level.width = reader.ReadInt32();
            level.height = reader.ReadInt32();
            level.layers = reader.ReadInt32();
            level.nSpawns = reader.ReadInt32();
            level.camx = reader.ReadInt32();
            level.camy = reader.ReadInt32();
            level.camw = reader.ReadInt32();
            level.camh = reader.ReadInt32();

            Debug.Log($"Header Parsed @ {reader.BaseStream.Position}: v{level.version}, Size:{level.width}x{level.height}, Layers:{level.layers}, Spawns:{level.nSpawns}");

            // Read Spawns string
            int lenStr = reader.ReadInt32();
            Debug.Log($"Spawns String Length: {lenStr} Parsed @ {reader.BaseStream.Position}");
            if (lenStr == 0) {
                level.spawns = "spawns.gon";
            } else {
                level.spawns = Encoding.UTF8.GetString(reader.ReadBytes(lenStr));
            }
            Debug.Log($"Spawns String '{level.spawns}' Parsed @ {reader.BaseStream.Position}");

            // Read Tiles string
            lenStr = reader.ReadInt32();
            if (lenStr == 0) {
                level.tiles = "tiles.gon";
            } else {
                level.tiles = Encoding.UTF8.GetString(reader.ReadBytes(lenStr));
            }
            Debug.Log($"Tiles String '{level.tiles}' Parsed @ {reader.BaseStream.Position}");

            // Skip 8 bytes
            reader.BaseStream.Position += 8;
            Debug.Log($"Skipped 8 bytes. Now @ {reader.BaseStream.Position}");

            level.groundLayer = new List<List<int>>();

            // Decode tile layers (We must read all layers to keep stream aligned)
            for (int l = 0; l < level.layers; l++)
            {
                Debug.Log($"Reading Layer {l} starting @ {reader.BaseStream.Position}...");
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

            Debug.Log($"Finished Layers. Starting Spawns @ {reader.BaseStream.Position}...");

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

                Debug.Log($"Parsed Spawn {i}: ({spawn.x}, {spawn.y}), UID: {spawn.uid}, Wave: {spawn.wave} @ {reader.BaseStream.Position}");

                if (spawn.uid < 12) 
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

            Debug.Log($"--- DECODE COMPLETE @ {reader.BaseStream.Position} ---");
        }

        // currentLevel = level;
        Debug.Log("Level loaded successfully!");
        updateLevel();
    }

    IEnumerator LevelWindow()
    {
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
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );

        yield return FileBrowser.WaitForLoadDialog( FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load" );

        if( FileBrowser.Success )
            fileDest = FileBrowser.Result[0];
            byte[] file = System.IO.File.ReadAllBytes(fileDest);
            DecodeLevel(file);
            Debug.Log("File destination: " + fileDest);
    }
}
