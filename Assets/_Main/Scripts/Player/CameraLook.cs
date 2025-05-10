using UnityEngine;
using UnityEngine.InputSystem;

namespace FPS.Player
{
    public class CameraLook : MonoBehaviour
    {
        [Header("Look Properties")]
        [SerializeField] private float lookSpeedY = 300f;
        [SerializeField] private float lookSpeedX = 150f;
        [SerializeField] private bool invertLook = true;
        [SerializeField] private bool smoothLook = true;
        [SerializeField] private float smoothTime = 0.25f;
        private float lookY;
        private float lookX;
        private Vector2 smoothedInput;
        private Vector2 smoothedInputRef;

        [Header("Components")]
        [SerializeField] private Transform mainTransform;

        [Header("Mouse Lock")]
        [SerializeField] private bool mouseLock = true;

        // Input
        private PlayerInput input;
        private InputAction look;

        [Header("Headbob Settings")]
        [SerializeField] private float walkBobAmplitude = 0.05f;
        [SerializeField] private float walkBobFrequency = 1.5f;
        [SerializeField] private float runBobAmplitude = 0.1f;
        [SerializeField] private float runBobFrequency = 2.5f;
        [SerializeField] private Transform playerCamera;

        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float bobTimer = 0f;

        [Header("Headbob Rotation")]
        [SerializeField] private float bobRotationMultiplier = 5f;

        private float currentBobAngle = 0f;
        private float bobAngleVelocity = 0f;

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            input = GetComponentInParent<PlayerInput>();
            look = input.actions["Look"];
        }

        private void Start()
        {
            originalCameraPosition = playerCamera.localPosition;
            originalCameraRotation = playerCamera.localRotation;
        }

        void MouseLock()
        {
            if (mouseLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Cursor.visible = !mouseLock;
        }

        void HandleHeadbob()
        {
            Vector3 velocity = playerController.characterController.velocity;
            Vector3 localVelocity = playerController.transform.InverseTransformDirection(velocity);
            float xAxisSpeed = localVelocity.x;
            float forwardSpeed = localVelocity.z;
            float totalSpeed = new Vector3(localVelocity.x, 0, localVelocity.z).magnitude;

            if (playerController.IsGrounded() && totalSpeed > 0.1f)
            {
                float amplitude = playerController.sprint.IsInProgress() ? runBobAmplitude : walkBobAmplitude;
                float frequency = playerController.sprint.IsInProgress() ? runBobFrequency : walkBobFrequency;

                bobTimer += Time.deltaTime * frequency;

                // Calculate vertical bobbing
                float verticalBob = Mathf.Sin(bobTimer) * amplitude;

                // Apply vertical bobbing to camera position
                Vector3 bobPosition = originalCameraPosition + transform.up * verticalBob;
                playerCamera.localPosition = bobPosition;

                // Apply camera rotation only when moving left or right
                if (Mathf.Abs(xAxisSpeed) > 0.1f)
                {
                    // Normalize xAxisSpeed by move speed
                    float xAxisInfluence = xAxisSpeed / playerController.moveSpeed;

                    // Clamp xAxisInfluence to [-1, 1]
                    xAxisInfluence = Mathf.Clamp(xAxisInfluence, -1f, 1f);

                    // Smoothly update the bob angle based on sideways movement
                    float targetBobAngle = -xAxisInfluence * bobRotationMultiplier;
                    currentBobAngle = Mathf.SmoothDampAngle(currentBobAngle, targetBobAngle, ref bobAngleVelocity, 0.1f);
                }
                else
                {
                    // Smoothly reset the bob angle to 0 when not moving sideways
                    currentBobAngle = Mathf.SmoothDampAngle(currentBobAngle, 0f, ref bobAngleVelocity, 0.1f);
                }

                // Apply smoothed rotation to the camera
                playerCamera.localRotation = Quaternion.Euler(0f, 0f, currentBobAngle) * originalCameraRotation;
            }
            else
            {
                // Reset bobbing when not moving
                bobTimer = 0f;
                playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, originalCameraPosition, Time.deltaTime * 5f);

                // Smoothly reset rotation to the original state
                currentBobAngle = Mathf.SmoothDampAngle(currentBobAngle, 0f, ref bobAngleVelocity, 0.1f);
                playerCamera.localRotation = Quaternion.Slerp(playerCamera.localRotation, originalCameraRotation, Time.deltaTime * 5f);
            }
        }

        void Update()
        {
            MouseLock();
            HandleHeadbob();

            // Input Read
            Vector2 mouseInput = look.ReadValue<Vector2>();

            if (smoothLook)
            {
                // Smooth the input
                smoothedInput = Vector2.SmoothDamp(smoothedInput, mouseInput, ref smoothedInputRef, smoothTime);

                // Yaw (Horizontal Look)
                lookY += Time.deltaTime * smoothedInput.x * lookSpeedX;

                // Pitch (Vertical Look)
                lookX += invertLook ? Time.deltaTime * -smoothedInput.y * lookSpeedY : Time.deltaTime * smoothedInput.y * lookSpeedY;
                lookX = Mathf.Clamp(lookX, -90, 90);

                // Apply Rotations
                transform.localEulerAngles = new Vector3(lookX, 0, 0);
                mainTransform.localRotation = Quaternion.Euler(0, lookY, 0);
            }
            else
            {
                // Yaw (Horizontal Look)
                lookY += Time.deltaTime * mouseInput.x * lookSpeedX;

                // Pitch (Vertical Look)
                lookX += invertLook ? Time.deltaTime * -mouseInput.y * lookSpeedY : Time.deltaTime * mouseInput.y * lookSpeedY;
                lookX = Mathf.Clamp(lookX, -90, 90);

                // Apply Rotations
                transform.localEulerAngles = new Vector3(lookX, 0, 0);
                mainTransform.localRotation = Quaternion.Euler(0, lookY, 0);
            }
        }
    }
}
