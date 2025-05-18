using System.Collections;
using UnityEngine;

// Component for managing weapon ammo and reloading
public class WeaponAmmoSystem : MonoBehaviour, IWeaponComponent
{
    private WeaponBase weapon;
    private WeaponDataSO weaponData;
    private WeaponEventsSO events;
    
    // Ammo state
    private int currentMagazineAmmo;
    private int currentTotalAmmo;
    private bool isReloading = false;
    
    // Coroutine references
    private Coroutine reloadCoroutine;
    
    public int CurrentMagazineAmmo => currentMagazineAmmo;
    public int CurrentTotalAmmo => currentTotalAmmo;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        weaponData = weapon.WeaponData;
        events = weapon.WeaponEvents;
        
        // Initialize ammo
        currentMagazineAmmo = weaponData.magazineSize;
        currentTotalAmmo = weaponData.startingAmmo;
        
        // Subscribe to events
        events.OnWeaponFired += HandleWeaponFired;
        events.OnReloadStarted += HandleReloadStarted;
        events.OnReloadCancelled += HandleReloadCancelled;
        events.OnReloadInputPressed += HandleReloadInput;
    }
    
    public void Tick()
    {
        // Nothing to update per frame for ammo system
    }
    
    public void OnWeaponEquipped()
    {
        // Notify current ammo state
        events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
    }
    
    public void OnWeaponUnequipped()
    {
        // Cancel reload if in progress
        if (isReloading)
        {
            StopReload();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (events != null)
        {
            events.OnWeaponFired -= HandleWeaponFired;
            events.OnReloadStarted -= HandleReloadStarted;
            events.OnReloadCancelled -= HandleReloadCancelled;
            events.OnReloadInputPressed -= HandleReloadInput;
        }
    }
    
    // Check if weapon can fire
    public bool CanFire()
    {
        return currentMagazineAmmo > 0 && !isReloading;
    }
    
    // Consume ammo for firing
    public void ConsumeAmmo(int amount = 1)
    {
        int prevAmmo = currentMagazineAmmo;
        currentMagazineAmmo = Mathf.Max(0, currentMagazineAmmo - amount);
        
        // Notify ammo changed
        if (prevAmmo != currentMagazineAmmo)
        {
            events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
        }
        
        // Check if magazine is now empty
        if (currentMagazineAmmo == 0)
        {
            events.RaiseMagazineEmpty(weapon);
            
            // Auto reload if no ammo left
            if (currentTotalAmmo > 0 && weapon.CurrentState != WeaponState.Reloading)
            {
                StartReload();
            }
            else if (currentTotalAmmo <= 0)
            {
                events.RaiseAmmoEmpty(weapon);
            }
        }
    }
    
    // Attempt to start reload
    public void StartReload()
    {
        // Check if reload is needed and possible
        if (isReloading || currentMagazineAmmo >= weaponData.magazineSize || currentTotalAmmo <= 0)
        {
            return;
        }
        
        // If weapon doesn't allow partial reloads, only reload when magazine is empty
        if (!weaponData.canReloadPartially && currentMagazineAmmo > 0)
        {
            return;
        }
        
        // Start reload process
        isReloading = true;
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
        
        // Notify reload started
        events.RaiseReloadStarted(weapon);
    }
    
    // Stop reload in progress
    public void StopReload()
    {
        if (!isReloading) return;
        
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        
        isReloading = false;
        events.RaiseReloadCancelled(weapon);
    }
    
    // Handle reload completion
    private IEnumerator ReloadCoroutine()
    {
        // Wait for reload time
        yield return new WaitForSeconds(weaponData.reloadTime);
        
        // Calculate ammo to reload
        int ammoNeeded = weaponData.magazineSize - currentMagazineAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, currentTotalAmmo);
        
        // Update ammo counts
        currentMagazineAmmo += ammoToLoad;
        currentTotalAmmo -= ammoToLoad;
        
        // Notify reload completed
        isReloading = false;
        events.RaiseReloadCompleted(weapon);
        events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
    }
    
    // Add ammo (pickup)
    public void AddAmmo(int amount)
    {
        int prevTotal = currentTotalAmmo;
        currentTotalAmmo = Mathf.Min(weaponData.maxAmmo, currentTotalAmmo + amount);
        
        if (prevTotal != currentTotalAmmo)
        {
            events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
        }
    }
    
    #region Event Handlers
    
    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        ConsumeAmmo();
    }
    
    private void HandleReloadStarted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        // State changes handled by the weapon manager
    }
    
    private void HandleReloadCancelled(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        isReloading = false;
    }
    
    private void HandleReloadInput(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        StartReload();
    }
    
    #endregion
}