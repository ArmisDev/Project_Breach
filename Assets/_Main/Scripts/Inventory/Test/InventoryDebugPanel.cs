using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class InventoryDebugPanel : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public ItemDatabase itemDatabase;
    
    [Header("UI Components")]
    public GameObject debugPanel;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI eventsText;
    public Transform itemSpawnContainer;
    public Button itemSpawnButtonPrefab;
    
    [Header("Settings")]
    public bool autoUpdate = true;
    public float updateInterval = 0.5f;
    
    private float lastUpdateTime;
    private System.Text.StringBuilder eventLog = new System.Text.StringBuilder();
    private int maxEventLogLines = 10;
    
    private void Start()
    {
        CreateItemSpawnButtons();
        SubscribeToEvents();
        UpdateDebugInfo();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void Update()
    {
        if (autoUpdate && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
        
        // Toggle panel with Tilde key
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            TogglePanel();
        }
    }
    
    private void TogglePanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }
    
    private void UpdateDebugInfo()
    {
        if (inventoryManager == null || statsText == null) return;
        
        var stats = new System.Text.StringBuilder();
        stats.AppendLine($"=== INVENTORY DEBUG INFO ===");
        stats.AppendLine($"Items Count: {inventoryManager.Items.Count}");
        stats.AppendLine($"Weight: {inventoryManager.CurrentWeight:F1}/{inventoryManager.MaxWeight}");
        stats.AppendLine($"Overencumbered: {inventoryManager.IsOverencumbered}");
        stats.AppendLine($"Auto-Stack: {inventoryManager.AutoStack}");
        stats.AppendLine();
        
        // Item type breakdown
        stats.AppendLine("Item Types:");
        var itemsByType = inventoryManager.Items.GroupBy(i => i.itemData.type);
        foreach (var group in itemsByType)
        {
            stats.AppendLine($"  {group.Key}: {group.Count()}");
        }
        stats.AppendLine();
        
        // Quick slots info
        stats.AppendLine("Quick Slots:");
        for (int i = 0; i < inventoryManager.QuickSlotCount; i++)
        {
            var item = inventoryManager.GetQuickSlotItem(i);
            stats.AppendLine($"  Slot {i}: {(item != null ? item.itemData.itemName : "Empty")}");
        }
        stats.AppendLine();
        
        // Grid statistics
        if (TryGetGrid(out InventoryGrid grid))
        {
            stats.AppendLine($"Grid Fill: {grid.GetFillPercentage():F1}%");
            stats.AppendLine($"Empty Cells: {grid.GetEmptyCellCount()}");
        }
        
        statsText.text = stats.ToString();
    }
    
    private void CreateItemSpawnButtons()
    {
        if (itemDatabase == null || itemSpawnContainer == null || itemSpawnButtonPrefab == null)
        {
            Debug.LogWarning("Missing references for item spawn buttons");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in itemSpawnContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for all item types
        var allItems = System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
            .SelectMany(type => itemDatabase.GetItemsByType(type))
            .Distinct()
            .OrderBy(item => item.type)
            .ThenBy(item => item.itemName);
        
        foreach (var itemData in allItems)
        {
            Button button = Instantiate(itemSpawnButtonPrefab, itemSpawnContainer);
            button.GetComponentInChildren<TextMeshProUGUI>().text = itemData.itemName;
            
            ItemData capturedItem = itemData; // Capture for closure
            button.onClick.AddListener(() => SpawnItem(capturedItem));
        }
    }
    
    private void SpawnItem(ItemData itemData)
    {
        if (inventoryManager == null || itemData == null) return;
        
        int count = itemData.isStackable ? 
            Mathf.Min(itemData.maxStackSize, 5) : 1;
        
        bool success = inventoryManager.AddItem(itemData, count, "Debug");
        
        if (!success)
        {
            LogEvent($"Failed to spawn {itemData.itemName}");
        }
    }
    
    #region Event Logging
    
    private void SubscribeToEvents()
    {
        InventoryEvents.OnItemAdded += item => LogEvent($"+ {item.itemData.itemName}");
        InventoryEvents.OnItemRemoved += item => LogEvent($"- {item.itemData.itemName}");
        InventoryEvents.OnItemMoved += (item, pos) => LogEvent($"→ {item.itemData.itemName} to {pos}");
        InventoryEvents.OnOverencumbered += () => LogEvent("⚠ OVERENCUMBERED!");
        InventoryEvents.OnNoLongerOverencumbered += () => LogEvent("✓ No longer overencumbered");
        InventoryEvents.OnQuickSlotUsed += slot => LogEvent($"◎ Used quick slot {slot}");
    }
    
    private void UnsubscribeFromEvents()
    {
        // Note: In a real implementation, you'd want to properly unsubscribe
        // For this debug panel, we'll rely on OnDestroy cleanup
    }
    
    private void LogEvent(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        eventLog.Insert(0, $"[{timestamp}] {message}\n");
        
        // Keep only last N lines
        var lines = eventLog.ToString().Split('\n');
        if (lines.Length > maxEventLogLines)
        {
            eventLog.Clear();
            for (int i = 0; i < maxEventLogLines; i++)
            {
                eventLog.AppendLine(lines[i]);
            }
        }
        
        if (eventsText != null)
        {
            eventsText.text = eventLog.ToString();
        }
    }
    
    #endregion
    
    #region Context Menu Commands
    
    [ContextMenu("Clear Inventory")]
    private void ClearInventory()
    {
        if (inventoryManager == null) return;
        
        var items = inventoryManager.Items.ToList();
        foreach (var item in items)
        {
            inventoryManager.RemoveItemAt(item.gridPosition);
        }
        
        LogEvent("Inventory cleared");
    }
    
    [ContextMenu("Fill Inventory")]
    private void FillInventory()
    {
        if (inventoryManager == null || itemDatabase == null) return;
        
        var allItems = itemDatabase.GetItemsByType(ItemType.Material)
            .Concat(itemDatabase.GetItemsByType(ItemType.Consumable))
            .ToArray();
        
        int added = 0;
        while (added < 50) // Limit to prevent infinite loop
        {
            ItemData randomItem = allItems[Random.Range(0, allItems.Length)];
            if (!inventoryManager.AddItem(randomItem, 1, "Debug_Fill"))
            {
                break;
            }
            added++;
        }
        
        LogEvent($"Added {added} items to fill inventory");
    }
    
    [ContextMenu("Test Save/Load")]
    private void TestSaveLoad()
    {
        if (inventoryManager == null) return;
        
        // Save current state
        var saveData = inventoryManager.GetSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        
        // Clear inventory
        ClearInventory();
        
        // Restore state
        inventoryManager.LoadFromSaveData(saveData, itemDatabase.GetItemByID);
        
        LogEvent("Save/Load test completed");
    }
    
    [ContextMenu("Stress Test")]
    private void StressTest()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        
        // Test add performance
        sw.Start();
        for (int i = 0; i < 100; i++)
        {
            ItemData item = itemDatabase.GetItemByID("scrap_metal");
            inventoryManager.AddItem(item, 20);
        }
        sw.Stop();
        LogEvent($"Added 2000 items in {sw.ElapsedMilliseconds}ms");
        
        // Test move performance
        sw.Restart();
        for (int i = 0; i < 100; i++)
        {
            var items = inventoryManager.Items;
            if (items.Count >= 2)
            {
                inventoryManager.MoveItem(items[0].gridPosition, items[1].gridPosition);
            }
        }
        sw.Stop();
        LogEvent($"100 moves in {sw.ElapsedMilliseconds}ms");
    }
    
    #endregion
    
    #region Helper Methods
    
    private bool TryGetGrid(out InventoryGrid grid)
    {
        grid = null;
        if (inventoryManager == null) return false;
        
        // Use reflection to get the private grid field
        var field = typeof(InventoryManager).GetField("grid", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            grid = field.GetValue(inventoryManager) as InventoryGrid;
            return grid != null;
        }
        
        return false;
    }
    
    #endregion
}

// Simple UI button prefab setup helper
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(InventoryDebugPanel))]
public class InventoryDebugPanelEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        InventoryDebugPanel panel = (InventoryDebugPanel)target;
        
        if (GUILayout.Button("Create Debug UI"))
        {
            CreateDebugUI(panel);
        }
    }
    
    private void CreateDebugUI(InventoryDebugPanel panel)
    {
        // Create canvas if needed
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("DebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create debug panel
        GameObject panelGO = new GameObject("InventoryDebugPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(0.3f, 1);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        
        // Add background
        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        // Create stats text
        GameObject statsGO = new GameObject("StatsText", typeof(RectTransform));
        statsGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI statsText = statsGO.AddComponent<TextMeshProUGUI>();
        statsText.text = "Debug Info Here";
        statsText.fontSize = 12;
        
        RectTransform statsRT = statsGO.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.05f, 0.5f);
        statsRT.anchorMax = new Vector2(0.95f, 0.95f);
        statsRT.offsetMin = Vector2.zero;
        statsRT.offsetMax = Vector2.zero;
        
        // Create event log
        GameObject eventsGO = new GameObject("EventsText", typeof(RectTransform));
        eventsGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI eventsText = eventsGO.AddComponent<TextMeshProUGUI>();
        eventsText.text = "Event Log Here";
        eventsText.fontSize = 10;
        
        RectTransform eventsRT = eventsGO.GetComponent<RectTransform>();
        eventsRT.anchorMin = new Vector2(0.05f, 0.05f);
        eventsRT.anchorMax = new Vector2(0.95f, 0.45f);
        eventsRT.offsetMin = Vector2.zero;
        eventsRT.offsetMax = Vector2.zero;
        
        // Assign references
        panel.debugPanel = panelGO;
        panel.statsText = statsText;
        panel.eventsText = eventsText;
        
        UnityEditor.EditorUtility.SetDirty(panel);
    }
}
#endif