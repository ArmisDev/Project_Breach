using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    
    // Dictionary for fast lookups
    private Dictionary<string, ItemData> itemLookup;
    
    // Initialize the lookup dictionary
    private void OnEnable()
    {
        BuildLookup();
    }
    
    [ContextMenu("Rebuild Lookup")]
    public void BuildLookup()
    {
        itemLookup = new Dictionary<string, ItemData>();
        
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID))
            {
                if (itemLookup.ContainsKey(item.itemID))
                {
                    Debug.LogWarning($"Duplicate item ID found: {item.itemID}");
                }
                else
                {
                    itemLookup[item.itemID] = item;
                }
            }
        }
        
        Debug.Log($"ItemDatabase: Loaded {itemLookup.Count} items");
    }
    
    // Get item by ID
    public ItemData GetItemByID(string itemID)
    {
        if (itemLookup == null) BuildLookup();
        
        if (itemLookup.TryGetValue(itemID, out ItemData item))
        {
            return item;
        }
        
        Debug.LogWarning($"Item not found: {itemID}");
        return null;
    }
    
    // Get all items of a specific type
    public List<ItemData> GetItemsByType(ItemType type)
    {
        return items.Where(item => item.type == type).ToList();
    }
    
    // Get all items of a specific rarity
    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        return items.Where(item => item.rarity == rarity).ToList();
    }
    
    // Add item to database
    [ContextMenu("Add New Item")]
    public void AddItem(ItemData newItem)
    {
        if (newItem == null || string.IsNullOrEmpty(newItem.itemID))
        {
            Debug.LogError("Cannot add invalid item to database");
            return;
        }
        
        if (items.Contains(newItem))
        {
            Debug.LogWarning($"Item {newItem.itemID} already exists in database");
            return;
        }
        
        items.Add(newItem);
        BuildLookup();
    }
    
    // Remove item from database
    public void RemoveItem(string itemID)
    {
        var item = GetItemByID(itemID);
        if (item != null)
        {
            items.Remove(item);
            BuildLookup();
        }
    }
    
    // Validation methods
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        var duplicateIDs = new Dictionary<string, int>();
        var invalidItems = new List<ItemData>();
        
        foreach (var item in items)
        {
            if (item == null)
            {
                invalidItems.Add(item);
                continue;
            }
            
            if (string.IsNullOrEmpty(item.itemID))
            {
                Debug.LogError($"Item {item.name} has no ID assigned!");
                continue;
            }
            
            if (duplicateIDs.ContainsKey(item.itemID))
            {
                duplicateIDs[item.itemID]++;
            }
            else
            {
                duplicateIDs[item.itemID] = 1;
            }
            
            // Additional validation
            if (item.size.x <= 0 || item.size.y <= 0)
            {
                Debug.LogError($"Item {item.itemID} has invalid size: {item.size}");
            }
            
            if (item.isStackable && item.maxStackSize <= 1)
            {
                Debug.LogWarning($"Item {item.itemID} is stackable but maxStackSize is {item.maxStackSize}");
            }
        }
        
        // Report duplicates
        foreach (var kvp in duplicateIDs)
        {
            if (kvp.Value > 1)
            {
                Debug.LogError($"Duplicate item ID: {kvp.Key} appears {kvp.Value} times!");
            }
        }
        
        // Remove invalid items
        foreach (var invalid in invalidItems)
        {
            items.Remove(invalid);
        }
        
        Debug.Log($"Validation complete. Found {duplicateIDs.Count} unique items.");
    }
    
    // Singleton pattern for easy access
    public static ItemDatabase Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildLookup();
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple ItemDatabase instances found! Using the first one.");
        }
    }
}