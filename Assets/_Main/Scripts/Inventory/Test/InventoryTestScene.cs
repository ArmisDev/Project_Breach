using UnityEngine;
using UnityEngine.UI;

public class InventoryTestSetup : MonoBehaviour
{
    [Header("Test Scene Components")]
    public InventoryManager inventoryManager;
    public InventoryUI inventoryUI;
    public ItemDatabase itemDatabase;
    
    [Header("Test Settings")]
    public int testGridWidth = 8;
    public int testGridHeight = 6;
    public int testMaxWeight = 50;
    
    [Header("Test Items to Spawn")]
    public string[] testItemIDs = new string[]
    {
        "medkit",
        "battery",
        "scrap_metal",
        "dimensional_artifact"
    };
    
    [Header("UI References")]
    public Canvas inventoryCanvas;
    public GameObject inventoryPanelPrefab;
    
    private void Start()
    {
        SetupTestScene();
    }
    
    public void SetupTestScene()
    {
        Debug.Log("=== Setting up Inventory Test Scene ===");
        
        // Create main inventory manager if not assigned
        if (inventoryManager == null)
        {
            GameObject imGO = new GameObject("InventoryManager");
            inventoryManager = imGO.AddComponent<InventoryManager>();
        }
        
        // Create canvas if not present
        if (inventoryCanvas == null)
        {
            GameObject canvasGO = new GameObject("InventoryCanvas");
            inventoryCanvas = canvasGO.AddComponent<Canvas>();
            inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create inventory UI if not assigned
        if (inventoryUI == null)
        {
            GameObject uiGO = new GameObject("InventoryUI", typeof(RectTransform));
            uiGO.transform.SetParent(inventoryCanvas.transform, false);
            inventoryUI = uiGO.AddComponent<InventoryUI>();
            
            // Set up basic UI structure
            RectTransform rt = uiGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800, 600);
            rt.anchoredPosition = Vector2.zero;
            
            // Add background
            Image bg = uiGO.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
        }
        
        // Spawn test items
        SpawnTestItems();
        
        // Subscribe to events for testing
        SubscribeToEvents();
        
        Debug.Log("Test scene setup complete!");
    }
    
    public void SpawnTestItems()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase not assigned! Cannot spawn test items.");
            return;
        }
        
        Debug.Log("Spawning test items...");
        
        foreach (string itemID in testItemIDs)
        {
            ItemData itemData = itemDatabase.GetItemByID(itemID);
            if (itemData != null)
            {
                bool success = inventoryManager.AddItem(itemData, 1, "TestSpawn");
                Debug.Log($"Spawned {itemData.itemName}: {success}");
            }
            else
            {
                Debug.LogWarning($"Item {itemID} not found in database");
            }
        }
    }
    
    private void SubscribeToEvents()
    {
        InventoryEvents.OnItemAdded += OnItemAdded;
        InventoryEvents.OnItemRemoved += OnItemRemoved;
        InventoryEvents.OnInventoryChanged += OnInventoryChanged;
        InventoryEvents.OnWeightChanged += OnWeightChanged;
    }
    
    private void OnDestroy()
    {
        InventoryEvents.OnItemAdded -= OnItemAdded;
        InventoryEvents.OnItemRemoved -= OnItemRemoved;
        InventoryEvents.OnInventoryChanged -= OnInventoryChanged;
        InventoryEvents.OnWeightChanged -= OnWeightChanged;
    }
    
    #region Event Handlers
    
    private void OnItemAdded(InventoryItem item)
    {
        Debug.Log($"[EVENT] Item Added: {item.itemData.itemName} at {item.gridPosition}");
    }
    
    private void OnItemRemoved(InventoryItem item)
    {
        Debug.Log($"[EVENT] Item Removed: {item.itemData.itemName}");
    }
    
    private void OnInventoryChanged()
    {
        Debug.Log($"[EVENT] Inventory Changed. Total items: {inventoryManager.Items.Count}");
    }
    
    private void OnWeightChanged(int newWeight)
    {
        Debug.Log($"[EVENT] Weight Changed: {newWeight}/{inventoryManager.MaxWeight}");
    }
    
    #endregion
    
    #region Test Commands
    
    [ContextMenu("Test Add Random Item")]
    public void TestAddRandomItem()
    {
        if (testItemIDs.Length == 0) return;
        
        string randomID = testItemIDs[Random.Range(0, testItemIDs.Length)];
        ItemData itemData = itemDatabase.GetItemByID(randomID);
        
        if (itemData != null)
        {
            bool success = inventoryManager.AddItem(itemData, 1, "RandomTest");
            Debug.Log($"Random item add result: {success}");
        }
    }
    
    [ContextMenu("Test Save Inventory")]
    public void TestSaveInventory()
    {
        var saveData = inventoryManager.GetSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        Debug.Log("=== SAVE DATA ===");
        Debug.Log(json);
    }
    
    [ContextMenu("Clear Inventory")]
    public void ClearInventory()
    {
        var items = inventoryManager.Items;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            inventoryManager.RemoveItemAt(items[i].gridPosition);
        }
        Debug.Log("Inventory cleared");
    }
    
    [ContextMenu("Test Grid Debug")]
    public void TestGridDebug()
    {
        // Access the grid through reflection for debugging
        var grid = inventoryManager.GetType().GetField("grid", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance)?.GetValue(inventoryManager) as InventoryGrid;
        
        if (grid != null)
        {
            Debug.Log(grid.GetGridDebugString());
        }
    }
    
    #endregion
}

// Component to create test items in editor
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(InventoryTestSetup))]
public class InventoryTestSetupEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        InventoryTestSetup setup = (InventoryTestSetup)target;
        
        if (GUILayout.Button("Setup Test Scene"))
        {
            setup.SetupTestScene();
        }
        
        if (GUILayout.Button("Spawn Test Items"))
        {
            setup.SpawnTestItems();
        }
        
        if (GUILayout.Button("Clear Inventory"))
        {
            setup.ClearInventory();
        }
        
        if (GUILayout.Button("Test Save"))
        {
            setup.TestSaveInventory();
        }
        
        if (GUILayout.Button("Show Grid Debug"))
        {
            setup.TestGridDebug();
        }
    }
}
#endif

// Helper component to create basic item database for testing
public class TestItemDatabaseCreator : MonoBehaviour
{
    [ContextMenu("Create Test Item Database")]
    public void CreateTestDatabase()
    {
#if UNITY_EDITOR
        // Create database asset
        ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();
        UnityEditor.AssetDatabase.CreateAsset(database, "Assets/TestItemDatabase.asset");
        
        // Create test items
        CreateTestItem("medkit", "Medkit", "Heals 50HP", new Vector2Int(1, 1), ItemType.Consumable, true, 10);
        CreateTestItem("battery", "Power Cell", "Energy source", new Vector2Int(1, 1), ItemType.Technology, true, 5);
        CreateTestItem("scrap_metal", "Scrap Metal", "Crafting material", new Vector2Int(1, 1), ItemType.Material, true, 20);
        CreateTestItem("dimensional_artifact", "Dimensional Crystal", "Reality-bending artifact", new Vector2Int(2, 2), ItemType.Artifact, false, 1);
        
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Test item database created!");
#endif
    }
    
#if UNITY_EDITOR
    private void CreateTestItem(string id, string name, string description, Vector2Int size, ItemType type, bool stackable, int maxStack)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemID = id;
        item.itemName = name;
        item.description = description;
        item.size = size;
        item.type = type;
        item.isStackable = stackable;
        item.maxStackSize = maxStack;
        item.weight = Random.Range(0.5f, 5f);
        
        // Create a simple icon texture
        Texture2D tex = new Texture2D(64, 64);
        Color color = Random.ColorHSV();
        for (int i = 0; i < tex.width * tex.height; i++)
        {
            tex.SetPixel(i % tex.width, i / tex.width, color);
        }
        tex.Apply();
        
        // Create sprite from texture
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        item.icon = sprite;
        
        UnityEditor.AssetDatabase.CreateAsset(item, $"Assets/Items/{id}.asset");
        UnityEditor.AssetDatabase.CreateAsset(tex, $"Assets/Items/{id}_icon.png");
        UnityEditor.AssetDatabase.CreateAsset(sprite, $"Assets/Items/{id}_sprite.asset");
        
        // Add to database
        ItemDatabase database = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/TestItemDatabase.asset");
        if (database != null)
        {
            database.AddItem(item);
        }
    }
#endif
}