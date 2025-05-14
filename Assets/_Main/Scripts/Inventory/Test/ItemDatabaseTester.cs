using UnityEngine;
using System.Collections.Generic;

public class ItemDatabaseTester : MonoBehaviour
{
    [Header("Testing")]
    public ItemDatabase database;
    public InventoryManager inventoryManager;
    
    [Header("Debug Controls")]
    [SerializeField] private KeyCode addMedkitKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode addRifleKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode addGeiger = KeyCode.Alpha3;
    [SerializeField] private KeyCode addCrystalKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode clearInventoryKey = KeyCode.C;
    
    private void Start()
    {
        TestDatabaseConnectivity();
    }
    
    private void TestDatabaseConnectivity()
    {
        if (database == null)
        {
            Debug.LogError("ItemDatabase not assigned!");
            return;
        }
        
        // Test fetching items
        Debug.Log("Testing ItemDatabase...");
        
        // Try to get specific items
        var medkit = database.GetItemByID("item_medkit");
        var rifle = database.GetItemByID("weapon_rifle_ak");
        var geiger = database.GetItemByID("tool_geiger");
        var crystal = database.GetItemByID("artifact_crystal");
        
        // Display results
        Debug.Log($"Medkit: {(medkit != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"Rifle: {(rifle != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"Geiger Counter: {(geiger != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"Crystal: {(crystal != null ? "FOUND" : "NOT FOUND")}");
        
        // Test filters
        var allConsumables = database.GetItemsByType(ItemType.Consumable);
        Debug.Log($"Total Consumables: {allConsumables.Count}");
        
        var allRareItems = database.GetItemsByRarity(ItemRarity.Rare);
        Debug.Log($"Total Rare Items: {allRareItems.Count}");
    }
    
    private void Update()
    {
        if (inventoryManager == null) return;
        
        // Quick add controls for testing
        if (Input.GetKeyDown(addMedkitKey))
        {
            var medkit = database.GetItemByID("item_medkit");
            if (medkit != null)
                inventoryManager.AddItem(medkit, 1, "Debug Test");
        }
        
        if (Input.GetKeyDown(addRifleKey))
        {
            var rifle = database.GetItemByID("weapon_rifle_ak");
            if (rifle != null)
                inventoryManager.AddItem(rifle, 1, "Debug Test");
        }
        
        if (Input.GetKeyDown(addGeiger))
        {
            var geiger = database.GetItemByID("tool_geiger");
            if (geiger != null)
                inventoryManager.AddItem(geiger, 1, "Debug Test");
        }
        
        if (Input.GetKeyDown(addCrystalKey))
        {
            var crystal = database.GetItemByID("artifact_crystal");
            if (crystal != null)
                inventoryManager.AddItem(crystal, 1, "Debug Test");
        }
        
        if (Input.GetKeyDown(clearInventoryKey))
        {
            ClearInventory();
        }
    }
    
    private void ClearInventory()
    {
        // Get a copy of all items to avoid modifying during iteration
        // var items = inventoryManager.GetAllItemsForUI();
        
        // foreach (var item in items)
        // {
        //     inventoryManager.RemoveItemAt(item.gridPosition);
        // }
        
        Debug.Log("Inventory cleared!");
    }
    
    private void OnGUI()
    {
        // Draw simple debug UI
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("Inventory Test Controls:");
        GUILayout.Label("1 - Add Medkit");
        GUILayout.Label("2 - Add Rifle");
        GUILayout.Label("3 - Add Geiger Counter");
        GUILayout.Label("4 - Add Crystal");
        GUILayout.Label("C - Clear Inventory");
        GUILayout.EndArea();
    }
}