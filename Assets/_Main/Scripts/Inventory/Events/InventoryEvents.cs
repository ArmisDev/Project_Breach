using UnityEngine;
using System;

/// <summary>
/// Centralized event system for the inventory. All inventory-related events are defined here.
/// This makes it easier to manage, subscribe to, and discover all inventory events.
/// </summary>
public static class InventoryEvents
{
    #region Inventory Management Events
    
    /// <summary>
    /// Fired when an item is added to the inventory
    /// </summary>
    public static event Action<InventoryItem> OnItemAdded;
    
    /// <summary>
    /// Fired when an item is removed from the inventory
    /// </summary>
    public static event Action<InventoryItem> OnItemRemoved;
    
    /// <summary>
    /// Fired when an item is moved within the inventory
    /// </summary>
    public static event Action<InventoryItem, Vector2Int> OnItemMoved;
    
    /// <summary>
    /// Fired when an item is placed in the inventory (initial placement)
    /// </summary>
    public static event Action<InventoryItem, Vector2Int> OnItemPlaced;
    
    /// <summary>
    /// Fired when the total inventory weight changes
    /// </summary>
    public static event Action<int> OnWeightChanged;
    
    /// <summary>
    /// Generic event fired whenever the inventory state changes
    /// </summary>
    public static event Action OnInventoryChanged;
    
    #endregion
    
    #region UI Events
    
    /// <summary>
    /// Fired when an item is right-clicked in the UI
    /// </summary>
    public static event Action<InventoryItem> OnItemRightClick;
    
    /// <summary>
    /// Fired when an item is double-clicked in the UI
    /// </summary>
    public static event Action<InventoryItem> OnItemDoubleClick;
    
    /// <summary>
    /// Fired when an item drag operation starts
    /// </summary>
    public static event Action<InventoryItem> OnItemDragStart;
    
    /// <summary>
    /// Fired when an item drag operation ends
    /// </summary>
    public static event Action<InventoryItem, bool> OnItemDragEnd; // bool indicates success
    
    /// <summary>
    /// Fired when a tooltip is shown for an item
    /// </summary>
    public static event Action<InventoryItem> OnTooltipShow;
    
    /// <summary>
    /// Fired when a tooltip is hidden
    /// </summary>
    public static event Action OnTooltipHide;
    
    #endregion
    
    #region Quick Slot Events
    
    /// <summary>
    /// Fired when an item is assigned to a quick slot
    /// </summary>
    public static event Action<int, InventoryItem> OnQuickSlotAssigned;
    
    /// <summary>
    /// Fired when a quick slot is used/activated
    /// </summary>
    public static event Action<int> OnQuickSlotUsed;
    
    /// <summary>
    /// Fired when a quick slot is unassigned/cleared
    /// </summary>
    public static event Action<int> OnQuickSlotCleared;
    
    #endregion
    
    #region Item State Events
    
    /// <summary>
    /// Fired when an item's condition changes (damage/repair)
    /// </summary>
    public static event Action<InventoryItem, float> OnItemConditionChanged;
    
    /// <summary>
    /// Fired when an item's power/charge changes
    /// </summary>
    public static event Action<InventoryItem, float> OnItemPowerChanged;
    
    /// <summary>
    /// Fired when an item is modified (attachments, upgrades)
    /// </summary>
    public static event Action<InventoryItem, ItemModification> OnItemModified;
    
    /// <summary>
    /// Fired when an item stack size changes
    /// </summary>
    public static event Action<InventoryItem, int> OnItemStackChanged;
    
    #endregion
    
    #region Gameplay Events
    
    /// <summary>
    /// Fired when a consumable item is used
    /// </summary>
    public static event Action<InventoryItem> OnConsumableUsed;
    
    /// <summary>
    /// Fired when equipment is equipped
    /// </summary>
    public static event Action<InventoryItem, EquipmentSlot> OnItemEquipped;
    
    /// <summary>
    /// Fired when equipment is unequipped
    /// </summary>
    public static event Action<InventoryItem, EquipmentSlot> OnItemUnequipped;
    
    /// <summary>
    /// Fired when the player becomes overencumbered
    /// </summary>
    public static event Action OnOverencumbered;
    
    /// <summary>
    /// Fired when the player is no longer overencumbered
    /// </summary>
    public static event Action OnNoLongerOverencumbered;
    
    #endregion
    
    #region System Events
    
    /// <summary>
    /// Fired when the inventory is initialized
    /// </summary>
    public static event Action OnInventoryInitialized;
    
    /// <summary>
    /// Fired when the inventory is saved
    /// </summary>
    public static event Action<bool> OnInventorySaved; // bool indicates success
    
    /// <summary>
    /// Fired when the inventory is loaded
    /// </summary>
    public static event Action<bool> OnInventoryLoaded; // bool indicates success
    
    /// <summary>
    /// Fired when the inventory grid is resized
    /// </summary>
    public static event Action<Vector2Int> OnInventoryResized;
    
    #endregion
    
    #region Private State
    
    // Track overencumbered state to detect changes
    private static bool wasOverencumbered = false;
    
    #endregion
    
    #region Helper Methods for Triggering Events
    
    // These methods ensure null checks and provide a safe way to trigger events
    // They also make it easier to add debugging or logging in the future
    
    public static void TriggerItemAdded(InventoryItem item)
    {
        OnItemAdded?.Invoke(item);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerItemRemoved(InventoryItem item)
    {
        OnItemRemoved?.Invoke(item);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerItemMoved(InventoryItem item, Vector2Int newPosition)
    {
        OnItemMoved?.Invoke(item, newPosition);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerItemPlaced(InventoryItem item, Vector2Int position)
    {
        OnItemPlaced?.Invoke(item, position);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerWeightChanged(int newWeight, float maxWeight)
    {
        OnWeightChanged?.Invoke(newWeight);
        
        // Check for overencumbered state
        bool isOverencumbered = newWeight > maxWeight;
        
        if (isOverencumbered && !wasOverencumbered)
        {
            OnOverencumbered?.Invoke();
        }
        else if (!isOverencumbered && wasOverencumbered)
        {
            OnNoLongerOverencumbered?.Invoke();
        }
        
        wasOverencumbered = isOverencumbered;
    }
    
    public static void TriggerItemConditionChanged(InventoryItem item, float newCondition)
    {
        OnItemConditionChanged?.Invoke(item, newCondition);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerItemPowerChanged(InventoryItem item, float newPower)
    {
        OnItemPowerChanged?.Invoke(item, newPower);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerQuickSlotAssigned(int slotIndex, InventoryItem item)
    {
        OnQuickSlotAssigned?.Invoke(slotIndex, item);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerQuickSlotUsed(int slotIndex)
    {
        OnQuickSlotUsed?.Invoke(slotIndex);
    }
    
    public static void TriggerConsumableUsed(InventoryItem item)
    {
        OnConsumableUsed?.Invoke(item);
    }
    
    public static void TriggerQuickSlotCleared(int slotIndex)
    {
        OnQuickSlotCleared?.Invoke(slotIndex);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerItemStackChanged(InventoryItem item, int newStackCount)
    {
        OnItemStackChanged?.Invoke(item, newStackCount);
        OnInventoryChanged?.Invoke();
    }
    
    public static void TriggerInventoryInitialized()
    {
        OnInventoryInitialized?.Invoke();
    }
    
    public static void TriggerInventorySaved(bool success)
    {
        OnInventorySaved?.Invoke(success);
    }
    
    public static void TriggerInventoryLoaded(bool success)
    {
        OnInventoryLoaded?.Invoke(success);
    }
    
    // UI Event Triggers
    public static void TriggerItemRightClick(InventoryItem item)
    {
        OnItemRightClick?.Invoke(item);
    }
    
    public static void TriggerItemDoubleClick(InventoryItem item)
    {
        OnItemDoubleClick?.Invoke(item);
    }
    
    public static void TriggerItemDragStart(InventoryItem item)
    {
        OnItemDragStart?.Invoke(item);
    }
    
    public static void TriggerItemDragEnd(InventoryItem item, bool success)
    {
        OnItemDragEnd?.Invoke(item, success);
    }
    
    public static void TriggerTooltipShow(InventoryItem item)
    {
        OnTooltipShow?.Invoke(item);
    }
    
    public static void TriggerTooltipHide()
    {
        OnTooltipHide?.Invoke();
    }
    
    // Debug helper to list all active event subscribers
    public static void LogEventSubscribers()
    {
        Debug.Log("=== INVENTORY EVENT SUBSCRIBERS ===");
        LogEventSubscriberCount("OnItemAdded", OnItemAdded);
        LogEventSubscriberCount("OnItemRemoved", OnItemRemoved);
        LogEventSubscriberCount("OnItemMoved", OnItemMoved);
        LogEventSubscriberCount("OnWeightChanged", OnWeightChanged);
        // Add more as needed for debugging
    }
    
    private static void LogEventSubscriberCount(string eventName, Delegate eventDelegate)
    {
        int count = eventDelegate?.GetInvocationList().Length ?? 0;
        Debug.Log($"{eventName}: {count} subscribers");
    }
    
    #endregion
}