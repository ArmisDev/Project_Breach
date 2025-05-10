using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(8, 6);
    public int maxWeight = 50; // For survival encumbrance system
    
    [Header("Auto-Stack Settings")]
    public bool autoStack = true;
    [SerializeField] private StackSearchRadius stackSearchRadius = StackSearchRadius.Grid;
    
    [Header("Performance Monitoring")]
    [SerializeField] private bool enablePerformanceTracking = false;
    private Dictionary<string, float> operationTimes = new Dictionary<string, float>();
    
    // Core components
    private InventoryGrid grid;
    private List<InventoryItem> items = new List<InventoryItem>();
    
    // Quick access slots for consumables
    [Header("Quick Access")]
    public int quickSlotCount = 4;
    private Dictionary<int, InventoryItem> quickSlots = new Dictionary<int, InventoryItem>();
    
    [Header("Performance")]
    [SerializeField] private bool useFastGridSearch = true;
    [SerializeField] private bool cacheAvailablePositions = true;
    private Dictionary<Vector2Int, List<Vector2Int>> cachedPositions = new Dictionary<Vector2Int, List<Vector2Int>>();
    
    // Properties
    public Vector2Int GridSize => gridSize;
    public float CurrentWeight 
    { 
        get 
        {
            if (_currentWeightDirty)
            {
                _cachedWeight = CalculateCurrentWeight();
                _currentWeightDirty = false;
            }
            return _cachedWeight;
        }
    }
    public bool IsOverencumbered => CurrentWeight > maxWeight;
    
    // Cached weight values
    private float _cachedWeight = 0f;
    private bool _currentWeightDirty = true;
    public List<InventoryItem> Items => new List<InventoryItem>(items); // Return copy to prevent external modification
    public int MaxWeight => maxWeight; // Public property for save system
    public bool AutoStack => autoStack; // Public property for save system
    public int QuickSlotCount => quickSlotCount; // Public property for save system
    
    [Header("Debug")]
    [SerializeField] private bool enableStateValidation = true;
    [SerializeField] private float validationInterval = 1f; // Validate every second
    private float lastValidationTime = 0f;
    
    [Header("Quick Slot Sync")]
    [SerializeField] private bool enableQuickSlotSync = true;
    [SerializeField] private float quickSlotSyncInterval = 0.5f; // Sync more frequently
    private float lastQuickSlotSyncTime = 0f;
    
    private void Awake()
    {
        InitializeInventory();
        InventoryEvents.TriggerInventoryInitialized();
    }
    
    private void Update()
    {
        // Periodic state validation
        if (enableStateValidation && Time.time - lastValidationTime >= validationInterval)
        {
            ValidateInventoryState();
            lastValidationTime = Time.time;
        }
        
        // Periodic quick slot synchronization
        if (enableQuickSlotSync && Time.time - lastQuickSlotSyncTime >= quickSlotSyncInterval)
        {
            ValidateAllQuickSlots();
            lastQuickSlotSyncTime = Time.time;
        }
    }
    
    private void InitializeInventory()
    {
        grid = new InventoryGrid(gridSize);
        items.Clear();
        quickSlots.Clear();
        
        // Initialize quick slots
        for (int i = 0; i < quickSlotCount; i++)
        {
            quickSlots[i] = null;
        }
    }
    
    #region Performance Tracking
    
    private void TrackPerformance(string operation, System.Action action)
    {
        if (!enablePerformanceTracking)
        {
            action();
            return;
        }
        
        float startTime = Time.realtimeSinceStartup;
        action();
        float endTime = Time.realtimeSinceStartup;
        
        float duration = (endTime - startTime) * 1000f; // Convert to milliseconds
        
        if (operationTimes.ContainsKey(operation))
        {
            operationTimes[operation] = (operationTimes[operation] + duration) / 2f; // Moving average
        }
        else
        {
            operationTimes[operation] = duration;
        }
    }
    
    [ContextMenu("Log Performance Stats")]
    public void LogPerformanceStats()
    {
        if (!enablePerformanceTracking)
        {
            Debug.Log("Performance tracking is disabled. Enable it to see stats.");
            return;
        }
        
        Debug.Log("=== Inventory Performance Stats ===");
        foreach (var kvp in operationTimes)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value:F2}ms average");
        }
        
        Debug.Log($"Total Items: {items.Count}");
        Debug.Log($"Grid Fill Percentage: {grid.GetFillPercentage():F1}%");
    }
    
    #endregion
    
    #region Adding Items
    
    public bool AddItem(ItemData itemData, int count = 1, string acquiredFrom = "Unknown")
    {
        if (itemData == null)
        {
            Debug.LogError("AddItem called with null ItemData");
            return false;
        }
        
        if (count <= 0)
        {
            Debug.LogWarning($"AddItem called with count <= 0: {count}");
            return false;
        }
        
        if (grid == null)
        {
            Debug.LogError("Cannot add item: grid is not initialized");
            return false;
        }
        
        bool result = false;
        TrackPerformance("AddItem", () =>
        {
            // If stackable, try to add to existing stacks first
            if (itemData.isStackable && autoStack)
            {
                if (TryAddToExistingStacks(itemData, count))
                {
                    result = true;
                    return;
                }
            }
            
            // Try to place new item instances
            int remaining = count;
            while (remaining > 0)
            {
                int stackSize = itemData.isStackable ? 
                    Mathf.Min(remaining, itemData.maxStackSize) : 1;
                
                Vector2Int position = FindAvailablePosition(itemData);
                if (position == Vector2Int.one * -1) // No space found
                {
                    Debug.Log($"Not enough space for {itemData.itemName}. Placed {count - remaining}/{count}");
                    result = remaining < count; // Return true if we placed at least some
                    return;
                }
                
                InventoryItem newItem = new InventoryItem(itemData, position)
                {
                    stackCount = stackSize,
                    acquiredFrom = acquiredFrom
                };
                
                PlaceItemInGrid(newItem);
                remaining -= stackSize;
            }
            
            result = true;
        });
        
        return result;
    }
    
    private bool TryAddToExistingStacks(ItemData itemData, int count)
    {
        if (itemData == null || count <= 0) return false;
        
        var stackableItems = items.Where(item => 
            item.itemData.itemID == itemData.itemID && 
            item.stackCount < item.itemData.maxStackSize).ToList();
        
        // Sort by remaining space to fill efficiently
        stackableItems.Sort((a, b) => 
            (b.itemData.maxStackSize - b.stackCount).CompareTo(a.itemData.maxStackSize - a.stackCount));
        
        int remaining = count;
        foreach (var item in stackableItems)
        {
            if (remaining <= 0) break;
            
            int spaceInStack = item.itemData.maxStackSize - item.stackCount;
            int toAdd = Mathf.Min(remaining, spaceInStack);
            
            item.stackCount += toAdd;
            remaining -= toAdd;
            
            // Mark weight as dirty
            _currentWeightDirty = true;
            
            InventoryEvents.TriggerItemStackChanged(item, item.stackCount);
            
            // Early exit if we've placed all items
            if (remaining <= 0) break;
        }
        
        return remaining <= 0;
    }
    
    #endregion
    
    #region Removing Items
    
    public bool RemoveItem(string itemID, int count = 1)
    {
        var itemsToRemove = items.Where(item => item.itemData.itemID == itemID).ToList();
        int remaining = count;
        
        foreach (var item in itemsToRemove)
        {
            if (remaining <= 0) break;
            
            if (item.stackCount <= remaining)
            {
                remaining -= item.stackCount;
                RemoveItemFromGrid(item);
            }
            else
            {
                item.stackCount -= remaining;
                remaining = 0;
                InventoryEvents.TriggerItemStackChanged(item, item.stackCount);
            }
        }
        
        return remaining <= 0;
    }
    
    public bool RemoveItemAt(Vector2Int position)
    {
        if (grid == null)
        {
            Debug.LogError("Cannot remove item: grid is not initialized");
            return false;
        }
        
        InventoryItem item = grid.GetItemAt(position);
        if (item == null)
        {
            Debug.LogWarning($"No item found at position {position}");
            return false;
        }
        
        RemoveItemFromGrid(item);
        return true;
    }
    
    #endregion
    
    #region Moving Items
    
    public bool MoveItem(Vector2Int from, Vector2Int to, bool rotate = false)
    {
        InventoryItem item = grid.GetItemAt(from);
        if (item == null) return false;
        
        // Check if we're trying to stack
        InventoryItem targetItem = grid.GetItemAt(to);
        if (targetItem != null && item.CanStackWith(targetItem))
        {
            return TryStackItems(item, targetItem);
        }
        
        // Check if item can fit at new position
        if (rotate) item.isRotated = !item.isRotated;
        
        if (!grid.CanPlaceItem(item, to))
        {
            if (rotate) item.isRotated = !item.isRotated; // Revert rotation
            return false;
        }
        
        // Move item
        grid.RemoveItem(from);
        item.gridPosition = to;
        grid.PlaceItem(item, to);
        
        InventoryEvents.TriggerItemMoved(item, to);
        
        // Validate state after critical operation
        if (enableStateValidation)
        {
            ValidateInventoryState();
        }
        
        return true;
    }
    
    private bool TryStackItems(InventoryItem source, InventoryItem target)
    {
        if (!source.CanStackWith(target)) return false;
        
        int available = target.itemData.maxStackSize - target.stackCount;
        if (available <= 0) return false;
        
        int toTransfer = Mathf.Min(source.stackCount, available);
        target.stackCount += toTransfer;
        source.stackCount -= toTransfer;
        
        InventoryEvents.TriggerItemStackChanged(target, target.stackCount);
        
        if (source.stackCount <= 0)
        {
            // Source item is being removed, sync its quick slots
            SyncQuickSlotsForItem(source);
            RemoveItemFromGrid(source);
        }
        else
        {
            InventoryEvents.TriggerItemStackChanged(source, source.stackCount);
            // Also sync source item's quick slots since its stack changed
            SyncQuickSlotsForItem(source);
        }
        
        // Sync target item's quick slots since its stack changed
        SyncQuickSlotsForItem(target);
        
        return true;
    }
    
    #endregion
    
    #region Grid Utilities
    
    private Vector2Int FindAvailablePosition(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("FindAvailablePosition called with null ItemData");
            return Vector2Int.one * -1;
        }
        
        if (useFastGridSearch)
        {
            // Optimized search: spiral outward from center
            return FindAvailablePositionSpiral(itemData);
        }
        else
        {
            // Original brute force search
            return FindAvailablePositionBruteForce(itemData);
        }
    }
    
    private Vector2Int FindAvailablePositionSpiral(ItemData itemData)
    {
        Vector2Int itemSize = itemData.size;
        Vector2Int center = new Vector2Int(gridSize.x / 2, gridSize.y / 2);
        
        // Start from center and spiral outward
        int maxDistance = Mathf.Max(gridSize.x, gridSize.y);
        
        for (int distance = 0; distance < maxDistance; distance++)
        {
            // Check positions in a square spiral pattern
            for (int x = -distance; x <= distance; x++)
            {
                for (int y = -distance; y <= distance; y++)
                {
                    // Only check the perimeter of the current square
                    if (Mathf.Abs(x) != distance && Mathf.Abs(y) != distance) continue;
                    
                    Vector2Int pos = center + new Vector2Int(x, y);
                    
                    // Check both orientations
                    if (grid.CanPlaceItem(itemData, pos, false))
                    {
                        return pos;
                    }
                    
                    if (itemSize.x != itemSize.y && grid.CanPlaceItem(itemData, pos, true))
                    {
                        return pos;
                    }
                }
            }
        }
        
        return Vector2Int.one * -1; // No position found
    }
    
    private Vector2Int FindAvailablePositionBruteForce(ItemData itemData)
    {
        // Try different positions and rotations
        for (int rotation = 0; rotation < 2; rotation++)
        {
            Vector2Int size = rotation == 0 ? itemData.size : new Vector2Int(itemData.size.y, itemData.size.x);
            
            for (int y = 0; y <= gridSize.y - size.y; y++)
            {
                for (int x = 0; x <= gridSize.x - size.x; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (grid.CanPlaceItem(itemData, pos, rotation == 1))
                    {
                        return pos;
                    }
                }
            }
        }
        
        return Vector2Int.one * -1; // No position found
    }
    
    private void PlaceItemInGrid(InventoryItem item)
    {
        grid.PlaceItem(item, item.gridPosition);
        items.Add(item);
        
        // Mark weight as dirty
        _currentWeightDirty = true;
        
        InventoryEvents.TriggerItemAdded(item);
        InventoryEvents.TriggerItemPlaced(item, item.gridPosition);
        InventoryEvents.TriggerWeightChanged(Mathf.RoundToInt(CurrentWeight), maxWeight);
    }
    
    private void RemoveItemFromGrid(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogError("RemoveItemFromGrid called with null item");
            return;
        }
        
        grid.RemoveItem(item.gridPosition);
        items.Remove(item);
        
        // Mark weight as dirty
        _currentWeightDirty = true;
        
        // Sync quick slots when removing an item
        SyncQuickSlotsForItem(item);
        
        InventoryEvents.TriggerItemRemoved(item);
        InventoryEvents.TriggerWeightChanged(Mathf.RoundToInt(CurrentWeight), maxWeight);
    }
    
    #endregion
    
    #region Quick Slots
    
    public bool AssignToQuickSlot(int slotIndex, InventoryItem item)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount)
        {
            Debug.LogWarning($"Invalid quick slot index: {slotIndex}");
            return false;
        }
        
        if (item == null)
        {
            // Allow clearing a slot by assigning null
            quickSlots[slotIndex] = null;
            InventoryEvents.TriggerQuickSlotCleared(slotIndex);
            return true;
        }
        
        if (!items.Contains(item))
        {
            Debug.LogWarning($"Cannot assign item to quick slot: {item.itemData.itemName} not in main inventory");
            return false;
        }
        
        // Only allow consumables in quick slots
        if (item.itemData.type != ItemType.Consumable)
        {
            Debug.LogWarning($"Cannot assign {item.itemData.itemName} to quick slot: not a consumable");
            return false;
        }
        
        // Check if item is already in another quick slot
        for (int i = 0; i < quickSlotCount; i++)
        {
            if (i != slotIndex && quickSlots.ContainsKey(i) && quickSlots[i] == item)
            {
                // Remove from previous slot
                quickSlots[i] = null;
                InventoryEvents.TriggerQuickSlotCleared(i);
                Debug.Log($"Moved {item.itemData.itemName} from quick slot {i} to {slotIndex}");
            }
        }
        
        quickSlots[slotIndex] = item;
        InventoryEvents.TriggerQuickSlotAssigned(slotIndex, item);
        return true;
    }
    
    public InventoryItem GetQuickSlotItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount) return null;
        
        if (quickSlots.ContainsKey(slotIndex))
        {
            var item = quickSlots[slotIndex];
            
            // Validate the item is still in main inventory
            if (item != null && !items.Contains(item))
            {
                Debug.LogWarning($"Quick slot {slotIndex} contained removed item {item.itemData.itemName}. Clearing...");
                quickSlots[slotIndex] = null;
                InventoryEvents.TriggerQuickSlotCleared(slotIndex);
                return null;
            }
            
            return item;
        }
        
        return null;
    }
    
    public bool UseQuickSlot(int slotIndex)
    {
        InventoryItem item = GetQuickSlotItem(slotIndex);
        if (item == null) return false;
        
        // Double-check item is still consumable
        if (item.itemData.type != ItemType.Consumable)
        {
            Debug.LogWarning($"Quick slot {slotIndex} contained non-consumable {item.itemData.itemName}. Clearing...");
            quickSlots[slotIndex] = null;
            InventoryEvents.TriggerQuickSlotCleared(slotIndex);
            return false;
        }
        
        // Use the item (this would interface with a consumable system)
        UseConsumable(item);
        
        // Remove one from stack
        item.stackCount--;
        if (item.stackCount <= 0)
        {
            quickSlots[slotIndex] = null;
            InventoryEvents.TriggerQuickSlotCleared(slotIndex);
            RemoveItemFromGrid(item);
        }
        else
        {
            InventoryEvents.TriggerItemStackChanged(item, item.stackCount);
        }
        
        InventoryEvents.TriggerQuickSlotUsed(slotIndex);
        return true;
    }
    
    // New method to sync a specific item in quick slots
    private void SyncQuickSlotsForItem(InventoryItem item)
    {
        if (item == null) return;
        
        for (int i = 0; i < quickSlotCount; i++)
        {
            if (quickSlots.ContainsKey(i) && quickSlots[i] == item)
            {
                // Item is in this quick slot, validate it
                if (!items.Contains(item) || item.itemData.type != ItemType.Consumable)
                {
                    quickSlots[i] = null;
                    InventoryEvents.TriggerQuickSlotCleared(i);
                    Debug.Log($"Cleared quick slot {i} - item no longer valid");
                }
                else if (item.stackCount <= 0)
                {
                    quickSlots[i] = null;
                    InventoryEvents.TriggerQuickSlotCleared(i);
                    Debug.Log($"Cleared quick slot {i} - item stack depleted");
                }
            }
        }
    }
    
    // New method to clear all invalid quick slots
    public void ValidateAllQuickSlots()
    {
        List<int> slotsToUpdate = new List<int>();
        
        // First pass: find all slots that need updating
        for (int i = 0; i < quickSlotCount; i++)
        {
            if (quickSlots.ContainsKey(i) && quickSlots[i] != null)
            {
                var item = quickSlots[i];
                
                // Check if item is still valid
                if (!items.Contains(item) || 
                    item.itemData.type != ItemType.Consumable || 
                    item.stackCount <= 0)
                {
                    slotsToUpdate.Add(i);
                }
            }
        }
        
        // Second pass: clear invalid slots
        foreach (int slotIndex in slotsToUpdate)
        {
            quickSlots[slotIndex] = null;
            InventoryEvents.TriggerQuickSlotCleared(slotIndex);
        }
        
        if (slotsToUpdate.Count > 0)
        {
            Debug.Log($"Validated quick slots: cleared {slotsToUpdate.Count} invalid assignments");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private float CalculateCurrentWeight()
    {
        return items.Sum(item => item.itemData.weight * item.stackCount);
    }
    
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return items.Where(item => item.itemData.type == type).ToList();
    }
    
    public InventoryItem GetItemByUniqueID(string uniqueID)
    {
        return items.FirstOrDefault(item => item.uniqueID == uniqueID);
    }
    
    public bool HasItem(string itemID, int minimumCount = 1)
    {
        return items.Where(item => item.itemData.itemID == itemID)
                   .Sum(item => item.stackCount) >= minimumCount;
    }
    
    // Method to check what item is at a specific position (for UI access)
    public InventoryItem GetItemAt(Vector2Int position)
    {
        return grid.GetItemAt(position);
    }
    
    // For survival mechanics
    public void RepairAllItems(float repairAmount)
    {
        foreach (var item in items)
        {
            if (item.IsDamaged())
            {
                float oldCondition = item.currentCondition;
                item.Repair(repairAmount);
                InventoryEvents.TriggerItemConditionChanged(item, item.currentCondition);
            }
        }
    }
    
    public void RechargeAllItems(float powerAmount)
    {
        foreach (var item in items.Where(i => i.itemData.requiresPower))
        {
            float oldPower = item.currentPower;
            item.Recharge(powerAmount);
            InventoryEvents.TriggerItemPowerChanged(item, item.currentPower);
        }
    }
    
    // Placeholder for consumable usage - would interface with game systems
    private void UseConsumable(InventoryItem item)
    {
        if (!item.itemData.isConsumable) return;
        
        // This would interface with player stats, health system, etc.
        Debug.Log($"Using {item.itemData.itemName}: Heal={item.itemData.healAmount}, Stamina={item.itemData.staminaAmount}");
        InventoryEvents.TriggerConsumableUsed(item);
    }
    
    #endregion
    
    #region Save/Load Support
    
    public InventorySaveData GetSaveData()
    {
        var saveData = new InventorySaveData
        {
            gridSize = gridSize,
            maxWeight = maxWeight,
            autoStack = autoStack,
            quickSlotCount = quickSlotCount,
            items = new List<InventoryItemSaveData>()
        };
        
        foreach (var item in items)
        {
            saveData.items.Add(InventoryItemSaveData.CreateFromItem(item));
        }
        
        // Save quick slot assignments
        for (int i = 0; i < quickSlotCount; i++)
        {
            var quickItem = GetQuickSlotItem(i);
            if (quickItem != null)
            {
                saveData.quickSlots[i] = quickItem.uniqueID;
            }
        }
        
        // Trigger save event
        bool success = true; // Would check if save was successful
        InventoryEvents.TriggerInventorySaved(success);
        
        return saveData;
    }
    
    public void LoadFromSaveData(InventorySaveData saveData, System.Func<string, ItemData> getItemData)
    {
        InitializeInventory();
        
        // Load settings from save data
        gridSize = saveData.gridSize;
        maxWeight = saveData.maxWeight;
        autoStack = saveData.autoStack;
        quickSlotCount = saveData.quickSlotCount;
        
        grid = new InventoryGrid(gridSize);
        
        foreach (var itemSave in saveData.items)
        {
            // Load ItemData by ID from a database/registry
            ItemData itemData = getItemData(itemSave.itemID);
            if (itemData != null)
            {
                // Create item from save data
                InventoryItem item = itemSave.ToInventoryItem(itemData);
                if (item != null)
                {
                    PlaceItemInGrid(item);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find ItemData for item ID: {itemSave.itemID}");
            }
        }
        
        // Restore quick slot assignments
        foreach (var kvp in saveData.quickSlots)
        {
            var item = GetItemByUniqueID(kvp.Value);
            if (item != null)
            {
                AssignToQuickSlot(kvp.Key, item);
            }
        }
        
        // Trigger load event
        bool success = true; // Would check if load was successful
        InventoryEvents.TriggerInventoryLoaded(success);
    }
    
    #endregion
    
    #region State Validation
    
    private void ValidateInventoryState()
    {
        // Validate grid state
        if (!grid.ValidateGridState(out string gridError))
        {
            Debug.LogWarning($"Grid validation failed: {gridError}");
            
            // Attempt to fix grid state
            if (grid.TryFixGridState())
            {
                Debug.Log("Grid state fixed automatically");
            }
            else
            {
                Debug.LogError("Failed to fix grid state");
            }
        }
        
        // Validate that all items in our list exist in the grid
        foreach (var item in items)
        {
            InventoryItem gridItem = grid.GetItemAt(item.gridPosition);
            if (gridItem != item)
            {
                Debug.LogError($"Item list desync: {item.itemData.itemName} claims to be at {item.gridPosition} but grid shows {(gridItem?.itemData.itemName ?? "empty")}");
            }
        }
        
        // Validate quick slots
        ValidateQuickSlots();
    }
    
    private void ValidateQuickSlots()
    {
        for (int i = 0; i < quickSlotCount; i++)
        {
            if (quickSlots.ContainsKey(i) && quickSlots[i] != null)
            {
                var quickItem = quickSlots[i];
                
                // Check if quick slot item is still in main inventory
                if (!items.Contains(quickItem))
                {
                    Debug.LogWarning($"Quick slot {i} contains {quickItem.itemData.itemName} which is not in main inventory. Clearing...");
                    quickSlots[i] = null;
                    InventoryEvents.TriggerQuickSlotCleared(i);
                }
                
                // Check if quick slot item is still a consumable
                if (quickItem.itemData.type != ItemType.Consumable)
                {
                    Debug.LogWarning($"Quick slot {i} contains {quickItem.itemData.itemName} which is no longer a consumable. Clearing...");
                    quickSlots[i] = null;
                    InventoryEvents.TriggerQuickSlotCleared(i);
                }
            }
        }
    }
    
    [ContextMenu("Validate Inventory State")]
    public void ManualValidateState()
    {
        Debug.Log("=== Manual Inventory Validation ===");
        ValidateInventoryState();
        Debug.Log(grid.GetGridDebugString());
    }
    
    [ContextMenu("Force Fix Grid State")]
    public void ManualFixGridState()
    {
        Debug.Log("=== Attempting to fix grid state ===");
        if (grid.TryFixGridState())
        {
            Debug.Log("Grid state fixed successfully");
            Debug.Log(grid.GetGridDebugString());
        }
        else
        {
            Debug.LogError("Failed to fix grid state");
        }
    }
    
    [ContextMenu("Validate Quick Slots")]
    public void ManualValidateQuickSlots()
    {
        Debug.Log("=== Manual Quick Slot Validation ===");
        ValidateAllQuickSlots();
        
        // Log current quick slot state
        for (int i = 0; i < quickSlotCount; i++)
        {
            var item = GetQuickSlotItem(i);
            if (item != null)
            {
                Debug.Log($"Quick Slot {i}: {item.itemData.itemName} (Stack: {item.stackCount})");
            }
            else
            {
                Debug.Log($"Quick Slot {i}: Empty");
            }
        }
    }
    
    #endregion
}

[System.Serializable]
public enum StackSearchRadius
{
    Adjacent,  // Only check adjacent slots
    Quadrant,  // Check quadrant of grid
    Grid       // Check entire grid
}