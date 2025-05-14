using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int count = 1;
    [SerializeField] private string acquiredFrom = "World Pickup";
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject itemModel; // 3D model of the item
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 2f;
    
    [Header("Interaction")]
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private LayerMask playerLayer = -1;
    [SerializeField] private bool autoPickup = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem pickupEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private bool oneTimePickup = true;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private TMPro.TextMeshPro itemNameText;
    [SerializeField] private TMPro.TextMeshPro itemCountText;
    
    [Header("Events")]
    public UnityEvent<ItemData, int> OnItemPickedUp;
    public UnityEvent OnPickupFailed;
    
    // Private members
    private float originalY;
    private bool isInRange = false;
    private FPS.Player.PlayerController currentPlayer;
    private Collider itemCollider;
    private bool hasBeenPickedUp = false;
    
    private void Start()
    {
        Setup();
    }
    
    private void Setup()
    {
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = true;
        
        // Set up visuals
        if (itemModel != null)
        {
            originalY = itemModel.transform.localPosition.y;
        }
        
        // Set up UI
        if (itemNameText != null && itemData != null)
        {
            itemNameText.text = itemData.itemName;
        }
        
        if (itemCountText != null)
        {
            if (count > 1)
            {
                itemCountText.text = $"x{count}";
            }
            else
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
        
        // Hide pickup prompt initially
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
        
        // Ensure audio source is set up
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
    }
    
    private void Update()
    {
        if (hasBeenPickedUp) return;
        
        // Floating animation
        AnimateItem();
        
        // Handle player interaction
        if (isInRange && currentPlayer != null)
        {
            if (autoPickup)
            {
                TryPickup();
            }
            else if (Input.GetKeyDown(interactKey))
            {
                TryPickup();
            }
        }
    }
    
    private void AnimateItem()
    {
        if (itemModel != null)
        {
            // Rotation
            itemModel.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            
            // Bob up and down
            float newY = originalY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 pos = itemModel.transform.localPosition;
            pos.y = newY;
            itemModel.transform.localPosition = pos;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp) return;
        
        // Check if it's a player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            currentPlayer = other.GetComponent<FPS.Player.PlayerController>();
            if (currentPlayer != null)
            {
                isInRange = true;
                
                // Show pickup prompt
                if (pickupPrompt != null)
                {
                    pickupPrompt.SetActive(true);
                }
                
                // Highlight effect
                if (itemModel != null)
                {
                    Renderer renderer = itemModel.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Add a subtle glow effect
                        renderer.material.SetFloat("_EmissionEnabled", 1);
                        renderer.material.SetColor("_EmissionColor", Color.white * 0.2f);
                    }
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPS.Player.PlayerController>() == currentPlayer)
        {
            isInRange = false;
            currentPlayer = null;
            
            // Hide pickup prompt
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
            
            // Remove highlight effect
            if (itemModel != null)
            {
                Renderer renderer = itemModel.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.SetFloat("_EmissionEnabled", 0);
                }
            }
        }
    }
    
    private void TryPickup()
    {
        if (currentPlayer == null || itemData == null || hasBeenPickedUp) return;
        
        // Get the inventory from the player
        InventoryManager inventory = currentPlayer.inventoryManager;
        if (inventory == null)
        {
            Debug.LogWarning("Player has no inventory!");
            OnPickupFailed?.Invoke();
            return;
        }
        
        // Try to add the item
        bool success = inventory.AddItem(itemData, count, acquiredFrom);
        
        if (success)
        {
            // Successful pickup
            hasBeenPickedUp = true;
            OnItemPickedUp?.Invoke(itemData, count);
            
            // Play effects
            if (pickupEffect != null)
            {
                pickupEffect.Play();
            }
            
            if (pickupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            
            // Hide visuals
            if (itemModel != null)
            {
                itemModel.SetActive(false);
            }
            
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
            
            // Destroy or respawn after delay
            if (oneTimePickup)
            {
                // Wait for sound to finish before destroying
                float destroyDelay = pickupSound != null ? pickupSound.length : 0.5f;
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                // Respawn logic could go here
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Failed pickup (inventory full)
            OnPickupFailed?.Invoke();
            
            // Show "Inventory Full" message
            if (currentPlayer != null)
            {
                // You could trigger a UI message here
                Debug.Log("Inventory is full!");
            }
        }
    }
    
    // Public method to set up the pickup dynamically
    public void Initialize(ItemData data, int itemCount = 1, string source = "World Pickup")
    {
        itemData = data;
        count = itemCount;
        acquiredFrom = source;
        Setup();
    }
    
    // Draw the pickup radius in the scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}