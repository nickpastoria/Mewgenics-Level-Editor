using UnityEngine;

public class LevelEditor : MonoBehaviour
{
    public int GridSize;
    public GameObject GridTile;
    private int[] Grid;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Grid = new int[GridSize];
        fill(Grid, 0);
        Debug.Log(Grid);
        drawGrid(Grid);
    }
    
    void fill(int[] array, int number)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = 0;
        }
    }

    Vector2 IsoToWorld(Vector2 gridPosition)
    {
        // Takes in a grid x and y and returns the corresponding world position
        Vector2 xVector = new Vector2 (0.695f, 0.35f);
        Vector2 yVector = new Vector2 (-0.695f, 0.35f);
        return gridPosition.x*xVector+gridPosition.y*yVector;

    }

    void drawGrid(int[] grid)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            int y = i/10;
            int x = i%10;
            Vector2 worldPos = IsoToWorld(new Vector2(x,y));
            Instantiate(GridTile, new Vector3(worldPos.x, worldPos.y, 0f), transform.rotation);
        }
    }

}
