using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventoryGrid
{
    private InventoryItem[,] grid;
    private Vector2Int gridSize;
    
    // Constructor
    public InventoryGrid(Vector2Int size)
    {
        gridSize = size;
        grid = new InventoryItem[size.x, size.y];
        ClearGrid();
    }
    
    #region Grid Operations
    
    public void ClearGrid()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                grid[x, y] = null;
            }
        }
    }
    
    public bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize.x && 
               position.y >= 0 && position.y < gridSize.y;
    }
    
    public bool IsCellEmpty(Vector2Int position)
    {
        if (!IsPositionValid(position)) return false;
        return grid[position.x, position.y] == null;
    }
    
    #endregion
    
    #region Item Placement
    
    public bool CanPlaceItem(InventoryItem item, Vector2Int position)
    {
        if (item == null) return false;
        return CanPlaceItem(item.itemData, position, item.isRotated);
    }
    
    public bool CanPlaceItem(ItemData itemData, Vector2Int position, bool isRotated = false)
    {
        Vector2Int itemSize = isRotated ? 
            new Vector2Int(itemData.size.y, itemData.size.x) : 
            itemData.size;
        
        // Check if the item would fit within grid bounds
        if (position.x + itemSize.x > gridSize.x || 
            position.y + itemSize.y > gridSize.y ||
            position.x < 0 || position.y < 0)
        {
            return false;
        }
        
        // Check for collisions with existing items
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                Vector2Int checkPos = position + new Vector2Int(x, y);
                if (!IsCellEmpty(checkPos))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    public bool PlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!CanPlaceItem(item, position))
        {
            Debug.LogWarning($"Cannot place {item.itemData.itemName} at position {position}");
            return false;
        }
        
        Vector2Int itemSize = item.Size;
        
        // Place the item in all cells it occupies
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, y);
                grid[cellPos.x, cellPos.y] = item;
            }
        }
        
        item.gridPosition = position;
        
        // Validate state after placement
        if (ValidateGridState(out string error))
        {
            return true;
        }
        else
        {
            Debug.LogError($"Grid validation failed after placing {item.itemData.itemName}: {error}");
            // Attempt to fix the state
            if (!TryFixGridState())
            {
                Debug.LogError("Failed to fix grid state automatically");
            }
            return true; // Still return true since item was placed, just with validation warnings
        }
    }
    
    public bool RemoveItem(Vector2Int position)
    {
        InventoryItem item = GetItemAt(position);
        if (item == null) return false;
        
        Vector2Int itemSize = item.Size;
        Vector2Int itemPosition = item.gridPosition;
        
        // Clear all cells this item occupies
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                Vector2Int cellPos = itemPosition + new Vector2Int(x, y);
                if (IsPositionValid(cellPos))
                {
                    grid[cellPos.x, cellPos.y] = null;
                }
            }
        }
        
        // Validate state after removal
        if (!ValidateGridState(out string error))
        {
            Debug.LogWarning($"Grid validation warning after removing {item.itemData.itemName}: {error}");
            // Note: We don't try to fix here since removal might be part of a larger operation
        }
        
        return true;
    }
    
    #endregion
    
    #region Item Queries
    
    public InventoryItem GetItemAt(Vector2Int position)
    {
        if (!IsPositionValid(position)) return null;
        return grid[position.x, position.y];
    }
    
    public List<InventoryItem> GetAllItems()
    {
        var items = new HashSet<InventoryItem>(); // Use HashSet to avoid duplicates
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    items.Add(grid[x, y]);
                }
            }
        }
        
        return new List<InventoryItem>(items);
    }
    
    public List<Vector2Int> GetOccupiedCells(InventoryItem item)
    {
        var cells = new List<Vector2Int>();
        Vector2Int itemSize = item.Size;
        Vector2Int startPos = item.gridPosition;
        
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                cells.Add(startPos + new Vector2Int(x, y));
            }
        }
        
        return cells;
    }
    
    #endregion
    
    #region Spatial Queries
    
    public List<Vector2Int> FindAvailablePositions(Vector2Int itemSize, bool allowRotation = true)
    {
        var positions = new List<Vector2Int>();
        
        // Check normal orientation
        for (int x = 0; x <= gridSize.x - itemSize.x; x++)
        {
            for (int y = 0; y <= gridSize.y - itemSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (CanPlaceItem(new ItemData { size = itemSize }, pos))
                {
                    positions.Add(pos);
                }
            }
        }
        
        // Check rotated orientation if allowed
        if (allowRotation && itemSize.x != itemSize.y)
        {
            Vector2Int rotatedSize = new Vector2Int(itemSize.y, itemSize.x);
            for (int x = 0; x <= gridSize.x - rotatedSize.x; x++)
            {
                for (int y = 0; y <= gridSize.y - rotatedSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (CanPlaceItem(new ItemData { size = itemSize }, pos, true))
                    {
                        positions.Add(pos);
                    }
                }
            }
        }
        
        return positions;
    }
    
    public Vector2Int FindBestPosition(Vector2Int itemSize, bool preferCenter = true)
    {
        var availablePositions = FindAvailablePositions(itemSize);
        if (availablePositions.Count == 0) return Vector2Int.one * -1;
        
        if (!preferCenter) return availablePositions[0];
        
        // Find position closest to center
        Vector2 center = new Vector2(gridSize.x * 0.5f, gridSize.y * 0.5f);
        Vector2Int bestPosition = availablePositions[0];
        float bestDistance = Vector2.Distance(bestPosition, center);
        
        foreach (var pos in availablePositions)
        {
            float distance = Vector2.Distance(pos, center);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPosition = pos;
            }
        }
        
        return bestPosition;
    }
    
    public List<InventoryItem> GetItemsInRadius(Vector2Int center, int radius)
    {
        var itemsInRadius = new HashSet<InventoryItem>();
        
        for (int x = Mathf.Max(0, center.x - radius); x <= Mathf.Min(gridSize.x - 1, center.x + radius); x++)
        {
            for (int y = Mathf.Max(0, center.y - radius); y <= Mathf.Min(gridSize.y - 1, center.y + radius); y++)
            {
                if (Vector2Int.Distance(new Vector2Int(x, y), center) <= radius)
                {
                    var item = GetItemAt(new Vector2Int(x, y));
                    if (item != null) itemsInRadius.Add(item);
                }
            }
        }
        
        return new List<InventoryItem>(itemsInRadius);
    }
    
    #endregion
    
    #region Advanced Features
    
    public bool TrySwapItems(Vector2Int posA, Vector2Int posB)
    {
        var itemA = GetItemAt(posA);
        var itemB = GetItemAt(posB);
        
        if (itemA == null || itemB == null) return false;
        
        // Temporarily remove both items
        RemoveItem(itemA.gridPosition);
        RemoveItem(itemB.gridPosition);
        
        // Check if they can be placed in swapped positions
        bool canPlaceA = CanPlaceItem(itemB, itemA.gridPosition);
        bool canPlaceB = CanPlaceItem(itemA, itemB.gridPosition);
        
        if (canPlaceA && canPlaceB)
        {
            // Perform the swap
            PlaceItem(itemB, itemA.gridPosition);
            PlaceItem(itemA, itemB.gridPosition);
            return true;
        }
        else
        {
            // Restore original positions
            PlaceItem(itemA, itemA.gridPosition);
            PlaceItem(itemB, itemB.gridPosition);
            return false;
        }
    }
    
    public int GetEmptyCellCount()
    {
        int count = 0;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == null) count++;
            }
        }
        return count;
    }
    
    public float GetFillPercentage()
    {
        int totalCells = gridSize.x * gridSize.y;
        int emptyCells = GetEmptyCellCount();
        return (float)(totalCells - emptyCells) / totalCells * 100f;
    }
    
    #endregion
    
    #region Debug Utilities
    
    public string GetGridDebugString()
    {
        var debug = new System.Text.StringBuilder();
        debug.AppendLine($"Grid Size: {gridSize.x}x{gridSize.y}");
        debug.AppendLine($"Fill Percentage: {GetFillPercentage():F1}%");
        debug.AppendLine("Grid Layout:");
        
        for (int y = gridSize.y - 1; y >= 0; y--) // Start from top
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (grid[x, y] == null)
                    debug.Append("_");
                else
                    debug.Append("#");
                debug.Append(" ");
            }
            debug.AppendLine();
        }
        
        return debug.ToString();
    }
    
    #endregion
    
    #region State Validation
    
    public bool ValidateGridState(out string errorMessage)
    {
        errorMessage = string.Empty;
        var errors = new System.Text.StringBuilder();
        bool isValid = true;
        
        // Check for items with mismatched positions
        var processedItems = new HashSet<InventoryItem>();
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var item = grid[x, y];
                if (item == null) continue;
                
                // Check if this is the first time we're seeing this item
                if (!processedItems.Contains(item))
                {
                    // Validate that the item's stored position matches its actual position
                    Vector2Int expectedPos = item.gridPosition;
                    Vector2Int itemSize = item.Size;
                    
                    // Check if item claims to be at a different position
                    bool itemClaimsCorrectPosition = (expectedPos.x == x && expectedPos.y == y);
                    
                    if (!itemClaimsCorrectPosition)
                    {
                        errors.AppendLine($"Item {item.itemData.itemName} claims to be at {expectedPos} but is actually at ({x},{y})");
                        isValid = false;
                    }
                    
                    // Validate that all cells this item should occupy are correctly filled
                    bool allCellsCorrect = true;
                    for (int ox = 0; ox < itemSize.x; ox++)
                    {
                        for (int oy = 0; oy < itemSize.y; oy++)
                        {
                            Vector2Int checkPos = expectedPos + new Vector2Int(ox, oy);
                            if (IsPositionValid(checkPos))
                            {
                                if (grid[checkPos.x, checkPos.y] != item)
                                {
                                    errors.AppendLine($"Item {item.itemData.itemName} should occupy cell ({checkPos.x},{checkPos.y}) but it contains {(grid[checkPos.x, checkPos.y]?.itemData.itemName ?? "null")}");
                                    allCellsCorrect = false;
                                    isValid = false;
                                }
                            }
                        }
                    }
                    
                    if (allCellsCorrect)
                    {
                        processedItems.Add(item);
                    }
                }
            }
        }
        
        // Check for orphaned grid references
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var item = grid[x, y];
                if (item != null)
                {
                    // Verify this cell should contain this item
                    Vector2Int cellPos = new Vector2Int(x, y);
                    Vector2Int itemPos = item.gridPosition;
                    Vector2Int itemSize = item.Size;
                    
                    bool shouldContainItem = (cellPos.x >= itemPos.x && cellPos.x < itemPos.x + itemSize.x &&
                                             cellPos.y >= itemPos.y && cellPos.y < itemPos.y + itemSize.y);
                    
                    if (!shouldContainItem)
                    {
                        errors.AppendLine($"Cell ({x},{y}) contains {item.itemData.itemName} but shouldn't according to item's position {itemPos} and size {itemSize}");
                        isValid = false;
                    }
                }
            }
        }
        
        errorMessage = errors.ToString();
        return isValid;
    }
    
    public bool TryFixGridState()
    {
        // Attempt to automatically fix common desync issues
        var itemsToMove = new List<(InventoryItem item, Vector2Int currentPos)>();
        
        // Find all items and their actual grid positions
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                var item = grid[x, y];
                if (item != null)
                {
                    // Check if this is the top-left corner of the item
                    bool isTopLeftCorner = true;
                    if (x > 0 && grid[x - 1, y] == item) isTopLeftCorner = false;
                    if (y > 0 && grid[x, y - 1] == item) isTopLeftCorner = false;
                    
                    if (isTopLeftCorner)
                    {
                        itemsToMove.Add((item, new Vector2Int(x, y)));
                    }
                }
            }
        }
        
        // Clear the grid
        ClearGrid();
        
        // Attempt to place all items at their correct positions
        foreach (var (item, actualPos) in itemsToMove)
        {
            // Update item's position to match where it actually was
            item.gridPosition = actualPos;
            
            // Try to place it
            if (!PlaceItem(item, item.gridPosition))
            {
                Debug.LogError($"Failed to fix placement for {item.itemData.itemName} at {item.gridPosition}");
                return false;
            }
        }
        
        return true;
    }
    
    #endregion
}