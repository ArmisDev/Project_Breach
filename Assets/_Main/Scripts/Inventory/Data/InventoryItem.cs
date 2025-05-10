using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class InventoryItem
{
    [Header("Reference")]
    public ItemData itemData;
    
    [Header("Grid Placement")]
    public Vector2Int gridPosition;
    public bool isRotated = false; // For items that can be rotated in grid
    
    [Header("Stack Information")]
    public int stackCount = 1;
    
    [Header("Instance Properties")]
    public float currentCondition; // Current durability/wear
    public float currentPower = 0f; // For rechargeable devices
    public string uniqueID; // For tracking individual items
    
    [Header("Modifications")]
    public List<ItemModification> modifications = new List<ItemModification>();
    
    [Header("History")]
    public float timeAcquired; // When the item was picked up
    public string acquiredFrom; // How/where it was obtained
    
    // Constructor for new item instances
    public InventoryItem(ItemData data, Vector2Int position)
    {
        itemData = data;
        gridPosition = position;
        stackCount = 1;
        currentCondition = data.condition;
        currentPower = 0f;
        uniqueID = Guid.NewGuid().ToString();
        modifications = new List<ItemModification>();
        timeAcquired = Time.time;
        acquiredFrom = "Unknown";
    }
    
    // Copy constructor for splitting stacks
    public InventoryItem(InventoryItem source, int newStackCount, Vector2Int newPosition)
    {
        itemData = source.itemData;
        gridPosition = newPosition;
        stackCount = newStackCount;
        currentCondition = source.currentCondition;
        currentPower = source.currentPower;
        uniqueID = Guid.NewGuid().ToString(); // New unique ID
        modifications = new List<ItemModification>();
        foreach (var mod in source.modifications)
        {
            modifications.Add(mod); // Copy modifications
        }
        timeAcquired = Time.time;
        acquiredFrom = source.acquiredFrom;
    }
    
    // Properties for easy access
    public Vector2Int Size => isRotated ? new Vector2Int(itemData.size.y, itemData.size.x) : itemData.size;
    public bool IsStackable => itemData.isStackable;
    public bool CanStackWith(InventoryItem other)
    {
        if (!IsStackable || !other.IsStackable) return false;
        if (itemData.itemID != other.itemData.itemID) return false;
        
        // Don't stack items with different modifications
        if (modifications.Count != other.modifications.Count) return false;
        
        // Only stack items with similar condition
        return Mathf.Abs(currentCondition - other.currentCondition) < 10f;
    }
    
    // Utility methods
    public bool IsDamaged() => currentCondition < itemData.condition;
    public bool NeedsPower() => itemData.requiresPower && currentPower < itemData.powerCost;
    public float ConditionPercentage() => currentCondition / itemData.condition;
    
    // Apply damage/wear
    public void ApplyDamage(float damage)
    {
        currentCondition = Mathf.Max(0, currentCondition - damage);
    }
    
    // Recharge/repair
    public void Repair(float amount)
    {
        currentCondition = Mathf.Min(itemData.condition, currentCondition + amount);
    }
    
    public void Recharge(float amount)
    {
        currentPower = Mathf.Min(100f, currentPower + amount);
    }
    
    // Modify item
    public void AddModification(ItemModification mod)
    {
        modifications.Add(mod);
    }
    
    public bool HasModification(string modID)
    {
        return modifications.Exists(mod => mod.modificationID == modID);
    }
    
    // For display purposes
    public string GetDisplayName()
    {
        string name = itemData.itemName;
        
        if (IsDamaged())
        {
            if (currentCondition / itemData.condition < 0.25f)
                name = "Broken " + name;
            else if (currentCondition / itemData.condition < 0.5f)
                name = "Damaged " + name;
            else if (currentCondition / itemData.condition < 0.75f)
                name = "Worn " + name;
        }
        
        if (modifications.Count > 0)
        {
            name += " (Modified)";
        }
        
        return name;
    }
    
    public string GetTooltipText()
    {
        string tooltip = itemData.description + "\n\n";
        
        // Add condition info
        if (itemData.condition > 0)
        {
            tooltip += $"Condition: {currentCondition:F1}/{itemData.condition}\n";
        }
        
        // Add power info
        if (itemData.requiresPower)
        {
            tooltip += $"Power: {currentPower:F1}%\n";
        }
        
        // Add modifications
        if (modifications.Count > 0)
        {
            tooltip += "\nModifications:\n";
            foreach (var mod in modifications)
            {
                tooltip += $"• {mod.name}: {mod.description}\n";
            }
        }
        
        // Add dimensional artifact info
        if (itemData.isDimensionalArtifact)
        {
            tooltip += $"\nDimensional Stability: {itemData.dimensionalStability}/10\n";
        }
        
        // Add weight info
        tooltip += $"\nWeight: {itemData.weight * stackCount:F1} kg";
        
        return tooltip;
    }
}

// Additional classes for item modifications
[System.Serializable]
public class ItemModification
{
    public string modificationID;
    public string name;
    public string description;
    public ModificationType type;
    public float value;
    
    // For visual modifications
    public Sprite modIcon;
    public Color tintColor = Color.white;
}

[System.Serializable]
public enum ModificationType
{
    Damage,              // +X damage
    Accuracy,            // +X accuracy
    FireRate,            // +X fire rate
    Capacity,            // +X ammo/power capacity
    Durability,          // +X condition
    Weight,              // -X weight
    Power,               // -X power cost
    Stability,           // +X dimensional stability
    ArmorPenetration,    // +X armor piercing
    Scope,               // Adds zoom/targeting
    Silencer,            // Reduces noise
    Flashlight,          // Adds light source
    Custom               // For special modifications
}