using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class MouseInput : MonoBehaviour
{
    public Grid grid; 
    public GameObject gridCursor;
    private Vector2 mouseScreenPosition;
    public LevelManager level;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {   
        if (Mouse.current != null)
        {
            mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            Vector3Int cellPosition = grid.WorldToCell(mouseWorldPos);
            if (cellPosition.x >= 0 && cellPosition.x < 10 && cellPosition.y >= 0 && cellPosition.y < 10)
            {
                gridCursor.transform.position = (grid.GetCellCenterWorld(cellPosition));
                if(Mouse.current.leftButton.isPressed)
                {
                    
                }
                if(Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    if(EditorManager.Instance.type == ItemBrowser.Type.Spawn) level.setSpawn(EditorManager.Instance.CurrentUID, cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.Tile) level.setTile(EditorManager.Instance.CurrentUID, cellPosition);
                    
                }
                if(Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    level.setTile(0, cellPosition);
                }
            }
        }
    }
}
