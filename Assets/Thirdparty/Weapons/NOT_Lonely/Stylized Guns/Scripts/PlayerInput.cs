namespace NL
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private bool hideCursor = true;

        [HideInInspector] public float vertical;
        [HideInInspector] public float horizontal;
        [HideInInspector] public bool tacticalWalk;
        [HideInInspector] public bool jump;
        [HideInInspector] public float xRot = 0;
        [HideInInspector] public float yRot = 0;

        [HideInInspector] public bool weaponUseHold;
        [HideInInspector] public bool weaponUseSecondaryHold;

        public UnityAction OnJumpPressed;
        public UnityAction OnWeaponUseStarted;
        public UnityAction OnWeaponUseFinished;
        public UnityAction OnWeaponUseSecondaryStarted;
        public UnityAction OnWeaponUseSecondaryFinished;

        public UnityAction OnWeaponReloadPressed;
        public static UnityAction OnWeaponInspectPressed;
        public UnityAction<float> OnWeaponChange;

        private string vAxisName = "Vertical";
        private string hAxisName = "Horizontal";

        private string xMouseAxisName = "Mouse X";
        private string yMouseAxisName = "Mouse Y";

        private string wheelName = "Mouse ScrollWheel";

        public static bool canInput = true;

        private void Start()
        {
            canInput = true;

            if (hideCursor)
                ToggleCursorState(true);
        }

        void Update()
        {
            HandleInput();
        }

        public static void ToggleCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleInput()
        {
            if (!canInput)
            {
                xRot = 0;
                yRot = 0;
                return;
            }

            vertical = Input.GetAxis(vAxisName);
            horizontal = Input.GetAxis(hAxisName);

            if (Input.GetKeyDown(KeyCode.Space)) OnJumpPressed?.Invoke();

            xRot = Input.GetAxis(yMouseAxisName);
            yRot = Input.GetAxis(xMouseAxisName);

            weaponUseHold = Input.GetMouseButton(0);
            weaponUseSecondaryHold = Input.GetMouseButton(1);

            if (Input.GetMouseButtonDown(0))
            {
                if (hideCursor)
                    ToggleCursorState(true);

                OnWeaponUseStarted?.Invoke();
            }

            if (Input.GetMouseButtonDown(1))
            {
                OnWeaponUseSecondaryStarted?.Invoke();
            }

            if (Input.GetMouseButtonUp(1)) OnWeaponUseSecondaryFinished?.Invoke();

            if (Input.GetMouseButtonUp(0)) OnWeaponUseFinished?.Invoke();
            if (Input.GetKeyDown(KeyCode.R)) OnWeaponReloadPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.F)) OnWeaponInspectPressed?.Invoke();

            if (Input.GetAxisRaw(wheelName) > 0f)
                OnWeaponChange?.Invoke(1);
            else if (Input.GetAxisRaw(wheelName) < 0f)
                OnWeaponChange?.Invoke(-1);
        }
    }
}
