using UnityEngine;

public class InventoryInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private FPS.Player.PlayerController playerController;
    
    [Header("Initial Items")]
    [SerializeField] private InitialItem[] startingItems;
    
    [System.Serializable]
    public class InitialItem
    {
        public string itemID;
        public int count = 1;
    }
    
    private void Start()
    {
        InitializeInventorySystem();
        AddStartingItems();
        SetupEventListeners();
    }
    
    private void InitializeInventorySystem()
    {
        // Ensure all components are properly connected
        if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        if (playerController == null) playerController = FindObjectOfType<FPS.Player.PlayerController>();
        
        // Connect references if not already set
        if (playerController.inventoryManager == null)
            playerController.inventoryManager = inventoryManager;
        if (playerController.inventoryUI == null)
            playerController.inventoryUI = inventoryUI;
        
        // Start with inventory closed
        inventoryUI.gameObject.SetActive(false);
        
        Debug.Log("Inventory system initialized");
    }
    
    private void AddStartingItems()
    {
        foreach (var startItem in startingItems)
        {
            var itemData = itemDatabase.GetItemByID(startItem.itemID);
            if (itemData != null)
            {
                inventoryManager.AddItem(itemData, startItem.count, "Starting Items");
                Debug.Log($"Added starting item: {itemData.itemName} x{startItem.count}");
            }
            else
            {
                Debug.LogWarning($"Starting item not found: {startItem.itemID}");
            }
        }
    }
    
    private void SetupEventListeners()
    {
        // Subscribe to inventory events for debugging or other purposes
        InventoryEvents.OnInventoryChanged += OnInventoryChanged;
        InventoryEvents.OnItemAdded += OnItemAdded;
        InventoryEvents.OnQuickSlotUsed += OnQuickSlotUsed;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        InventoryEvents.OnInventoryChanged -= OnInventoryChanged;
        InventoryEvents.OnItemAdded -= OnItemAdded;
        InventoryEvents.OnQuickSlotUsed -= OnQuickSlotUsed;
    }
    
    private void OnInventoryChanged()
    {
        // You can add additional logic here when inventory changes
    }
    
    private void OnItemAdded(InventoryItem item)
    {
        Debug.Log($"Item added: {item.itemData.itemName}");
    }
    
    private void OnQuickSlotUsed(int slotIndex)
    {
        Debug.Log($"Quick slot {slotIndex + 1} used");
    }
    
    [ContextMenu("Test Add Items")]
    private void TestAddItems()
    {
        // Test method to add items during development
        AddStartingItems();
    }
}