using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Manager class for weapon switching, equipping, etc.
public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private List<WeaponBase> availableWeapons = new List<WeaponBase>();
    [SerializeField] private Transform weaponHolder;
    
    [Header("Weapon Events")]
    [SerializeField] private WeaponEventsSO weaponEvents;
    
    [Header("Weapon Switching")]
    [SerializeField] private InputActionReference nextWeaponAction;
    [SerializeField] private InputActionReference previousWeaponAction;
    [SerializeField] private float switchCooldown = 0.5f;
    
    // Internal state
    private WeaponBase currentWeapon;
    private int currentWeaponIndex = -1;
    private float lastSwitchTime;
    private bool isSwitchingWeapon = false;
    
    private void Awake()
    {
        // Setup input actions
        nextWeaponAction.action.performed += HandleNextWeapon;
        previousWeaponAction.action.performed += HandlePreviousWeapon;
        
        // Disable all weapons initially
        foreach (var weapon in availableWeapons)
        {
            weapon.gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Enable input
        nextWeaponAction.action.Enable();
        previousWeaponAction.action.Enable();
        
        // Subscribe to events
        if (weaponEvents != null)
        {
            weaponEvents.OnAnimationEvent += HandleAnimationEvent;
        }
    }
    
    private void OnDisable()
    {
        // Disable input
        nextWeaponAction.action.Disable();
        previousWeaponAction.action.Disable();
        
        // Unsubscribe from events
        if (weaponEvents != null)
        {
            weaponEvents.OnAnimationEvent -= HandleAnimationEvent;
        }
    }
    
    private void Start()
    {
        // Equip first weapon if available
        if (availableWeapons.Count > 0)
        {
            EquipWeapon(0);
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup input actions
        nextWeaponAction.action.performed -= HandleNextWeapon;
        previousWeaponAction.action.performed -= HandlePreviousWeapon;
    }
    
    // Equip a weapon by index
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Count)
        {
            Debug.LogWarning($"Invalid weapon index: {index}");
            return;
        }
        
        // Don't allow switching during cooldown or while already switching
        if (Time.time - lastSwitchTime < switchCooldown || isSwitchingWeapon)
        {
            return;
        }
        
        lastSwitchTime = Time.time;
        
        // Start switching process
        StartSwitchingWeapon(index);
    }
    
    // Start the weapon switching process
    private void StartSwitchingWeapon(int newIndex)
    {
        if (currentWeaponIndex == newIndex) return;
        
        isSwitchingWeapon = true;
        
        // Handle current weapon holstering
        if (currentWeapon != null)
        {
            // Holster current weapon
            currentWeapon.ChangeState(WeaponState.Holstering);
            // Actual unequip happens after holster animation completes
        }
        else
        {
            // No current weapon, proceed immediately
            CompleteWeaponSwitch(newIndex);
        }
    }
    
    // Complete the weapon switch after unequipping
    private void CompleteWeaponSwitch(int newIndex)
    {
        // Unequip current weapon if any
        if (currentWeapon != null)
        {
            currentWeapon.Unequip();
            currentWeapon.gameObject.SetActive(false);
        }
        
        // Update indices
        currentWeaponIndex = newIndex;
        currentWeapon = availableWeapons[newIndex];
        
        // Activate and equip new weapon
        currentWeapon.gameObject.SetActive(true);
        currentWeapon.Equip();
        
        isSwitchingWeapon = false;
    }
    
    // Get the next weapon index
    private int GetNextWeaponIndex()
    {
        int nextIndex = currentWeaponIndex + 1;
        if (nextIndex >= availableWeapons.Count)
        {
            nextIndex = 0;
        }
        return nextIndex;
    }
    
    // Get the previous weapon index
    private int GetPreviousWeaponIndex()
    {
        int prevIndex = currentWeaponIndex - 1;
        if (prevIndex < 0)
        {
            prevIndex = availableWeapons.Count - 1;
        }
        return prevIndex;
    }
    
    // Add a new weapon
    public void AddWeapon(WeaponBase newWeapon)
    {
        if (newWeapon == null) return;
        
        // Check if already in inventory
        if (!availableWeapons.Contains(newWeapon))
        {
            // Parent to weapon holder if available
            if (weaponHolder != null)
            {
                newWeapon.transform.SetParent(weaponHolder);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;
            }
            
            availableWeapons.Add(newWeapon);
            
            // Ensure weapon is initially inactive
            newWeapon.gameObject.SetActive(false);
        }
    }
    
    #region Input Handlers
    
    private void HandleNextWeapon(InputAction.CallbackContext context)
    {
        EquipWeapon(GetNextWeaponIndex());
    }
    
    private void HandlePreviousWeapon(InputAction.CallbackContext context)
    {
        EquipWeapon(GetPreviousWeaponIndex());
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleAnimationEvent(WeaponBase weapon, string eventName)
    {
        // Only process events for the current weapon
        if (weapon != currentWeapon) return;
        
        switch (eventName)
        {
            case "HolsterComplete":
                // Now complete the weapon switch
                if (isSwitchingWeapon)
                {
                    CompleteWeaponSwitch(currentWeaponIndex == -1 ? 0 : GetNextWeaponIndex());
                }
                break;
        }
    }
    
    #endregion
}