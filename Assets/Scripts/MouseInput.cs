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
    InputAction modifyAction;
    private bool isClickDragging = false;
    private LevelManager.Spawn draggedSpawn;
    private SpriteRenderer spriteRenderer;
    private Sprite newSprite;

    // Written by Claude
    // Minimum pixel distance the mouse must move before a click becomes a drag.
    // Increase this value if accidental drags are still triggering too easily.
    private const float DRAG_THRESHOLD_PX = 8f;
    private Vector2 clickOrigin;        // screen position where the left button was pressed
    private bool dragPending = false;   // true while we have a held click that hasn't crossed the threshold yet
    void Start()
    {
        modifyAction = InputSystem.actions.FindAction("Modify");
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
                    // Written by Claude
                    // Record where the click started; actual drag activation is deferred
                    // until the mouse has moved past DRAG_THRESHOLD_PX (see isPressed block below)
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && !level.spawnLocFree(cellPosition.x, cellPosition.y))
                    {
                        dragPending = true;
                        clickOrigin = mouseScreenPosition;
                        draggedSpawn = level.GetSpawnAtLoc(cellPosition.x, cellPosition.y);
                    }
                }
                if(Mouse.current.leftButton.isPressed)
                {
                    // Written by Claude
                    // Activate the drag only once the mouse has moved far enough from the click origin.
                    // This prevents a stationary click from accidentally entering drag mode.
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && dragPending && !isClickDragging)
                    {
                        if(Vector2.Distance(mouseScreenPosition, clickOrigin) >= DRAG_THRESHOLD_PX)
                        {
                            isClickDragging = true;
                            newSprite = spriteLibrary.findSpawnByID(draggedSpawn.uid);
                            spriteRenderer = mouseDragImage.gameObject.GetComponent<SpriteRenderer>();
                            spriteRenderer.sortingOrder = 10;
                            spriteRenderer.sprite = newSprite;
                            mouseDragImage.SetActive(true);
                        }
                    }

                    // Move the drag image with the cursor while dragging
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && isClickDragging)
                    {
                        mouseDragImage.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 10);
                    }
                }
                if(Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    // Clear pending drag state on any release, whether or not a drag actually started
                    dragPending = false;

                    // Set Spawns and tiles
                    if(EditorManager.Instance.type == ItemBrowser.Type.Spawn) level.setSpawn(EditorManager.Instance.CurrentUID, cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.Tile) level.setTile(EditorManager.Instance.CurrentUID, cellPosition);

                    // Enable Inspector
                    if(EditorManager.Instance.type == ItemBrowser.Type.None) level.EnableInspector(cellPosition);
                    if(EditorManager.Instance.type == ItemBrowser.Type.None && isClickDragging)
                    {
                        if (level.spawnLocFree(cellPosition.x, cellPosition.y))
                        {
                            // If we are holding ctrl then we need to copy the item instead of moving it
                            if (modifyAction.IsPressed())
                            {
                                Debug.Log("Copying drag");
                                level.setSpawn(new LevelManager.Spawn(draggedSpawn), cellPosition);
                            }
                            else
                            {
                                draggedSpawn.x = cellPosition.x;
                                draggedSpawn.y = cellPosition.y; 
                            }
                        }
                        level.updateLevel();
                        isClickDragging = false;
                        dragPending = false;
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
