using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDropHandler
{
    [Header("Slot Components")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI stackCountText;
    [SerializeField] private TextMeshProUGUI hotkeyText;
    
    [Header("Visual States")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color occupiedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color quickSlotColor = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Header("Sci-Fi Effects")]
    [SerializeField] private ParticleSystem scanlineEffect;
    [SerializeField] private Animation slotAnimation;
    [SerializeField] private float borderGlowSpeed = 1f;
    
    [Header("Performance")]
    [SerializeField] private float borderGlowUpdateInterval = 0.1f; // Update every 100ms instead of every frame
    private float lastBorderGlowUpdate = 0f;
    
    // Internal state
    private Vector2Int gridPosition;
    private InventoryItem currentItem;
    private InventoryUI inventoryUI;
    private bool isQuickSlot = false;
    private int quickSlotIndex = -1;
    private bool isHovered = false;
    
    // Border glow effect
    private float borderGlowTimer = 0f;
    private Color baseBorderColor;
    
    private void Awake()
    {
        if (borderImage != null)
        {
            baseBorderColor = borderImage.color;
        }
    }
    
    private void Update()
    {
        // Only update border glow at intervals instead of every frame
        if (Time.time - lastBorderGlowUpdate >= borderGlowUpdateInterval)
        {
            UpdateBorderGlow();
            lastBorderGlowUpdate = Time.time;
        }
    }
    
    #region Initialization
    
    public void Initialize(Vector2Int position, InventoryUI ui)
    {
        gridPosition = position;
        inventoryUI = ui;
        isQuickSlot = false;
        
        // Setup visual state
        UpdateVisualState();
        
        // Debug display position (can be removed in final version)
        if (GetComponentInChildren<TextMeshProUGUI>() == null)
        {
            GameObject debugText = new GameObject("DebugText");
            debugText.transform.SetParent(transform);
            TextMeshProUGUI debugTMP = debugText.AddComponent<TextMeshProUGUI>();
            debugTMP.text = $"{position.x},{position.y}";
            debugTMP.fontSize = 8;
            debugTMP.alignment = TextAlignmentOptions.Center;
            RectTransform rt = debugText.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.one * 20;
        }
    }
    
    public void InitializeAsQuickSlot(int index, InventoryUI ui)
    {
        quickSlotIndex = index;
        inventoryUI = ui;
        isQuickSlot = true;
        
        // Setup quick slot appearance
        if (slotBackground != null)
        {
            slotBackground.color = quickSlotColor;
        }
        
        // Display hotkey (1-9)
        if (hotkeyText != null)
        {
            hotkeyText.text = (index + 1).ToString();
            hotkeyText.gameObject.SetActive(true);
        }
        
        UpdateVisualState();
    }
    
    #endregion
    
    #region Item Management
    
    public void SetItem(InventoryItem item)
    {
        currentItem = item;
        UpdateItemDisplay();
        UpdateVisualState();
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        UpdateItemDisplay();
        UpdateVisualState();
    }
    
    private void UpdateItemDisplay()
    {
        if (currentItem != null)
        {
            // Show item icon
            if (itemIcon != null && currentItem.itemData != null)
            {
                itemIcon.sprite = currentItem.itemData.icon;
                itemIcon.gameObject.SetActive(true);
                
                // Apply condition-based tinting
                float conditionPercent = currentItem.ConditionPercentage();
                Color iconColor = Color.white;
                
                if (conditionPercent < 0.25f)
                    iconColor = Color.Lerp(Color.red, Color.yellow, conditionPercent * 4);
                else if (conditionPercent < 0.5f)
                    iconColor = Color.Lerp(Color.yellow, Color.white, (conditionPercent - 0.25f) * 4);
                
                itemIcon.color = iconColor;
            }
            else if (itemIcon == null)
            {
                Debug.LogWarning("itemIcon is not assigned on slot");
            }
            
            // Show stack count
            if (stackCountText != null)
            {
                if (currentItem.stackCount > 1)
                {
                    stackCountText.text = currentItem.stackCount.ToString();
                    stackCountText.gameObject.SetActive(true);
                }
                else
                {
                    stackCountText.gameObject.SetActive(false);
                }
            }
            
            // Add power indicator for tech items
            if (currentItem.itemData != null && currentItem.itemData.requiresPower)
            {
                // You could add a power indicator here
                // For example, a small battery icon or power bar
            }
        }
        else
        {
            // Hide item display
            if (itemIcon != null)
            {
                itemIcon.gameObject.SetActive(false);
            }
            
            if (stackCountText != null)
            {
                stackCountText.gameObject.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    private void UpdateVisualState()
    {
        if (slotBackground == null) return;
        
        Color targetColor = normalColor;
        
        if (isQuickSlot)
        {
            targetColor = quickSlotColor;
        }
        else if (currentItem != null)
        {
            targetColor = occupiedColor;
        }
        
        if (isHovered)
        {
            targetColor = Color.Lerp(targetColor, hoverColor, 0.5f);
        }
        
        slotBackground.color = targetColor;
    }
    
    public void ShowHighlight(Color color)
    {
        if (highlightImage != null)
        {
            highlightImage.color = color;
            highlightImage.gameObject.SetActive(color.a > 0);
        }
    }
    
    private void UpdateBorderGlow()
    {
        if (borderImage == null) return;
        
        // Apply sci-fi border glow effect
        borderGlowTimer += Time.deltaTime * borderGlowSpeed;
        
        if (isQuickSlot)
        {
            // Pulsing glow for quick slots
            float glow = 0.5f + 0.5f * Mathf.Sin(borderGlowTimer);
            Color glowColor = baseBorderColor;
            glowColor.a = glowColor.a * glow;
            borderImage.color = glowColor;
        }
        else if (currentItem != null && currentItem.itemData.isDimensionalArtifact)
        {
            // Special effect for dimensional artifacts
            float glow = 0.3f + 0.7f * Mathf.Sin(borderGlowTimer * 2f);
            Color glowColor = Color.cyan;
            glowColor.a = 0.5f * glow;
            borderImage.color = glowColor;
        }
        else
        {
            borderImage.color = baseBorderColor;
        }
    }
    
    #endregion
    
    #region Mouse Events
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
        
        // Show tooltip for item
        if (currentItem != null && inventoryUI != null)
        {
            inventoryUI.ShowTooltip(currentItem, eventData.position);
        }
        
        // Play hover sound effect (you can add AudioSource here)
        PlayHoverSound();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
        
        // Hide tooltip
        if (inventoryUI != null)
        {
            inventoryUI.HideTooltip();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // This is handled by the InventoryUI drag system
        // But we can add visual feedback here
        if (isQuickSlot && eventData.pointerDrag != null)
        {
            var draggedItem = eventData.pointerDrag.GetComponent<ItemDragUI>();
            if (draggedItem != null)
            {
                // Try to assign to quick slot
                // This would be handled by the inventory manager
            }
        }
    }
    
    private void HandleLeftClick()
    {
        if (isQuickSlot && currentItem != null)
        {
            // Use quick slot item
            if (inventoryUI != null && inventoryUI.GetComponent<InventoryManager>() != null)
            {
                inventoryUI.GetComponent<InventoryManager>().UseQuickSlot(quickSlotIndex);
            }
        }
    }
    
    private void HandleRightClick()
    {
        if (currentItem != null)
        {
            // Context menu or item info
            InventoryEvents.TriggerItemRightClick(currentItem);
        }
    }
    
    #endregion
    
    #region Audio & Effects
    
    private void PlayHoverSound()
    {
        // Add hover sound effect for retro sci-fi feel
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null && audio.clip != null)
        {
            audio.pitch = Random.Range(0.95f, 1.05f); // Slight pitch variation
            audio.Play();
        }
    }
    
    private void PlayClickSound()
    {
        // Add click sound effect
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null && audio.clip != null)
        {
            audio.pitch = Random.Range(0.9f, 1.1f);
            audio.Play();
        }
    }
    
    #endregion
    
    #region Accessors
    
    public Vector2Int GridPosition => gridPosition;
    public InventoryItem Item => currentItem;
    public bool IsQuickSlot => isQuickSlot;
    public int QuickSlotIndex => quickSlotIndex;
    
    #endregion
}

// Extension for slot animations
[System.Serializable]
public class SlotAnimationData
{
    public AnimationClip hoverAnimation;
    public AnimationClip clickAnimation;
    public AnimationClip errorAnimation;
    public AnimationClip successAnimation;
}

// Component for adding sci-fi visual effects to slots
public class SlotScanlineEffect : MonoBehaviour
{
    [Header("Scanline Settings")]
    public float scanlineSpeed = 2f;
    public float scanlineOpacity = 0.3f;
    public Color scanlineColor = Color.cyan;
    
    private Image scanlineImage;
    private float timer;
    
    private void Start()
    {
        scanlineImage = GetComponent<Image>();
        if (scanlineImage == null)
        {
            scanlineImage = gameObject.AddComponent<Image>();
        }
    }
    
    private void Update()
    {
        if (scanlineImage == null) return;
        
        timer += Time.deltaTime * scanlineSpeed;
        
        // Create scrolling scanline effect
        float yPos = (timer % 1f);
        scanlineImage.transform.localPosition = new Vector3(0, yPos * 100 - 50, 0);
        
        Color color = scanlineColor;
        color.a = scanlineOpacity * Mathf.Sin(timer * Mathf.PI);
        scanlineImage.color = color;
    }
}