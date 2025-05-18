using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Interface for weapon components
public interface IWeaponComponent
{
    void Initialize(WeaponBase weapon);
    void Tick();
    void OnWeaponEquipped();
    void OnWeaponUnequipped();
}

public class WeaponBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected WeaponDataSO weaponData;
    [SerializeField] protected WeaponEventsSO weaponEvents;
    [SerializeField] protected Transform weaponTransform;
    
    // Current state
    protected WeaponState currentState = WeaponState.Disabled;
    protected List<IWeaponComponent> components = new List<IWeaponComponent>();
    
    // Properties
    public WeaponDataSO WeaponData => weaponData;
    public WeaponEventsSO WeaponEvents => weaponEvents;
    public WeaponState CurrentState => currentState;
    public Transform WeaponTransform => weaponTransform;
    
    protected virtual void Awake()
    {
        // Find and initialize all weapon components
        GetComponents(components);
        foreach (var component in components)
        {
            component.Initialize(this);
        }
    }
    
    protected virtual void OnEnable()
    {
        // Subscribe to events
        if (weaponEvents != null)
        {
            weaponEvents.OnFireInputChanged += HandleFireInput;
            weaponEvents.OnReloadInputPressed += HandleReloadInput;
            weaponEvents.OnWeaponStateChanged += HandleStateChanged;
        }
    }
    
    protected virtual void OnDisable()
    {
        // Unsubscribe from events
        if (weaponEvents != null)
        {
            weaponEvents.OnFireInputChanged -= HandleFireInput;
            weaponEvents.OnReloadInputPressed -= HandleReloadInput;
            weaponEvents.OnWeaponStateChanged -= HandleStateChanged;
        }
    }
    
    protected virtual void Update()
    {
        // Update all components
        foreach (var component in components)
        {
            component.Tick();
        }
    }
    
    public void ChangeState(WeaponState newState)
    {
        if (newState == currentState) return;
        
        WeaponState oldState = currentState;
        currentState = newState;
        
        OnStateChanged(oldState, newState);
        weaponEvents?.RaiseWeaponStateChanged(this, newState);
    }
    
    protected virtual void OnStateChanged(WeaponState oldState, WeaponState newState)
    {
        // Override in derived classes
    }
    
    public void Equip()
    {
        foreach (var component in components)
        {
            component.OnWeaponEquipped();
        }
        ChangeState(WeaponState.Ready);
        weaponEvents?.RaiseWeaponEquipped(this);
    }
    
    public void Unequip()
    {
        foreach (var component in components)
        {
            component.OnWeaponUnequipped();
        }
        ChangeState(WeaponState.Disabled);
        weaponEvents?.RaiseWeaponUnequipped(this);
    }
    
    protected virtual void HandleFireInput(WeaponBase weapon, bool isPressed)
    {
        // Only respond to events for this weapon
        if (weapon != this) return;
        
        // Can be implemented in derived classes
    }
    
    protected virtual void HandleReloadInput(WeaponBase weapon)
    {
        // Only respond to events for this weapon
        if (weapon != this) return;
        
        // Can be implemented in derived classes
    }
    
    protected virtual void HandleStateChanged(WeaponBase weapon, WeaponState newState)
    {
        // Only respond to events for this weapon
        if (weapon != this) return;
        
        // Can be implemented in derived classes
    }
}