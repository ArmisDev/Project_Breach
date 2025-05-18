namespace NL
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable]
    public enum FireMode
    {
        Manual,
        SemiAuto,
        Auto
    }

    public class Firearm : WeaponBase
    {
        [Header("REFERENCES")]
        public Transform visualShootSource;
        private ParticleSystem muzzleParticleSystem;
        private AudioSource sfxSource;
        private Light muzzleFlashLight;

        private FPSController playerController;
        private PlayerInput playerInput;
        private Transform raycastShootSource;
        private Animator animator;

        [Header("SETTINGS")]
        [SerializeField] private FireMode fireMode = FireMode.Auto;

        [Tooltip("The time the weapon will not shoot right after deployment.")]
        [SerializeField]
        private float deployTime = 0.3f;

        [Tooltip("Rate of fire in rounds per minute.")]
        [SerializeField] private int fireRate = 600;

        [SerializeField] private float manualFireModeInterval = 0.5f;

        [Tooltip("Used for shotguns to emitate the grapeshot effect.")]
        [SerializeField] private float grapeshotSpray = 0;

        [Tooltip("If true, the weapon will start reloading when the bullets in mag is over.")]
        [SerializeField] private bool autoReload = true;
        [Tooltip("Can the reload process be interrupted. Usefull for shotguns.")]
        [SerializeField] private bool interruptibleReload = false;
        [Tooltip("Reload time when the weapon is empty, i.e. no bullets in mag is left. Set it equal or greater than the animation length.")]
        [SerializeField] private float reloadTimeEmpty = 2;
        [Tooltip("Reload time when the weapon has some bullets in mag. Set it equal or greater than the animation length.")]
        [SerializeField] private float reloadTimeFull = 1;

        [SerializeField] private int magCapacity = 30;
        [SerializeField] private int bulletsCountTotal = 120;
        [SerializeField] private int maxBulletsTotal = 120;

        [SerializeField] private float muzzleFlashSize = 0.5f;
        [SerializeField] private float muzzleFlashLightTime = 0.2f;

        [Tooltip("Maximum distance for the raycast hit.")]
        [SerializeField] private float maxShootDistance = 100;
        [SerializeField] private LayerMask hittableLayers = ~0;

        [Header("SFX")]
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip emptyShootClip;
        [SerializeField] private AudioClip[] reloadSequnceSFXs;

        private Vector3 shootTargetPos;

        private bool canShoot = true;
        [HideInInspector] public float damageMultiplier = 1;
        private ParticleSystem.MainModule muzzlePsMainModule;

        private Coroutine shootOnceRoutine;
        private Coroutine reloadRoutine;
        private Coroutine emptyShootRoutine;
        private Coroutine muzzleFlashLightRoutine;

        private float fireInterval => 60f / fireRate; // calculate fire interval in seconds from rounds/minute
        private WaitForSeconds waitForNextShoot;
        private WaitForSeconds waitForReloadEmpty;
        private WaitForSeconds waitForReloadFull;

        private int magBulletsCount;
        private int stockBullets;
        private int magazineCapacity;
        private int maxBulletsInStock;

        private float muzzleFlashLightSpeed => 1 / muzzleFlashLightTime;
        private float muzzleFlashLightIntensity;

        private WaitForSeconds waitForDeploy => new WaitForSeconds(deployTime);

        void Awake()
        {
            InitWeapon();
        }

        public void InitWeapon()
        {
            if (fireMode == FireMode.Manual)
                waitForNextShoot = new WaitForSeconds(manualFireModeInterval);
            else if (fireMode == FireMode.SemiAuto || fireMode == FireMode.Auto)
                waitForNextShoot = new WaitForSeconds(fireInterval);

            RefreshBulletsData();

            waitForReloadEmpty = new WaitForSeconds(reloadTimeEmpty);
            waitForReloadFull = new WaitForSeconds(reloadTimeFull);

            muzzleParticleSystem = visualShootSource.GetComponent<ParticleSystem>();
            sfxSource = visualShootSource.GetComponent<AudioSource>();
            if (muzzleParticleSystem != null) muzzlePsMainModule = muzzleParticleSystem.main;
            muzzleFlashLight = visualShootSource.GetComponent<Light>();
            muzzleFlashLightIntensity = muzzleFlashLight.intensity;
            muzzleFlashLight.intensity = 0;

            animator = GetComponent<Animator>();
            playerController = GetComponentInParent<FPSController>();
            playerInput = GetComponentInParent<PlayerInput>();

            raycastShootSource = playerController.raycastSource;
        }

        public void RefreshBulletsData()
        {
            magazineCapacity = magCapacity;
            maxBulletsInStock = maxBulletsTotal;

            magBulletsCount = magazineCapacity;
            stockBullets = bulletsCountTotal;
        }

        private void OnEnable()
        {
            reloadRoutine = null;
            shootOnceRoutine = null;
            canShoot = false;

            StartCoroutine(Deploy());

            if (playerController != null && playerInput != null)
            {
                animator.SetFloat("IsEmpty", magBulletsCount == 0 ? 1 : 0);
                playerInput.OnWeaponUseStarted += Use;
                playerInput.OnWeaponReloadPressed += TryReload;
                PlayerInput.OnWeaponInspectPressed += OnInspectPressed;
                playerController.OnJump += OnJump;
            }
        }

        private IEnumerator Deploy()
        {
            yield return null;
            yield return waitForDeploy;
            canShoot = true;
        }

        private void OnDisable()
        {
            muzzleFlashLight.intensity = 0;
            StopAllCoroutines();
            shootOnceRoutine = null;

            if (playerController != null && playerInput != null)
            {
                playerInput.OnWeaponUseStarted -= Use;
                playerInput.OnWeaponReloadPressed -= TryReload;
                PlayerInput.OnWeaponInspectPressed -= OnInspectPressed;
                playerController.OnJump -= OnJump;
            }
        }

        public bool AddAmmo(int count)
        {
            int prevAmmo = stockBullets;
            stockBullets = Mathf.Clamp(stockBullets + count, stockBullets, maxBulletsInStock);

            if (stockBullets > prevAmmo)
            {
                //Update HUD here
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Update()
        {
            if (playerInput != null)
            {
                if (fireMode == FireMode.Auto)
                {
                    if (playerInput.weaponUseHold) Use();
                }
            }

            PassPlayerSpeedToAnimator();
        }

        private float curPlayerSpeed;
        private float refVel;
        private void PassPlayerSpeedToAnimator()
        {
            curPlayerSpeed = Mathf.SmoothDamp(curPlayerSpeed, playerController.normalizedSpeed, ref refVel, 0.15f);
            animator.SetFloat("PlayerSpeed", curPlayerSpeed);
        }

        private void OnInspectPressed()
        {
            if (shootOnceRoutine != null) return;

            animator.SetTrigger("Inspect");
        }

        private void OnJump()
        {
            if (reloadRoutine != null) return;

            animator.SetTrigger("Jump");
        }

        public void Use()
        {
            if (magBulletsCount > 0)
            {
                if (canShoot) Shoot();
            }
            else if (autoReload)
            {
                if (emptyShootClip != null && reloadRoutine == null)
                {
                    if (emptyShootRoutine == null)
                    {
                        emptyShootRoutine = StartCoroutine(EmptyShoot());
                    }
                }

                TryReload();
            }
        }

        private IEnumerator EmptyShoot()
        {
            if (emptyShootClip != null)
                sfxSource.PlayOneShot(emptyShootClip);
            else
                Debug.LogWarning($"Empty Shoot Clip is not provided for {gameObject.name}. Empty Shoot sound will not be played.");

            yield return new WaitForSeconds(fireInterval * 4);

            emptyShootRoutine = null;
        }

        private void TryReload()
        {
            if (stockBullets > 0 && magBulletsCount != magazineCapacity && shootOnceRoutine == null)
                Reload();
        }

        public void Reload()
        {
            if (reloadRoutine == null)
            {
                muzzleFlashLight.intensity = 0;
                StopAllCoroutines();

                canShoot = false;
                reloadRoutine = StartCoroutine(ReloadWeapon());
            }
        }

        private void Shoot()
        {
            if (reloadRoutine != null && interruptibleReload)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
                animator.SetTrigger("Locomotion");
                canShoot = true;

                return;
            }
            else if (shootOnceRoutine == null)
                shootOnceRoutine = StartCoroutine(ShootOnce());
        }

        private IEnumerator ShootOnce()
        {
            magBulletsCount--;

            if (magBulletsCount == 0)
            {
                animator.SetFloat("IsEmpty", 1);
            }

            if (fireMode == FireMode.SemiAuto || fireMode == FireMode.Auto)
                animator.SetTrigger("Shoot");
            else if (fireMode == FireMode.Manual && magBulletsCount > 0)
            {
                animator.SetTrigger("BoltJerk");
                canShoot = false;
            }

            canShoot = false;

            //TODO: update HUD here

            PlayShootSFX();
            DoMuzzleFlash(muzzleFlashSize);

            //TODO: procedural recoil here

            Vector3 dir = GetShootDir();

            bool isHit = false;
            RaycastHit hit;

            if (isHit = Physics.Raycast(raycastShootSource.position, dir, out hit, maxShootDistance, hittableLayers, QueryTriggerInteraction.Ignore))
            {
                shootTargetPos = hit.point;
            }
            else
            {
                shootTargetPos = raycastShootSource.position + raycastShootSource.forward * maxShootDistance;
            }

            //TODO: hit logic here

            if (magBulletsCount > 0)
            {
                yield return waitForNextShoot;
                canShoot = true;
            }
            else
            {
                yield return null;
            }

            shootOnceRoutine = null;
        }

        private void DoMuzzleFlash(float size)
        {
            if (muzzleParticleSystem == null)
                return;

            muzzlePsMainModule.startSizeMultiplier = size;
            muzzleParticleSystem.Emit(1);

            if (muzzleFlashLightRoutine != null) StopCoroutine(muzzleFlashLightRoutine);
            muzzleFlashLightRoutine = StartCoroutine(MuzzleFlashLightAnim());
        }

        private IEnumerator MuzzleFlashLightAnim()
        {
            float t = 1;

            muzzleFlashLight.intensity = muzzleFlashLightIntensity;

            while (t > 0)
            {
                t -= Time.deltaTime * muzzleFlashLightSpeed;
                muzzleFlashLight.intensity *= t;
                yield return null;
            }

            muzzleFlashLightRoutine = null;
        }

        //For shotguns
        private int GetEmptyBulletSlots()
        {
            return magazineCapacity - magBulletsCount;
        }

        //called from the animation event
        public void OneRoundInsert()
        {
            if (stockBullets > 0)
            {
                stockBullets--;
                magBulletsCount++;
                //TODO: update HUD here
            }
            else
            {
                if (reloadRoutine != null) StopCoroutine(reloadRoutine);
                reloadRoutine = null;

                animator.SetTrigger("Locomotion");

                canShoot = true;

                return;
            }

            if (GetEmptyBulletSlots() == 0)
            {
                if (reloadRoutine != null) StopCoroutine(reloadRoutine);
                reloadRoutine = null;

                animator.SetTrigger("Locomotion");
            }

            canShoot = true;
        }

        private IEnumerator ReloadWeapon()
        {
            animator.SetTrigger("Reload");

            //TODO: single clip reload SFX here

            if (interruptibleReload)
            {
                //wait for at least one round insert
                yield return waitForReloadFull;
            }
            else
            {
                if (magBulletsCount == 0)
                {
                    yield return waitForReloadEmpty;
                }
                else
                {
                    yield return waitForReloadFull;
                }

                animator.SetFloat("IsEmpty", 0);

                int emptyBulletSlots = magazineCapacity - magBulletsCount;

                if (stockBullets >= emptyBulletSlots)
                {
                    stockBullets -= emptyBulletSlots;
                    magBulletsCount = magazineCapacity;
                }
                else
                {
                    magBulletsCount += stockBullets;
                    stockBullets = 0;
                }

                //TODO: update HUD here

                canShoot = true;
                reloadRoutine = null;
            }
        }

        private Vector3 GetShootDir()
        {
            Vector3 dir = raycastShootSource.forward;

            //TODO: add extra logic here, such as greater bullets spray when player moves

            return dir;
        }

        private Vector3 GetGrapeshotSpray()
        {
            Vector3 dir = raycastShootSource.forward * 2;

            dir += UnityEngine.Random.onUnitSphere * grapeshotSpray;

            return dir.normalized;
        }

        private void PlayShootSFX()
        {
            if (shootClip != null)
                sfxSource.PlayOneShot(shootClip);
        }

        public void PlayReloadSFX()
        {
            //TODO: single clip reload sfx here
        }

        //Called from the animation events
        public void PlaySFX(int id)
        {
            if (reloadSequnceSFXs == null || reloadSequnceSFXs.Length == 0 || reloadSequnceSFXs[id] == null)
                return;

            sfxSource.PlayOneShot(reloadSequnceSFXs[id]);
        }
    }
}
