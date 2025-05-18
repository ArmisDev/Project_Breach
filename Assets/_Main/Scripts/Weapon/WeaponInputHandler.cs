using UnityEngine;
using UnityEngine.InputSystem;

// Component for handling weapon input
public class WeaponInputHandler : MonoBehaviour, IWeaponComponent
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference fireInputAction;
    [SerializeField] private InputActionReference reloadInputAction;
    
    private WeaponBase weapon;
    private WeaponEventsSO events;
    private bool isEnabled = false;
    private bool isFireButtonPressed = false;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        events = weapon.WeaponEvents;

        // Initial setup
        fireInputAction.action.performed += OnFirePerformed;
        fireInputAction.action.canceled += OnFireCanceled;
        reloadInputAction.action.performed += OnReloadPerformed;
    }
    
    public void Tick()
    {
        // Handle continuous fire input for automatic weapons
        if (isEnabled && 
            weapon.WeaponData.weaponType == WeaponType.FullAuto)
        {
            // Use our local flag for extra safety
            bool isButtonCurrentlyPressed = fireInputAction.action.IsPressed() && isFireButtonPressed;
            
            // Check if we're already firing and button is still pressed
            if (isButtonCurrentlyPressed)
            {
                // For full auto weapons, continuously raise the fire input event while pressed
                events.RaiseFireInputChanged(weapon, true);
            }
            else if (!isButtonCurrentlyPressed && weapon.CurrentState == WeaponState.Firing)
            {
                // Force a fire input release if the button is no longer pressed
                // This is a safety mechanism for cases where the canceled event might be missed
                events.RaiseFireInputChanged(weapon, false);
            }
        }
    }
    
    public void OnWeaponEquipped()
    {
        isEnabled = true;
        
        // Enable input actions
        fireInputAction.action.Enable();
        reloadInputAction.action.Enable();
    }
    
    public void OnWeaponUnequipped()
    {
        isEnabled = false;
        
        // Release input actions
        if (fireInputAction.action.IsPressed())
        {
            events.RaiseFireInputChanged(weapon, false);
        }
        
        // Disable input actions
        fireInputAction.action.Disable();
        reloadInputAction.action.Disable();
    }
    
    private void OnDestroy()
    {
        // Cleanup
        fireInputAction.action.performed -= OnFirePerformed;
        fireInputAction.action.canceled -= OnFireCanceled;
        reloadInputAction.action.performed -= OnReloadPerformed;
    }
    
    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        isFireButtonPressed = true;
        
        // Different behavior based on weapon type
        switch (weapon.WeaponData.weaponType)
        {
            case WeaponType.SemiAuto:
            case WeaponType.Shotgun:
            case WeaponType.Projectile:
            case WeaponType.Burst:
                // Single fire event for non-automatic weapons
                events.RaiseFireInputChanged(weapon, true);
                break;
        }
    }
    
    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        // Signal that fire button was released - make sure this is reliable
        events.RaiseFireInputChanged(weapon, false);
        
        // For extra safety, ensure we set a local flag 
        isFireButtonPressed = false;
    }
    
    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        // Signal reload button press
        events.RaiseReloadInputPressed(weapon);
    }
}