using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("Grid Properties")]
    public Vector2Int size = Vector2Int.one;
    
    [Header("Stacking")]
    public bool isStackable = false;
    [Range(1, 999)]
    public int maxStackSize = 1;
    
    [Header("Item Type")]
    public ItemType type;
    public ItemRarity rarity = ItemRarity.Common;
    
    [Header("Visual")]
    public Sprite icon;
    public GameObject worldModel;
    
    [Header("Survival Elements")]
    [Range(0, 100)]
    public int condition = 100; // Durability for tools/weapons
    public float weight = 1f; // Could affect movement/stamina
    
    [Header("Sci-Fi Elements")]
    public bool requiresPower = false; // For 70s tech devices
    [Range(0, 100)]
    public int powerCost = 0; // Power consumption per use
    public bool isDimensionalArtifact = false; // Special properties for artifacts
    [Range(0, 10)]
    public int dimensionalStability = 10; // How stable the item is in this reality
    
    [Header("Resource Properties")]
    public bool isConsumable = false;
    public float healAmount = 0f; // For medkits, food
    public float staminaAmount = 0f; // For energy drinks, stims
    public float radiationReduction = 0f; // For rad pills
    
    [Header("Equipment Properties")]
    public bool isEquippable = false;
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;
    public float armorRating = 0f; // For armor pieces
    public float damage = 0f; // For weapons
    public float fireRate = 0f; // For firearms
    
    [Header("Crafting")]
    public bool isCraftingMaterial = false;
    public Material materialType = Material.None;
    
    // Custom properties for special items
    [System.Serializable]
    public class CustomProperty
    {
        public string name;
        public string value;
    }
    
    [Header("Custom Properties")]
    public CustomProperty[] customProperties;
}

[System.Serializable]
public enum ItemType
{
    Weapon,      // 70s firearms, energy weapons
    Tool,        // Geiger counter, multitool, flashlight
    Consumable,  // Food, water, medkits, ammo
    Armor,       // Radiation suits, combat armor
    Artifact,    // Dimensional objects
    Material,    // Scrap metal, chemicals, wire
    Quest,       // Story items, data tapes
    Container,   // Crates, lockers, canisters
    Technology,  // 70s computers, radio equipment
    Misc         // Misc items
}

[System.Serializable]
public enum ItemRarity
{
    Common,      // Junk, basic supplies
    Uncommon,    // Decent tools, weapons
    Rare,        // Quality equipment
    Epic,        // Pre-war tech
    Legendary,   // Dimensional artifacts
    Anomalous    // Reality-warping items
}

[System.Serializable]
public enum EquipmentSlot
{
    None,
    PrimaryWeapon,
    SecondaryWeapon,
    Sidearm,
    Head,        // Gas mask, scope
    Torso,       // Armor vest
    Legs,        // Rad suit bottom
    Feet,        // Hazard boots
    Hands,       // Hazmat gloves
    Accessory,   // Geiger counter, radio
    Backpack     // Expanded storage
}

[System.Serializable]
public enum Material
{
    None,
    Scrap,           // Basic metal
    Electronics,     // Circuit boards, wires
    Chemical,        // Cleaning supplies, fuel
    Organic,         // Food, biological matter
    PreWarTech,      // Rare 70s technology
    DimensionalCrystal, // Reality-warping material
    Ammunition,      // Gun parts, bullets
    Energy           // Power cells, batteries
}