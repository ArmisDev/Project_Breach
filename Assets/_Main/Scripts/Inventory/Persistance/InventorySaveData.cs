using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class InventorySaveData
{
    [Header("Grid Configuration")]
    public Vector2Int gridSize;
    public int maxWeight;
    public bool autoStack;
    
    [Header("Items")]
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
    
    [Header("Quick Slots")]
    public Dictionary<int, string> quickSlots = new Dictionary<int, string>(); // slot index -> item uniqueID
    public int quickSlotCount;
    
    [Header("Metadata")]
    public float lastSaveTime;
    public string saveVersion = "1.0";
    public string playerID; // For multiplayer/cloud saves
    
    // Constructor
    public InventorySaveData()
    {
        lastSaveTime = Time.realtimeSinceStartup;
    }
    
    // Create save data from current inventory
    public static InventorySaveData CreateFromInventory(InventoryManager inventory)
    {
        var saveData = new InventorySaveData
        {
            gridSize = inventory.GridSize,
            maxWeight = inventory.maxWeight,
            autoStack = inventory.autoStack,
            quickSlotCount = inventory.quickSlotCount,
            lastSaveTime = Time.realtimeSinceStartup
        };
        
        // Save all items
        foreach (var item in inventory.Items)
        {
            saveData.items.Add(InventoryItemSaveData.CreateFromItem(item));
        }
        
        // Save quick slot assignments
        for (int i = 0; i < inventory.quickSlotCount; i++)
        {
            var quickItem = inventory.GetQuickSlotItem(i);
            if (quickItem != null)
            {
                saveData.quickSlots[i] = quickItem.uniqueID;
            }
        }
        
        return saveData;
    }
}

[System.Serializable]
public class InventoryItemSaveData
{
    [Header("Basic Info")]
    public string itemID;
    public int stackCount;
    public string uniqueID;
    
    [Header("Grid Placement")]
    public Vector2Int position;
    public bool isRotated;
    
    [Header("Instance Properties")]
    public float currentCondition;
    public float currentPower;
    
    [Header("History")]
    public float timeAcquired;
    public string acquiredFrom;
    
    [Header("Modifications")]
    public List<ItemModificationSaveData> modifications = new List<ItemModificationSaveData>();
    
    [Header("Custom Data")]
    public string customData; // For storing additional data in JSON format
    
    // Create save data from item
    public static InventoryItemSaveData CreateFromItem(InventoryItem item)
    {
        var saveData = new InventoryItemSaveData
        {
            itemID = item.itemData.itemID,
            stackCount = item.stackCount,
            uniqueID = item.uniqueID,
            position = item.gridPosition,
            isRotated = item.isRotated,
            currentCondition = item.currentCondition,
            currentPower = item.currentPower,
            timeAcquired = item.timeAcquired,
            acquiredFrom = item.acquiredFrom
        };
        
        // Save modifications
        foreach (var mod in item.modifications)
        {
            saveData.modifications.Add(ItemModificationSaveData.CreateFromModification(mod));
        }
        
        return saveData;
    }
    
    // Create an item from save data
    public InventoryItem ToInventoryItem(ItemData itemData)
    {
        if (itemData == null) return null;
        
        var item = new InventoryItem(itemData, position)
        {
            stackCount = this.stackCount,
            uniqueID = this.uniqueID,
            isRotated = this.isRotated,
            currentCondition = this.currentCondition,
            currentPower = this.currentPower,
            timeAcquired = this.timeAcquired,
            acquiredFrom = this.acquiredFrom
        };
        
        // Restore modifications
        item.modifications = new List<ItemModification>();
        foreach (var modSave in modifications)
        {
            item.modifications.Add(modSave.ToItemModification());
        }
        
        return item;
    }
}

[System.Serializable]
public class ItemModificationSaveData
{
    public string modificationID;
    public string name;
    public string description;
    public ModificationType type;
    public float value;
    
    [Header("Visual Data")]
    public string modIconPath; // Path to sprite asset
    public Color tintColor;
    
    // Create save data from modification
    public static ItemModificationSaveData CreateFromModification(ItemModification mod)
    {
        return new ItemModificationSaveData
        {
            modificationID = mod.modificationID,
            name = mod.name,
            description = mod.description,
            type = mod.type,
            value = mod.value,
            modIconPath = GetAssetPath(mod.modIcon),
            tintColor = mod.tintColor
        };
    }
    
    // Create modification from save data
    public ItemModification ToItemModification()
    {
        return new ItemModification
        {
            modificationID = this.modificationID,
            name = this.name,
            description = this.description,
            type = this.type,
            value = this.value,
            modIcon = LoadSprite(modIconPath),
            tintColor = this.tintColor
        };
    }
    
    // Helper methods for sprite paths
    private static string GetAssetPath(Sprite sprite)
    {
        if (sprite == null) return "";
        #if UNITY_EDITOR
        return UnityEditor.AssetDatabase.GetAssetPath(sprite);
        #else
        return sprite.name; // Fallback for builds
        #endif
    }
    
    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Resources.Load<Sprite>(path);
    }
}

// Utility class for save/load operations
public static class InventorySaveSystem
{
    private const string SAVE_PATH = "inventory_save.json";
    
    // Save inventory to JSON file
    public static bool SaveInventory(InventoryManager inventory, string fileName = null)
    {
        try
        {
            string path = fileName ?? GetSavePath(SAVE_PATH);
            var saveData = InventorySaveData.CreateFromInventory(inventory);
            string json = JsonUtility.ToJson(saveData, true);
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Inventory saved to {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save inventory: {e.Message}");
            return false;
        }
    }
    
    // Load inventory from JSON file
    public static InventorySaveData LoadInventory(string fileName = null)
    {
        try
        {
            string path = fileName ?? GetSavePath(SAVE_PATH);
            
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"Save file not found at {path}");
                return null;
            }
            
            string json = System.IO.File.ReadAllText(path);
            var saveData = JsonUtility.FromJson<InventorySaveData>(json);
            
            Debug.Log($"Inventory loaded from {path}");
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
            return null;
        }
    }
    
    // Get save file path
    private static string GetSavePath(string fileName)
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "Saves", fileName);
    }
    
    // Create backup of current save
    public static bool BackupSave(string backupName = null)
    {
        try
        {
            string currentPath = GetSavePath(SAVE_PATH);
            if (!System.IO.File.Exists(currentPath)) return false;
            
            string backupFileName = backupName ?? $"inventory_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string backupPath = GetSavePath(backupFileName);
            
            System.IO.File.Copy(currentPath, backupPath, true);
            Debug.Log($"Backup created at {backupPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}");
            return false;
        }
    }
    
    // Auto-save functionality
    public static class AutoSave
    {
        private static float lastSaveTime;
        private static float saveInterval = 60f; // Auto-save every minute
        
        public static void Initialize(float intervalSeconds = 60f)
        {
            saveInterval = intervalSeconds;
            lastSaveTime = Time.realtimeSinceStartup;
        }
        
        public static void Update(InventoryManager inventory)
        {
            if (Time.realtimeSinceStartup - lastSaveTime >= saveInterval)
            {
                SaveInventory(inventory);
                lastSaveTime = Time.realtimeSinceStartup;
            }
        }
    }
}

// Save data for debugging and analytics
[System.Serializable]
public class InventoryAnalytics
{
    public int totalItemsCollected;
    public int uniqueItemsCollected;
    public float totalLootValue;
    public Dictionary<string, int> itemIDCount = new Dictionary<string, int>();
    public float totalPlayTime;
    public int itemsRepaired;
    public int itemsRecharged;
    
    public static InventoryAnalytics GetAnalytics(InventorySaveData saveData, System.Func<string, ItemData> getItemData = null)
    {
        var analytics = new InventoryAnalytics
        {
            totalItemsCollected = saveData.items.Count,
            uniqueItemsCollected = new HashSet<string>(saveData.items.Select(i => i.itemID)).Count,
            totalPlayTime = saveData.lastSaveTime
        };
        
        // Analyze item distribution by ID
        foreach (var item in saveData.items)
        {
            if (analytics.itemIDCount.ContainsKey(item.itemID))
                analytics.itemIDCount[item.itemID]++;
            else
                analytics.itemIDCount[item.itemID] = 1;
                
            // Count items that have been repaired or recharged
            if (item.currentCondition < 100f)
                analytics.itemsRepaired++;
            if (item.currentPower > 0f)
                analytics.itemsRecharged++;
        }
        
        // If we have access to ItemData, we can do more detailed analysis
        if (getItemData != null)
        {
            analytics.totalLootValue = 0f;
            var itemTypeCount = new Dictionary<ItemType, int>();
            var itemRarityCount = new Dictionary<ItemRarity, int>();
            
            foreach (var item in saveData.items)
            {
                var itemData = getItemData(item.itemID);
                if (itemData != null)
                {
                    // Analyze types and rarities
                    if (itemTypeCount.ContainsKey(itemData.type))
                        itemTypeCount[itemData.type]++;
                    else
                        itemTypeCount[itemData.type] = 1;
                        
                    if (itemRarityCount.ContainsKey(itemData.rarity))
                        itemRarityCount[itemData.rarity]++;
                    else
                        itemRarityCount[itemData.rarity] = 1;
                }
            }
        }
        
        return analytics;
    }
}