using System.Collections;
using UnityEngine;

// Component for managing weapon ammo and reloading
public class WeaponAmmoSystem : MonoBehaviour, IWeaponComponent
{
    [Header("UI Reference")]
    [SerializeField] private HUDUI hudUI;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
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

        if (enableDebugLogs)
            Debug.Log($"Ammo initialized: {currentMagazineAmmo}/{currentTotalAmmo} for {weaponData.weaponName}");

        // Subscribe to events
        events.OnWeaponFired += HandleWeaponFired;
        events.OnReloadStarted += HandleReloadStarted;
        events.OnReloadCancelled += HandleReloadCancelled;
        events.OnReloadInputPressed += HandleReloadInput;
        
        // Update UI with initial values
        UpdateUI();
    }
    
    public void Tick()
    {
        // Nothing to update per frame for ammo system
    }
    
    public void OnWeaponEquipped()
    {
        // Notify current ammo state
        events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
        
        // Update UI immediately when weapon is equipped
        UpdateUI();
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

    void Update()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (hudUI != null)
        {
            hudUI.UpdateAmmoUI(currentMagazineAmmo, currentTotalAmmo, weaponData.magazineSize);
        }
    }

    // Check if weapon can fire
    public bool CanFire()
    {
        // Basic check for ammo and reload state
        bool canFire = currentMagazineAmmo > 0 && !isReloading;
        
        if (!canFire && enableDebugLogs)
        {
            Debug.Log($"Cannot fire: ammo={currentMagazineAmmo}, isReloading={isReloading}");
        }
        
        return canFire;
    }
    
    // Consume ammo for firing
    public void ConsumeAmmo(int amount = 1)
    {
        // Safety check
        if (amount <= 0) return;
        
        int prevAmmo = currentMagazineAmmo;
        currentMagazineAmmo = Mathf.Max(0, currentMagazineAmmo - amount);
        
        if (enableDebugLogs)
            Debug.Log($"Ammo consumed: {prevAmmo} → {currentMagazineAmmo} (amount: {amount})");
        
        // Notify ammo changed
        if (prevAmmo != currentMagazineAmmo)
        {
            events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
        }
        
        // Check if magazine is now empty
        if (currentMagazineAmmo == 0)
        {
            if (enableDebugLogs)
                Debug.Log("Magazine empty");
                
            events.RaiseMagazineEmpty(weapon);
            
            // Auto reload if needed - uncomment to enable
            // if (currentTotalAmmo > 0 && weapon.CurrentState != WeaponState.Reloading)
            // {
            //     StartReload();
            // }
            
            // Notify if completely out of ammo
            if (currentTotalAmmo <= 0)
            {
                if (enableDebugLogs)
                    Debug.Log("Completely out of ammo");
                    
                events.RaiseAmmoEmpty(weapon);
            }
        }
    }
    
    // Attempt to start reload
    public bool StartReload()
    {
        // Check if reload is needed and possible
        if (isReloading)
        {
            if (enableDebugLogs) Debug.Log("Cannot reload: Already reloading");
            return false;
        }
        
        if (currentMagazineAmmo >= weaponData.magazineSize)
        {
            if (enableDebugLogs) Debug.Log("Cannot reload: Magazine already full");
            return false;
        }
        
        if (currentTotalAmmo <= 0)
        {
            if (enableDebugLogs) Debug.Log("Cannot reload: No reserve ammo");
            return false;
        }
        
        // If weapon doesn't allow partial reloads, only reload when magazine is empty
        if (!weaponData.canReloadPartially && currentMagazineAmmo > 0)
        {
            if (enableDebugLogs) Debug.Log("Cannot reload: Partial reloads not allowed");
            return false;
        }
        
        // Start reload process
        isReloading = true;
        
        if (enableDebugLogs)
            Debug.Log($"Starting reload. Current ammo: {currentMagazineAmmo}/{currentTotalAmmo}");
        
        // Start UI reload animation - no tight coupling, just informing the UI of reload duration
        if (hudUI != null)
        {
            hudUI.StartReloadAnimation(weaponData.reloadTime);
        }
        
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
        
        // Notify reload started
        events.RaiseReloadStarted(weapon);
        return true;
    }
    
    // Stop reload in progress
    public void StopReload()
    {
        if (!isReloading)
        {
            if (enableDebugLogs) Debug.Log("Cannot stop reload: Not currently reloading");
            return;
        }
        
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        
        // Stop UI reload animation
        if (hudUI != null)
        {
            hudUI.StopReloadAnimation();
        }
        
        if (enableDebugLogs)
            Debug.Log("Reload canceled");
            
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
        int previousMagazineAmmo = currentMagazineAmmo;
        int previousTotalAmmo = currentTotalAmmo;
        
        currentMagazineAmmo += ammoToLoad;
        currentTotalAmmo -= ammoToLoad;
        
        if (enableDebugLogs)
            Debug.Log($"Reload complete. Ammo: {previousMagazineAmmo}/{previousTotalAmmo} → {currentMagazineAmmo}/{currentTotalAmmo}");
        
        // Notify reload completed
        isReloading = false;
        events.RaiseReloadCompleted(weapon);
        events.RaiseAmmoChanged(weapon, currentMagazineAmmo);
    }

    #region Event Handlers

    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        
        if (enableDebugLogs)
            Debug.Log($"Handling weapon fired - consuming ammo");
            
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
        
        if (enableDebugLogs)
            Debug.Log("Reload input detected");
            
        StartReload();
    }
    
    #endregion
}