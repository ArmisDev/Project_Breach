using System;
using UnityEngine;

// A ScriptableObject for weapon events to decouple components
[CreateAssetMenu(fileName = "WeaponEvents", menuName = "Weapons/Weapon Events")]
public class WeaponEventsSO : ScriptableObject
{
    // Firing events
    public event Action<WeaponBase> OnWeaponFired;
    public event Action<WeaponBase> OnWeaponFailedToFire;
    
    // Ammo events
    public event Action<WeaponBase, int> OnAmmoChanged;
    public event Action<WeaponBase> OnMagazineEmpty;
    public event Action<WeaponBase> OnAmmoEmpty;
    
    // Reload events
    public event Action<WeaponBase> OnReloadStarted;
    public event Action<WeaponBase> OnReloadCancelled;
    public event Action<WeaponBase> OnReloadCompleted;
    
    // Animation events
    public event Action<WeaponBase, string> OnAnimationEvent;
    
    // State events
    public event Action<WeaponBase, WeaponState> OnWeaponStateChanged;
    
    // Equip/Unequip events
    public event Action<WeaponBase> OnWeaponEquipped;
    public event Action<WeaponBase> OnWeaponUnequipped;
    
    // Input events
    public event Action<WeaponBase, bool> OnFireInputChanged;
    public event Action<WeaponBase> OnReloadInputPressed;
    
    // Clear all event subscriptions (important to prevent memory leaks!)
    public void Reset()
    {
        OnWeaponFired = null;
        OnWeaponFailedToFire = null;
        OnAmmoChanged = null;
        OnMagazineEmpty = null;
        OnAmmoEmpty = null;
        OnReloadStarted = null;
        OnReloadCancelled = null;
        OnReloadCompleted = null;
        OnAnimationEvent = null;
        OnWeaponStateChanged = null;
        OnWeaponEquipped = null;
        OnWeaponUnequipped = null;
        OnFireInputChanged = null;
        OnReloadInputPressed = null;
    }
    
    // Helper functions to raise events
    public void RaiseWeaponFired(WeaponBase weapon) => OnWeaponFired?.Invoke(weapon);
    public void RaiseWeaponFailedToFire(WeaponBase weapon) => OnWeaponFailedToFire?.Invoke(weapon);
    public void RaiseAmmoChanged(WeaponBase weapon, int newAmount) => OnAmmoChanged?.Invoke(weapon, newAmount);
    public void RaiseMagazineEmpty(WeaponBase weapon) => OnMagazineEmpty?.Invoke(weapon);
    public void RaiseAmmoEmpty(WeaponBase weapon) => OnAmmoEmpty?.Invoke(weapon);
    public void RaiseReloadStarted(WeaponBase weapon) => OnReloadStarted?.Invoke(weapon);
    public void RaiseReloadCancelled(WeaponBase weapon) => OnReloadCancelled?.Invoke(weapon);
    public void RaiseReloadCompleted(WeaponBase weapon) => OnReloadCompleted?.Invoke(weapon);
    public void RaiseAnimationEvent(WeaponBase weapon, string eventName) => OnAnimationEvent?.Invoke(weapon, eventName);
    public void RaiseWeaponStateChanged(WeaponBase weapon, WeaponState newState) => OnWeaponStateChanged?.Invoke(weapon, newState);
    public void RaiseWeaponEquipped(WeaponBase weapon) => OnWeaponEquipped?.Invoke(weapon);
    public void RaiseWeaponUnequipped(WeaponBase weapon) => OnWeaponUnequipped?.Invoke(weapon);
    public void RaiseFireInputChanged(WeaponBase weapon, bool isPressed) => OnFireInputChanged?.Invoke(weapon, isPressed);
    public void RaiseReloadInputPressed(WeaponBase weapon) => OnReloadInputPressed?.Invoke(weapon);
}

// Enum to track weapon states
public enum WeaponState
{
    Ready,
    Firing,
    Reloading,
    Empty,
    Equipping,
    Holstering,
    Disabled
}