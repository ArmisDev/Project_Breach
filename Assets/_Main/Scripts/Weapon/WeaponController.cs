using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(PlayerInput))]
public class WeaponController : MonoBehaviour
{
    [Header("Weapon GameObjects")]
    [SerializeField] private Transform weaponCameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject fireFX;
    private Weapon weapon;

    [Header("Weapon Recoil Parameters")]
    [SerializeField] private float recoilDecay = 5f;
    [SerializeField] private float recoilMultiplier = 2f;
    [SerializeField] private float maxRecoil = 20f; // Maximum vertical recoil angle
    [SerializeField] private float recoilRandomness = 0.1f; // Horizontal randomness
    private Vector3 firstShotPosition;
    private Vector3 currentRecoil;
    private bool hasShot = false;
    private Quaternion originalRotation;

    // Input
    private PlayerInput input;
    private InputAction fireInput;
    private InputAction reloadInput;

    [Header("Weapon Parameters")]
    // Fire rates
    [SerializeField] private float fireRate = 0.1f; // Time between shots in seconds
    [SerializeField] private int burstCount = 0;
    [SerializeField] private int burstSize = 3;
    private bool isFiring = false;
    private float lastFireTime;


    [Header("Reload")]
    [SerializeField] private float reloadTime;
    private bool isReloading = false;
    [SerializeField] private AudioClip clipIn;
    [SerializeField] private AudioClip clipOut;
    [SerializeField] private AudioClip slideBack;
    [SerializeField] private AudioClip slidePull;
    [SerializeField] private AudioClip slideRelease;

    // Fire Type
    [SerializeField] private WeaponFireType weaponFireType;
    private enum WeaponFireType
    {
        semi_auto,
        burst,
        full_auto
    }

    [Header("Ammo")]
    public int magSize = 30;
    public int currentAmmo = 0;
    public int totalAmmo = 0;

    public event Action OnOutOfAmmo;
    public event Action OnWeaponFired;
    public event Action OnWeaponReload;

    [Header("Weapon Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip equipSound;



    void Awake()
    {
        weapon = GetComponent<Weapon>();
        input = GetComponent<PlayerInput>();
        fireInput = input.actions["Fire"];
        reloadInput = input.actions["Reload"];

        // Store original rotation
        originalRotation = weaponCameraTransform.localRotation;
        fireFX.SetActive(false);
    }

    private void Update()
    {
        if (!isReloading)
        {
            WeaponFire();
        }

        HandleRecoilRecovery();

        if (reloadInput.WasPerformedThisFrame() && totalAmmo != 0)
        {
            Debug.Log("Reload");
            HandleWeaponReload();
        }
    }

    public void WeaponFire()
    {
        // Determine if we can fire based on weapon type
        bool canFire = false;

        switch (weaponFireType)
        {
            case WeaponFireType.semi_auto:
                // One shot per click
                if (fireInput.WasPerformedThisFrame())
                {
                    canFire = true;
                }
                break;

            case WeaponFireType.full_auto:
                // Continuous firing while button is held
                if (fireInput.IsPressed() && Time.time > lastFireTime + fireRate)
                {
                    canFire = true;
                }
                break;

            case WeaponFireType.burst:
                // Only start a new burst if we're not already in the middle of one
                if (fireInput.WasPerformedThisFrame() && !isFiring)
                {
                    burstCount = 0;
                    isFiring = true;
                }

                if (isFiring && Time.time > lastFireTime + fireRate)
                {
                    canFire = true;

                    // Check if we've finished the burst
                    if (++burstCount >= burstSize)
                    {
                        isFiring = false;
                    }
                }
                break;
        }

        if (canFire)
        {
            // Check weapon ammo to make sure weapon can be fired
            if (currentAmmo == 0)
            {
                OnOutOfAmmo?.Invoke();
                return;
            }

            // Fire the weapon
            lastFireTime = Time.time;

            // Log the position of the first shot
            if (!hasShot)
            {
                firstShotPosition = playerCamera.transform.position;
                hasShot = true;
            }

            // Calculate and apply recoil for this shot
            CalculateAndApplyRecoil();
            HandleWeaponAudio();
            HandleWeaponFired();
            // Additional weapon firing effects would go here
            // (muzzle flash, sound, projectile instantiation, etc.)
        }
    }

    private void CalculateAndApplyRecoil()
    {
        // Calculate vector from shot position to screen center (in world space)
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        Vector3 worldCenter = playerCamera.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, 10f));
        Vector3 recoilVector = (worldCenter - firstShotPosition).normalized;

        // Calculate recoil amount for this shot
        float verticalRecoil = recoilVector.y * recoilMultiplier;

        // Add some randomness for horizontal recoil (more realistic)
        float horizontalRecoil = UnityEngine.Random.Range(-recoilRandomness, recoilRandomness);

        // Add recoil to current recoil (primary vertical, minor horizontal)
        // Negative pitch for upward recoil, small random yaw for horizontal variance
        currentRecoil += new Vector3(-Mathf.Abs(verticalRecoil), horizontalRecoil, 0);

        // Clamp vertical recoil to prevent excessive recoil
        currentRecoil.x = Mathf.Clamp(currentRecoil.x, -maxRecoil, 0);

        // Apply the recoil effect
        ApplyRecoil();
    }

    private void ApplyRecoil()
    {
        // Apply the current recoil to the weapon transform
        weaponCameraTransform.localRotation = originalRotation * Quaternion.Euler(currentRecoil);
    }

    private void HandleRecoilRecovery()
    {
        // If we have recoil, gradually reduce it
        if (currentRecoil.magnitude > 0.01f)
        {
            currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoilDecay * Time.deltaTime);
            ApplyRecoil();
        }
        else if (hasShot && !isFiring && !fireInput.IsPressed())
        {
            // Reset shot tracking when recoil is recovered and not firing
            hasShot = false;
            currentRecoil = Vector3.zero;
            ApplyRecoil();
        }
    }

    private void HandleWeaponAudio()
    {
        audioSource.PlayOneShot(fireSound);
    }

    private void HandleWeaponFired()
    {
        currentAmmo--;
        OnWeaponFired?.Invoke();
    }

    public void PlayFX()
    {
        fireFX.SetActive(true);
    }

    public void StopFX()
    {
        fireFX.SetActive(false);
    }

    private void HandleWeaponReload()
    {
        if (!isReloading)
        {
            OnWeaponReload?.Invoke();
            StartCoroutine(ReloadTime());
        }
    }

    IEnumerator ReloadTime()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime); ;
        if (totalAmmo < magSize)
        {
            currentAmmo = totalAmmo;
        }

        else
        {
            currentAmmo = magSize;
        }
        totalAmmo -= currentAmmo;
        isReloading = false;
    }

    public void PlayClipOut()
    {
        audioSource.PlayOneShot(clipOut);
    }

    public void PlayClipIn()
    {
        audioSource.PlayOneShot(clipIn);
    }

    public void PlaySlidePull()
    {
        audioSource.PlayOneShot(slidePull);
    }

    public void PlaySlideRelease()
    {
        audioSource.PlayOneShot(slideRelease);
    }

    public void PlaySlideBack()
    {
        audioSource.PlayOneShot(slideBack);
    }

    public void PlayEquipSound()
    {
        audioSource.PlayOneShot(equipSound);
    }
}