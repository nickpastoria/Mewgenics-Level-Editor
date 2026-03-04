using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    private int[] groundLayer = new int[100];
    private List<Spawn> entityList;
    private List<GameObject> goList;
    public Grid grid;
    public GameObject grass;
    public GameObject groundLayerParent;
    
    private struct Spawn {
        int uid;
        Vector2Int position;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            entityList = new List<Spawn>();
            goList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateLevel()
    {
        clearGameObjects();
        for (int i  = 0; i < groundLayer.Length; i++)
        {
            if (groundLayer[i] == 2)
            {
                Vector3Int gridPos = ArrToVec(i);
                GameObject newObject = Instantiate(grass, grid.GetCellCenterWorld(gridPos), transform.rotation);
                Debug.Log("Placing tile"); 
                goList.Add(newObject);
            }
        }
    }
    
    // Sets a tile in our internal list to its corresponding UID
    public void setTile(int UID, Vector3Int position)
    {
        int el = VecToArr(position);
        groundLayer[el] = UID;
        Debug.Log("Set tile " + position.x + ", " + position.y + " to UID: " + UID);
        updateLevel();
    }

    // Converts a Vector2 grid position into the corresopnding flat 0-99 list position
    private int VecToArr(Vector3Int position)
    {
        // lel much easier than going the other way ++
        // perhaps I didn't need a helper function for this, but fuck you it's my code
        return position.y * 10 + position.x;
    }

    private Vector3Int ArrToVec(int index)
    {
        int x = index%10;
        int y = index/10;
        return new Vector3Int(x, y, 0);
    }

    public void clearGround()
    {
        // I opted to have this as a class member rather than a static since I don't
        // want external things to need to call a specific ground layer
        // there is only one after all
        for (int i = 0; i < groundLayer.Length; i++)
        {
            groundLayer[i] = 0;
        }
    }

    private void clearGameObjects()
    {
        foreach(GameObject gm in goList)
        {
            Destroy(gm);
        }
        goList.Clear();
    }
}
