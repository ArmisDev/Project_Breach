namespace NL
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class FPSController : MonoBehaviour
    {
        [Header("MOVEMENT")]
        public bool canMove = true;

        public float walkSpeed = 3;
        public float runSpeed = 6;
        public float jumpPower = 1.5f;
        public float gravity = 10;

        [Header("LOOK")]
        public float camLookSens = 2;
        public float camLookXLimit = 85;
        public float smoothTime = 25f;

        [Header("REFERENCES")]
        public Transform cameraTransform;
        public Transform raycastSource;
        public CharacterController charController;
        private PlayerInput playerInput;

        [HideInInspector] public float camLookSensMultiplier = 1;
        public static float speedMultiplier = 1;
        private Quaternion characterTargetRot;
        private Quaternion cameraTargetRot;
        private Vector3 moveDir = Vector3.zero;
        private bool jumpInputPressed;

        [HideInInspector] public float normalizedSpeed;

        public UnityAction OnJump;

        public static FPSController instance;

        // Start is called before the first frame update
        void Awake()
        {
            instance = this;
            charController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            speedMultiplier = 1;
        }

        private void OnEnable()
        {
            playerInput.OnJumpPressed += OnJumpInput;
        }

        private void OnDisable()
        {
            playerInput.OnJumpPressed -= OnJumpInput;
        }

        

        private void Start()
        {
            cameraTargetRot = cameraTransform.localRotation;
        }

        public void SetInitialRotation(Quaternion characterRotation, Quaternion cameraRotation)
        {
            characterTargetRot = characterRotation;
        }

        void Update()
        {
            Movement();
            LookRotation();
            GetNormalizedSpeed();
        }

        private void GetNormalizedSpeed()
        {
            if (charController.isGrounded)
                normalizedSpeed = Mathf.InverseLerp(0, runSpeed, Mathf.Clamp(charController.velocity.magnitude, 0, runSpeed));
            else
                normalizedSpeed = 0;
        }

        private void OnJumpInput()
        {
            jumpInputPressed = true;
        }

        private void Movement()
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            float curSpeed = (playerInput.tacticalWalk ? walkSpeed : runSpeed) * speedMultiplier;

            float curSpeedZ = canMove ? curSpeed * playerInput.vertical : 0;
            float curSpeedX = canMove ? curSpeed * playerInput.horizontal : 0;
            float moveDirY = moveDir.y;

            moveDir = (forward * curSpeedZ) + (right * curSpeedX);

            if (jumpInputPressed && canMove && charController.isGrounded)
            {
                moveDir.y = jumpPower;
                OnJump?.Invoke();
            }
            else
                moveDir.y = moveDirY;

            if (!charController.isGrounded)
                moveDir.y -= gravity * Time.deltaTime;

            charController.Move(moveDir * Time.deltaTime);

            jumpInputPressed = false;
        }

        Quaternion nanRot = new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN);
        public void LookRotation()
        {
            characterTargetRot *= Quaternion.Euler(0f, playerInput.yRot * camLookSens * camLookSensMultiplier, 0f);
            cameraTargetRot *= Quaternion.Euler(-playerInput.xRot * camLookSens * camLookSensMultiplier, 0f, 0f);

            characterTargetRot = characterTargetRot.normalized;
            cameraTargetRot = cameraTargetRot.normalized;

            cameraTargetRot = ClampRotationAroundXAxis(cameraTargetRot);

            if (characterTargetRot != nanRot)
                transform.localRotation = characterTargetRot;
            if (cameraTargetRot != nanRot)
                cameraTransform.localRotation = cameraTargetRot;
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, -camLookXLimit, camLookXLimit);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }
    }
}
