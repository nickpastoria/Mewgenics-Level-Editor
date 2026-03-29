using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MouseInput : MonoBehaviour
{
    public SpriteLibrary spriteLibrary;
    public Grid grid; 
    public GameObject gridCursor;
    private Vector2 mouseScreenPosition;
    public LevelManager level;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EventSystem eventSys;
    public GameObject mouseDragImage;
    private bool isClickDragging = false;
    private LevelManager.Spawn draggedSpawn;
    private SpriteRenderer spriteRenderer;
    private Sprite newSprite;
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
            if (cellPosition.x >= 0 && cellPosition.x < 10 && cellPosition.y >= 0 && cellPosition.y < 10 && EditorManager.Instance.mouseEnabled)
            {
                gridCursor.transform.position = (grid.GetCellCenterWorld(cellPosition));
                if(Mouse.current.leftButton.wasPressedThisFrame)
                {
                    //Start of click and drag
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && !level.spawnLocFree(cellPosition.x, cellPosition.y))
                    {
                        isClickDragging = true;
                        draggedSpawn = level.GetSpawnAtLoc(cellPosition.x, cellPosition.y);
                        newSprite = spriteLibrary.findSpawnByID(draggedSpawn.uid);
                        spriteRenderer = new SpriteRenderer();
                        spriteRenderer = mouseDragImage.gameObject.GetComponent<SpriteRenderer>();
                        spriteRenderer.sortingOrder = 10;
                        spriteRenderer.sprite = newSprite;
                        mouseDragImage.SetActive(true);
                    }
                }
                if(Mouse.current.leftButton.isPressed)
                {
                    // Click and Drag Logic
                    // Check to make sure we're not placing anything
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && isClickDragging)
                    {
                        mouseDragImage.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 10);
                    }
                }
                if(Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    // Set Spawns and tiles
                    if(EditorManager.Instance.type == ItemBrowser.Type.Spawn) level.setSpawn(EditorManager.Instance.CurrentUID, cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.Tile) level.setTile(EditorManager.Instance.CurrentUID, cellPosition);

                    // Enable Inspector
                    if(EditorManager.Instance.type == ItemBrowser.Type.None) level.EnableInspector(cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && isClickDragging)
                    {
                        draggedSpawn.x = cellPosition.x;
                        draggedSpawn.y = cellPosition.y;
                        level.updateLevel();
                        isClickDragging = false;
                        mouseDragImage.SetActive(false);
                    }
                    
                }
                if(Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    if(EditorManager.Instance.type == ItemBrowser.Type.Tile) level.setTile(0, cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.Spawn || EditorManager.Instance.type == ItemBrowser.Type.None ) level.DeleteSpawnAtLocation(cellPosition.x, cellPosition.y);
                }
            }
            else if (!EventSystem.current.IsPointerOverGameObject() && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                // *** Your non-UI click logic goes here ***
                Debug.Log("Mouse clicked on empty space (not UI).");
                level.DisableInspector();
            }
        }
    }
}
