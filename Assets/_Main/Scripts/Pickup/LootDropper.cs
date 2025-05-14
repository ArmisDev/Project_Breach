using UnityEngine;
using System.Collections.Generic;

public class LootDropper : MonoBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        public ItemData itemData;
        [Range(0f, 1f)] public float dropChance = 0.5f;
        public int minCount = 1;
        public int maxCount = 1;
        [Tooltip("Weight affects how likely this item is to drop relative to others")]
        public float weight = 1f;
    }
    
    [Header("Loot Configuration")]
    [SerializeField] private LootItem[] possibleDrops;
    [SerializeField] private int minItems = 1;
    [SerializeField] private int maxItems = 3;
    [SerializeField] private bool guaranteedDrop = true;
    
    [Header("Drop Settings")]
    [SerializeField] private GameObject itemPickupPrefab;
    [SerializeField] private float dropRadius = 2f;
    [SerializeField] private float dropHeight = 1f;
    [SerializeField] private float dropForce = 5f;
    [SerializeField] private bool scatterItems = true;
    
    [Header("Container Settings")]
    [SerializeField] private bool isContainer = false;
    [SerializeField] private Animator containerAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip containerOpenSound;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem dropEffect;
    [SerializeField] private bool playEffectOnDrop = true;
    
    private bool hasDropped = false;
    
    #region Public Methods
    
    public void DropLoot()
    {
        if (hasDropped && isContainer) return; // Containers can only be looted once
        
        // Play container animation if applicable
        if (isContainer && containerAnimator != null)
        {
            containerAnimator.SetTrigger(openAnimationTrigger);
        }
        
        // Play sound effect
        if (containerOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(containerOpenSound);
        }
        
        // Generate loot
        List<ItemData> itemsToDrop = DetermineLoot();
        
        // Drop each item
        foreach (var item in itemsToDrop)
        {
            SpawnPickup(item);
        }
        
        hasDropped = true;
        
        // Play effects
        if (playEffectOnDrop && dropEffect != null)
        {
            dropEffect.Play();
        }
    }
    
    public void DropSpecificItem(ItemData item, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnPickup(item);
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private List<ItemData> DetermineLoot()
    {
        List<ItemData> loot = new List<ItemData>();
        
        // Determine how many items to drop
        int numItems = Random.Range(minItems, maxItems + 1);
        
        // For each item slot
        for (int i = 0; i < numItems; i++)
        {
            // Select item based on weighted probability
            ItemData selectedItem = SelectWeightedRandomItem();
            if (selectedItem != null)
            {
                // Check drop chance
                var lootItem = GetLootItemData(selectedItem);
                if (lootItem != null && Random.value <= lootItem.dropChance)
                {
                    // Add multiple if applicable
                    int count = Random.Range(lootItem.minCount, lootItem.maxCount + 1);
                    for (int j = 0; j < count; j++)
                    {
                        loot.Add(selectedItem);
                    }
                }
            }
        }
        
        // Ensure at least one item if guaranteed drop
        if (guaranteedDrop && loot.Count == 0 && possibleDrops.Length > 0)
        {
            ItemData fallbackItem = possibleDrops[Random.Range(0, possibleDrops.Length)].itemData;
            loot.Add(fallbackItem);
        }
        
        return loot;
    }
    
    private ItemData SelectWeightedRandomItem()
    {
        if (possibleDrops.Length == 0) return null;
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var item in possibleDrops)
        {
            totalWeight += item.weight;
        }
        
        // Random selection based on weight
        float randomPoint = Random.value * totalWeight;
        float currentWeight = 0f;
        
        foreach (var item in possibleDrops)
        {
            currentWeight += item.weight;
            if (randomPoint <= currentWeight)
            {
                return item.itemData;
            }
        }
        
        // Fallback
        return possibleDrops[0].itemData;
    }
    
    private LootItem GetLootItemData(ItemData itemData)
    {
        foreach (var lootItem in possibleDrops)
        {
            if (lootItem.itemData == itemData)
            {
                return lootItem;
            }
        }
        return null;
    }
    
    private void SpawnPickup(ItemData itemData)
    {
        if (itemPickupPrefab == null || itemData == null) return;
        
        // Calculate spawn position
        Vector3 spawnPosition = transform.position + Vector3.up * dropHeight;
        
        if (scatterItems)
        {
            // Add random offset
            Vector2 randomCircle = Random.insideUnitCircle * dropRadius;
            spawnPosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        // Create pickup
        GameObject pickupObject = Instantiate(itemPickupPrefab, spawnPosition, Random.rotation);
        
        // Configure the pickup
        ItemPickup pickup = pickupObject.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.Initialize(itemData, 1, "Loot Drop");
        }
        
        // Add physics for scatter effect
        Rigidbody rb = pickupObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            
            rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * dropForce, ForceMode.Impulse);
        }
    }
    
    #endregion
    
    #region Unity Events
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dropRadius);
        
        // Show drop height
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * dropHeight);
    }
    
    #endregion
    
    #region Convenience Methods
    
    // For use with trigger zones
    private void OnTriggerEnter(Collider other)
    {
        if (isContainer && other.GetComponent<FPS.Player.PlayerController>() != null)
        {
            // Auto-open container when player approaches
            // You can also make this require pressing 'E' to interact
            if (Input.GetKeyDown(KeyCode.E))
            {
                DropLoot();
            }
        }
    }
    
    #endregion
}