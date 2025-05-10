using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using DG.Tweening; // DOTween namespace

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private InventorySlot slotPrefab;
    
    [Header("Item Display")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemUIPrefab;
    
    [Header("Grid Settings")]
    [SerializeField] private float slotSize = 50f;
    [SerializeField] private float slotSpacing = 5f;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private Image weightBar;
    [SerializeField] private Color overencumberedColor = Color.red;
    [SerializeField] private Color normalWeightColor = Color.white;
    
    [Header("Drag & Drop")]
    [SerializeField] private GameObject dragPreview;
    [SerializeField] private Image dragPreviewImage;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;
    [SerializeField] private float dragAlpha = 0.7f;
    
    [Header("Tooltip")]
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform tooltipRect;
    
    [Header("Quick Slots")]
    [SerializeField] private Transform quickSlotsContainer;
    [SerializeField] private InventorySlot[] quickSlots;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    [SerializeField] private float highlightAlpha = 0.3f;
    
    [Header("Performance")]
    [SerializeField] private int itemUIPoolSize = 20; // Initial pool size
    private Queue<GameObject> itemUIPool = new Queue<GameObject>();
    
    // Internal references
    private InventorySlot[,] slotGrid;
    private Dictionary<InventoryItem, GameObject> itemUIObjects = new Dictionary<InventoryItem, GameObject>();
    private InventoryItem draggingItem;
    private Vector2Int draggingItemOffset;
    private bool isRotating;
    
    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeUI()
    {
        CreateSlotGrid();
        InitializeQuickSlots();
        InitializeItemUIPool();
        RefreshInventoryDisplay();
        UpdateWeightDisplay();
    }
    
    #region Object Pooling
    
    private void InitializeItemUIPool()
    {
        if (itemUIPrefab == null)
        {
            Debug.LogError("itemUIPrefab is not assigned - cannot initialize object pool");
            return;
        }
        
        // Pre-instantiate pool objects
        for (int i = 0; i < itemUIPoolSize; i++)
        {
            GameObject pooledItem = Instantiate(itemUIPrefab, itemsContainer);
            pooledItem.SetActive(false);
            itemUIPool.Enqueue(pooledItem);
        }
        
        Debug.Log($"Initialized ItemUI pool with {itemUIPoolSize} objects");
    }
    
    private GameObject GetPooledItemUI()
    {
        if (itemUIPool.Count > 0)
        {
            GameObject pooledItem = itemUIPool.Dequeue();
            pooledItem.SetActive(true);
            return pooledItem;
        }
        else
        {
            // Pool exhausted, create new object (but warn about it)
            Debug.LogWarning("ItemUI pool exhausted, creating new object. Consider increasing pool size.");
            return Instantiate(itemUIPrefab, itemsContainer);
        }
    }
    
    private void ReturnToPool(GameObject itemUI)
    {
        if (itemUI == null) return;
        
        // Reset the object state
        itemUI.transform.localScale = Vector3.one;
        itemUI.transform.rotation = Quaternion.identity;
        
        // Kill any active tweens
        itemUI.transform.DOKill();
        
        Image itemImage = itemUI.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.color = Color.white;
            itemImage.DOKill();
        }
        
        // Return to pool
        itemUI.SetActive(false);
        itemUIPool.Enqueue(itemUI);
    }
    
    #endregion
    
    #region Grid Creation
    
    private void CreateSlotGrid()
    {
        Vector2Int gridSize = inventoryManager.GridSize;
        slotGrid = new InventorySlot[gridSize.x, gridSize.y];
        
        // Configure grid layout
        GridLayoutGroup gridLayout = slotsContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = slotsContainer.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        gridLayout.cellSize = Vector2.one * slotSize;
        gridLayout.spacing = Vector2.one * slotSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridSize.x;
        
        // Create slots
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                InventorySlot slot = Instantiate(slotPrefab, slotsContainer);
                slot.Initialize(new Vector2Int(x, y), this);
                slotGrid[x, y] = slot;
            }
        }
    }
    
    private void InitializeQuickSlots()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (quickSlots[i] != null)
            {
                quickSlots[i].InitializeAsQuickSlot(i, this);
            }
        }
    }
    
    #endregion
    
    #region Event Subscriptions
    
    private void SubscribeToEvents()
    {
        InventoryEvents.OnItemAdded += OnItemAddedToInventory;
        InventoryEvents.OnItemRemoved += OnItemRemovedFromInventory;
        InventoryEvents.OnItemMoved += OnItemMovedInInventory;
        InventoryEvents.OnWeightChanged += OnWeightChanged;
        InventoryEvents.OnInventoryChanged += RefreshInventoryDisplay;
    }
    
    private void UnsubscribeFromEvents()
    {
        InventoryEvents.OnItemAdded -= OnItemAddedToInventory;
        InventoryEvents.OnItemRemoved -= OnItemRemovedFromInventory;
        InventoryEvents.OnItemMoved -= OnItemMovedInInventory;
        InventoryEvents.OnWeightChanged -= OnWeightChanged;
        InventoryEvents.OnInventoryChanged -= RefreshInventoryDisplay;
    }
    
    #endregion
    
    #region Inventory Display
    
    public void RefreshInventoryDisplay()
    {
        // Return existing item UIs to pool
        foreach (var itemUI in itemUIObjects.Values)
        {
            if (itemUI != null)
            {
                ReturnToPool(itemUI);
            }
        }
        itemUIObjects.Clear();
        
        // Create new item UIs from pool
        foreach (var item in inventoryManager.Items)
        {
            CreateItemUI(item);
        }
        
        // Update quick slots
        UpdateQuickSlotsDisplay();
    }
    
    private void CreateItemUI(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogError("CreateItemUI called with null item");
            return;
        }
        
        if (itemsContainer == null)
        {
            Debug.LogError("itemsContainer is not assigned");
            return;
        }
        
        // Get from pool instead of instantiating
        GameObject itemUIGO = GetPooledItemUI();
        if (itemUIGO == null)
        {
            Debug.LogError("Failed to get item UI from pool");
            return;
        }
        
        ItemDragUI itemDragUI = itemUIGO.GetComponent<ItemDragUI>();
        
        if (itemDragUI != null)
        {
            itemDragUI.Setup(item, this);
            itemUIObjects[item] = itemUIGO;
            PositionItemUI(item, itemUIGO);
            
            // Add a spawn animation for sci-fi effect
            itemUIGO.transform.localScale = Vector3.zero;
            itemUIGO.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBounce)
                .SetDelay(Random.Range(0f, 0.2f)); // Stagger animations
            
            // Add a glow effect for special items
            if (item.itemData != null && item.itemData.isDimensionalArtifact)
            {
                Image itemImage = itemUIGO.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.DOFade(0.7f, 1f).SetLoops(-1, LoopType.Yoyo);
                }
            }
        }
        else
        {
            Debug.LogError("ItemDragUI component not found on pooled object");
            ReturnToPool(itemUIGO);
        }
    }
    
    private void PositionItemUI(InventoryItem item, GameObject itemUI)
    {
        if (item == null || itemUI == null)
        {
            Debug.LogError("PositionItemUI called with null parameters");
            return;
        }
        
        if (slotGrid == null)
        {
            Debug.LogError("Cannot position item UI: slot grid is not initialized");
            return;
        }
        
        Vector2Int gridPos = item.gridPosition;
        
        // Validate position is within bounds
        if (gridPos.x < 0 || gridPos.x >= slotGrid.GetLength(0) || 
            gridPos.y < 0 || gridPos.y >= slotGrid.GetLength(1))
        {
            Debug.LogError($"Invalid grid position for item UI: {gridPos}");
            return;
        }
        
        InventorySlot slot = slotGrid[gridPos.x, gridPos.y];
        if (slot == null)
        {
            Debug.LogError($"Slot at position {gridPos} is null");
            return;
        }
        
        RectTransform itemRect = itemUI.GetComponent<RectTransform>();
        if (itemRect == null)
        {
            Debug.LogError("Item UI does not have a RectTransform component");
            return;
        }
        
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        if (slotRect == null)
        {
            Debug.LogError("Slot does not have a RectTransform component");
            return;
        }
        
        itemRect.anchoredPosition = slotRect.anchoredPosition;
        
        // Set size based on item dimensions
        Vector2Int itemSize = item.Size;
        float width = itemSize.x * slotSize + (itemSize.x - 1) * slotSpacing;
        float height = itemSize.y * slotSize + (itemSize.y - 1) * slotSpacing;
        itemRect.sizeDelta = new Vector2(width, height);
    }
    
    #endregion
    
    #region Weight Display
    
    private void UpdateWeightDisplay()
    {
        float currentWeight = inventoryManager.CurrentWeight;
        float maxWeight = inventoryManager.MaxWeight;
        
        if (weightText != null)
        {
            weightText.text = $"{currentWeight:F1} / {maxWeight} kg";
            weightText.color = inventoryManager.IsOverencumbered ? overencumberedColor : normalWeightColor;
        }
        
        if (weightBar != null)
        {
            weightBar.fillAmount = currentWeight / maxWeight;
            weightBar.color = inventoryManager.IsOverencumbered ? overencumberedColor : normalWeightColor;
        }
    }
    
    #endregion
    
    #region Drag & Drop
    
    public void StartDragging(InventoryItem item, Vector2 screenPosition)
    {
        if (item == null)
        {
            Debug.LogError("StartDragging called with null item");
            return;
        }
        
        if (slotGrid == null)
        {
            Debug.LogError("Cannot start dragging: slot grid is not initialized");
            return;
        }
        
        draggingItem = item;
        
        // Trigger drag start event
        InventoryEvents.TriggerItemDragStart(item);
        
        // Calculate offset from item position to mouse
        Vector2Int itemPos = item.gridPosition;
        
        // Validate position is within bounds
        if (itemPos.x < 0 || itemPos.x >= slotGrid.GetLength(0) || 
            itemPos.y < 0 || itemPos.y >= slotGrid.GetLength(1))
        {
            Debug.LogError($"Invalid item position for dragging: {itemPos}");
            draggingItem = null;
            return;
        }
        
        InventorySlot slot = slotGrid[itemPos.x, itemPos.y];
        if (slot == null)
        {
            Debug.LogError($"Slot at position {itemPos} is null");
            draggingItem = null;
            return;
        }
        
        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position);
        Vector2 offset = screenPosition - slotScreenPos;
        
        // Convert offset to grid coordinates
        draggingItemOffset = new Vector2Int(
            Mathf.RoundToInt(offset.x / (slotSize + slotSpacing)),
            Mathf.RoundToInt(offset.y / (slotSize + slotSpacing))
        );
        
        // Setup drag preview
        if (dragPreview != null)
        {
            dragPreview.SetActive(true);
            
            if (dragPreviewImage != null && item.itemData != null)
            {
                dragPreviewImage.sprite = item.itemData.icon;
            }
            
            if (dragPreviewCanvasGroup != null)
            {
                // Fade in the drag preview smoothly
                dragPreviewCanvasGroup.alpha = 0f;
                dragPreviewCanvasGroup.DOFade(dragAlpha, 0.2f).SetEase(Ease.OutQuad);
            }
            
            // Set preview size
            Vector2Int itemSize = item.Size;
            float width = itemSize.x * slotSize + (itemSize.x - 1) * slotSpacing;
            float height = itemSize.y * slotSize + (itemSize.y - 1) * slotSpacing;
            RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(width, height);
            }
            
            // Add a slight scale animation
            dragPreview.transform.localScale = Vector3.one * 0.8f;
            dragPreview.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            Debug.LogWarning("Drag preview is not assigned");
        }
        
        // Show placement preview
        ShowPlacementPreview(true);
    }
    
    public void UpdateDragging(Vector2 screenPosition)
    {
        if (draggingItem == null) return;
        
        // Update drag preview position
        if (dragPreview != null)
        {
            dragPreview.transform.position = screenPosition;
        }
        
        // Update placement preview
        Vector2Int gridPos = ScreenToGridPosition(screenPosition);
        gridPos -= draggingItemOffset;
        UpdatePlacementPreview(gridPos);
    }
    
    public void EndDragging(Vector2 screenPosition)
    {
        if (draggingItem == null) return;
        
        // Hide drag preview
        if (dragPreview != null)
        {
            dragPreview.SetActive(false);
        }
        
        // Hide placement preview
        ShowPlacementPreview(false);
        
        // Attempt to place item
        Vector2Int targetPos = ScreenToGridPosition(screenPosition);
        targetPos -= draggingItemOffset;
        
        bool success = false;
        if (inventoryManager.MoveItem(draggingItem.gridPosition, targetPos, isRotating))
        {
            // Success - item moved
            success = true;
        }
        else
        {
            // Failed - animate item back to original position
            AnimateItemReturn(draggingItem);
        }
        
        // Trigger drag end event
        InventoryEvents.TriggerItemDragEnd(draggingItem, success);
        
        draggingItem = null;
        isRotating = false;
    }
    
    private void ShowPlacementPreview(bool show)
    {
        foreach (var slot in slotGrid)
        {
            slot.ShowHighlight(Color.clear);
        }
    }
    
    private void UpdatePlacementPreview(Vector2Int gridPos)
    {
        // Reset all slot highlights
        ShowPlacementPreview(false);
        
        if (draggingItem == null) return;
        
        Vector2Int itemSize = isRotating ? 
            new Vector2Int(draggingItem.itemData.size.y, draggingItem.itemData.size.x) : 
            draggingItem.itemData.size;
        
        bool canPlace = true;
        
        // Check if position is valid
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                
                if (checkPos.x < 0 || checkPos.x >= inventoryManager.GridSize.x ||
                    checkPos.y < 0 || checkPos.y >= inventoryManager.GridSize.y)
                {
                    canPlace = false;
                    break;
                }
                
                // Check for conflicts (temporarily remove dragging item from consideration)
                InventoryItem itemAtPos = inventoryManager.GetItemAt(checkPos);
                if (itemAtPos != null && itemAtPos != draggingItem)
                {
                    canPlace = false;
                    break;
                }
            }
            
            if (!canPlace) break;
        }
        
        // Apply highlight
        Color highlightColor = canPlace ? validPlacementColor : invalidPlacementColor;
        highlightColor.a = highlightAlpha;
        
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                
                if (checkPos.x >= 0 && checkPos.x < inventoryManager.GridSize.x &&
                    checkPos.y >= 0 && checkPos.y < inventoryManager.GridSize.y)
                {
                    slotGrid[checkPos.x, checkPos.y].ShowHighlight(highlightColor);
                }
            }
        }
    }
    
    #endregion
    
    #region Tooltip
    
    public void ShowTooltip(InventoryItem item, Vector2 screenPosition)
    {
        if (item == null)
        {
            Debug.LogWarning("ShowTooltip called with null item");
            return;
        }
        
        if (tooltip == null)
        {
            Debug.LogWarning("Tooltip GameObject is not assigned");
            return;
        }
        
        if (tooltipText == null)
        {
            Debug.LogWarning("Tooltip text component is not assigned");
            return;
        }
        
        tooltip.SetActive(true);
        tooltipText.text = item.GetTooltipText();
        
        if (tooltipRect != null)
        {
            // Position tooltip
            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);
            
            float tooltipWidth = corners[2].x - corners[0].x;
            float tooltipHeight = corners[2].y - corners[0].y;
            
            Vector2 tooltipPos = screenPosition;
            
            // Keep tooltip on screen
            if (tooltipPos.x + tooltipWidth > Screen.width)
                tooltipPos.x = screenPosition.x - tooltipWidth;
            
            if (tooltipPos.y - tooltipHeight < 0)
                tooltipPos.y = screenPosition.y + tooltipHeight;
            
            tooltip.transform.position = tooltipPos;
        }
        else
        {
            Debug.LogWarning("Tooltip RectTransform is not assigned, using screen position");
            tooltip.transform.position = screenPosition;
        }
        
        // Trigger tooltip show event
        InventoryEvents.TriggerTooltipShow(item);
    }
    
    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
        
        // Trigger tooltip hide event
        InventoryEvents.TriggerTooltipHide();
    }
    
    #endregion
    
    #region Quick Slots
    
    private void UpdateQuickSlotsDisplay()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            InventoryItem quickItem = inventoryManager.GetQuickSlotItem(i);
            if (quickSlots[i] != null)
            {
                quickSlots[i].SetItem(quickItem);
            }
        }
    }
    
    #endregion
    
    #region Utility
    
    private Vector2Int ScreenToGridPosition(Vector2 screenPosition)
    {
        // Convert screen position to grid coordinates
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            slotsContainer.GetComponent<RectTransform>(),
            screenPosition,
            null,
            out Vector2 localPoint
        );
        
        int x = Mathf.FloorToInt((localPoint.x + slotSize / 2) / (slotSize + slotSpacing));
        int y = Mathf.FloorToInt((localPoint.y + slotSize / 2) / (slotSize + slotSpacing));
        
        return new Vector2Int(x, y);
    }
    
    private void AnimateItemReturn(InventoryItem item)
    {
        // Smooth animation to return item to original position using DOTween
        if (itemUIObjects.TryGetValue(item, out GameObject itemUI))
        {
            // Kill any existing tweens on this object
            itemUI.transform.DOKill();
            
            // Get the target position
            Vector2Int gridPos = item.gridPosition;
            InventorySlot slot = slotGrid[gridPos.x, gridPos.y];
            Vector2 targetPosition = slot.GetComponent<RectTransform>().anchoredPosition;
            
            // Animate return with sci-fi feel
            itemUI.transform.DOLocalMove(targetPosition, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    // Ensure proper positioning after animation
                    PositionItemUI(item, itemUI);
                });
            
            // Add a slight scale bounce for sci-fi effect
            itemUI.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 2, 0.5f);
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnItemAddedToInventory(InventoryItem item)
    {
        CreateItemUI(item);
    }
    
    private void OnItemRemovedFromInventory(InventoryItem item)
    {
        if (itemUIObjects.TryGetValue(item, out GameObject itemUI))
        {
            ReturnToPool(itemUI);
            itemUIObjects.Remove(item);
        }
    }
    
    private void OnItemMovedInInventory(InventoryItem item, Vector2Int newPosition)
    {
        if (itemUIObjects.TryGetValue(item, out GameObject itemUI))
        {
            PositionItemUI(item, itemUI);
        }
    }
    
    private void OnWeightChanged(int newWeight)
    {
        UpdateWeightDisplay();
    }
    
    #endregion
    
    #region Input Handling
    
    private void Update()
    {
        // Handle rotation during drag
        if (draggingItem != null && Input.GetKeyDown(KeyCode.R))
        {
            isRotating = !isRotating;
            
            // Update drag preview rotation
            if (dragPreview != null)
            {
                Vector2Int itemSize = isRotating ? 
                    new Vector2Int(draggingItem.itemData.size.y, draggingItem.itemData.size.x) : 
                    draggingItem.itemData.size;
                
                float width = itemSize.x * slotSize + (itemSize.x - 1) * slotSpacing;
                float height = itemSize.y * slotSize + (itemSize.y - 1) * slotSpacing;
                dragPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            }
            
            // Update placement preview
            Vector2Int gridPos = ScreenToGridPosition(Input.mousePosition);
            gridPos -= draggingItemOffset;
            UpdatePlacementPreview(gridPos);
        }
    }
    
    #endregion
}

// Helper component for item UI dragging
public class ItemDragUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    private InventoryItem item;
    private InventoryUI inventoryUI;
    private Image image;
    private CanvasGroup canvasGroup;
    
    public void Setup(InventoryItem inventoryItem, InventoryUI ui)
    {
        if (inventoryItem == null)
        {
            Debug.LogError("ItemDragUI.Setup called with null item");
            return;
        }
        
        if (ui == null)
        {
            Debug.LogError("ItemDragUI.Setup called with null InventoryUI");
            return;
        }
        
        item = inventoryItem;
        inventoryUI = ui;
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (image == null)
        {
            Debug.LogWarning("ItemDragUI: Image component not found");
        }
        
        if (canvasGroup == null)
        {
            Debug.LogWarning("ItemDragUI: CanvasGroup component not found");
        }
        
        if (image != null && item != null && item.itemData != null)
        {
            image.sprite = item.itemData.icon;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null)
        {
            Debug.LogError("Cannot begin drag: missing required components");
            return;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
        
        inventoryUI.StartDragging(item, eventData.position);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            Debug.LogError("Cannot drag: InventoryUI is null");
            return;
        }
        
        inventoryUI.UpdateDragging(eventData.position);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            Debug.LogError("Cannot end drag: InventoryUI is null");
            return;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        inventoryUI.EndDragging(eventData.position);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null)
        {
            Debug.LogError("Cannot handle click: item is null");
            return;
        }
        
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryEvents.TriggerItemRightClick(item);
        }
        else if (eventData.clickCount == 2)
        {
            InventoryEvents.TriggerItemDoubleClick(item);
        }
    }
}