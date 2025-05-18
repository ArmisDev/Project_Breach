namespace NL { 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeThrower : WeaponBase
{
    [Header("REFERENCES")]
    public GrenadeBase grenadePrefab;
    public Transform throwSourcePrimary;
    public Transform throwSourceSecondary;
    private Animator animator;
    private AudioSource sfxSource;

    [SerializeField] private int grenadesCount = 50;
    [SerializeField] private float recoverTime = 1.5f;
    [SerializeField] private float throwForcePrimary = 25;
    [SerializeField] private float throwForceSecondary = 10;

    [SerializeField] private AudioClip pullPinClip;
    [SerializeField] private AudioClip throwClip;

    private Transform throwSource;
    private FPSController playerController;
    private PlayerInput playerInput;
    private bool canShoot = true;
    private bool pinIsPulled;
    private GrenadeBase grenadeInstance;
    [HideInInspector] public int ammoCount;
    private float throwForce;

    private Coroutine throwRoutine;
    private WaitForSeconds waitForRecoverAfterThrow;
    private WaitForSeconds waitForDeploy = new WaitForSeconds(0.3f);

    void Awake()
    {
        InitWeapon();
    }

    private void OnEnable()
    {
        canShoot = false;
        throwRoutine = null;

        if (playerInput == null) return;

        StartCoroutine(Deploy());

        playerInput.OnWeaponUseFinished += Use;
        playerInput.OnWeaponUseSecondaryFinished += UseSecondary;

        playerController.OnJump += OnJump;
    }

    private IEnumerator Deploy()
    {
        yield return null;
        yield return waitForDeploy;
        canShoot = true;
    }

    private void OnDisable()
    {
        pinIsPulled = false;
        if (playerInput == null) return;

        playerInput.OnWeaponUseFinished -= Use;
        playerInput.OnWeaponUseSecondaryFinished -= UseSecondary;
        playerController.OnJump -= OnJump;
    }

    public void InitWeapon()
    {
        animator = GetComponent<Animator>();
        sfxSource = GetComponent<AudioSource>();

        waitForRecoverAfterThrow = new WaitForSeconds(recoverTime);

        playerController = GetComponentInParent<FPSController>();
        playerInput = GetComponentInParent<PlayerInput>();

        RefreshAmmoData();
    }

    private void OnJump()
    {
        if (throwRoutine != null || !canShoot) return;

        animator.SetTrigger("Jump");
    }

    public void RefreshAmmoData()
    {
        ammoCount = grenadesCount;
    }

    private void Update()
    {
        if (playerInput != null)
        {
            if (playerInput.weaponUseHold || playerInput.weaponUseSecondaryHold) PreUse();
        }

        animator.SetFloat("PlayerSpeed", playerController.normalizedSpeed);
    }

    public void PreUse()
    {
        if (ammoCount > 0)
        {
            if (canShoot && !pinIsPulled)
            {
                if (animator != null) animator.SetTrigger("PullPin");
                pinIsPulled = true;
            }
        }
    }

    public void Use()
    {
        if (!pinIsPulled) return;

        ThrowGrenade(true);
    }

    public void UseSecondary()
    {
        if (!pinIsPulled) return;

        ThrowGrenade(false);
    }

    private void ThrowGrenade(bool high)
    {
        if (high)
        {
            throwSource = throwSourcePrimary;
            throwForce = throwForcePrimary;
            if (animator != null) animator.SetTrigger("ThrowHigh");
        }
        else
        {
            throwSource = throwSourceSecondary;
            throwForce = throwForceSecondary;
            if (animator != null) animator.SetTrigger("ThrowLow");
        }
    }

    public void PlayPullPinSFX()
    {
        if (sfxSource == null || pullPinClip == null) return;

        sfxSource.PlayOneShot(pullPinClip);
    }
    private void PlayThrowSFX()
    {
        if (sfxSource == null || throwClip == null) return;

        sfxSource.PlayOneShot(throwClip);
    }

    /// <summary>
    /// Call from the animation event
    /// </summary>
    public void Throw()
    {
        if (throwRoutine == null && canShoot)
            throwRoutine = StartCoroutine(Throw(throwForce));
    }

    private IEnumerator Throw(float force)
    {
        PlayThrowSFX();

        canShoot = false;

        ammoCount--;
        pinIsPulled = false;

        grenadeInstance = Instantiate(grenadePrefab, throwSource.position, throwSource.rotation);
        //grenadeInstance.InitGrenade(data, thisActor);

        grenadeInstance.rb.AddForce(throwSource.forward * force, ForceMode.Impulse);
        grenadeInstance.rb.AddTorque(Random.onUnitSphere * force * 0.5f, ForceMode.Impulse);

        yield return waitForRecoverAfterThrow;

        throwRoutine = null;
        canShoot = true;
    }
}
}
