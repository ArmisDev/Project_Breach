using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemDragUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    private InventoryItem item;
    private InventoryUI inventoryUI;
    private Image image;
    private CanvasGroup canvasGroup;
    
    public void Setup(InventoryItem inventoryItem, InventoryUI ui)
    {
        if (inventoryItem == null)
        {
            Debug.LogError("ItemDragUI.Setup called with null item");
            return;
        }
        
        if (ui == null)
        {
            Debug.LogError("ItemDragUI.Setup called with null InventoryUI");
            return;
        }
        
        item = inventoryItem;
        inventoryUI = ui;
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (image == null)
        {
            Debug.LogWarning("ItemDragUI: Image component not found");
        }
        
        if (canvasGroup == null)
        {
            Debug.LogWarning("ItemDragUI: CanvasGroup component not found");
        }
        
        if (image != null && item != null && item.itemData != null)
        {
            image.sprite = item.itemData.icon;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null)
        {
            Debug.LogError("Cannot begin drag: missing required components");
            return;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
        
        inventoryUI.StartDragging(item, eventData.position);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            Debug.LogError("Cannot drag: InventoryUI is null");
            return;
        }
        
        inventoryUI.UpdateDragging(eventData.position);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            Debug.LogError("Cannot end drag: InventoryUI is null");
            return;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        inventoryUI.EndDragging(eventData.position);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null)
        {
            Debug.LogError("Cannot handle click: item is null");
            return;
        }
        
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryEvents.TriggerItemRightClick(item);
        }
        else if (eventData.clickCount == 2)
        {
            InventoryEvents.TriggerItemDoubleClick(item);
        }
    }
}