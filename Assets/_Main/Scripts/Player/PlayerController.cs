using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FPS.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Movement")]
        public float moveSpeed = 2.5f;
        [SerializeField] private float speedMult = 1.75f;
        [HideInInspector] public Vector3 playerVelocity;

        [Header("Player Jump")]
        [SerializeField] private float jumpForce = 0.75f;
        [SerializeField] private float gravity = -13f;
        private float jumpVelocity = 0f;
        public UnityAction OnJump;
        private float jumpStartTime;
        private float airTime;
        private bool isJumping = false;
        public event Action OnLanded;
        [HideInInspector] public float fallTime;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private float sphereRadius = 0.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private LayerMask groundLayer;
        private float lastGroundedTime;

        [Header("Inventory Integration")]
        public InventoryManager inventoryManager;
        public InventoryUI inventoryUI;
        [SerializeField] private bool isInventoryOpen = false;

        // Core Components
        [HideInInspector] public CharacterController characterController;
        public Transform raycastSource;
        private CameraLook cameraLook;

        // Input
        private PlayerInput input;
        private InputAction move;
        [HideInInspector] public InputAction sprint;
        private InputAction jump;
        private InputAction inventory;
        private InputAction[] quickSlotActions;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            cameraLook = GetComponentInChildren<CameraLook>();
            input = GetComponent<PlayerInput>();
            
            // Setup input actions
            move = input.actions["Move"];
            sprint = input.actions["Sprint"];
            jump = input.actions["Jump"];
            inventory = input.actions["Inventory"];
            
            // Setup quick slot actions (1-9)
            quickSlotActions = new InputAction[9];
            for (int i = 0; i < 9; i++)
            {
                quickSlotActions[i] = input.actions[$"Quickslot{i + 1}"];
            }
        }

        private void OnEnable()
        {
            input.actions["Inventory"].performed += OnInventoryToggle;
            
            // Subscribe to quick slot inputs
            for (int i = 0; i < quickSlotActions.Length; i++)
            {
                int index = i; // Capture variable for closure
                quickSlotActions[i].performed += ctx => OnQuickSlot(index);
            }
        }

        private void OnDisable()
        {
            input.actions["Inventory"].performed -= OnInventoryToggle;
            
            // Unsubscribe from quick slot inputs
            for (int i = 0; i < quickSlotActions.Length; i++)
            {
                quickSlotActions[i].performed -= ctx => OnQuickSlot(i);
            }
        }

        void MovementLogic()
        {
            // Don't move if inventory is open
            if (isInventoryOpen) return;

            // Get movement input
            Vector2 initMove = move.ReadValue<Vector2>();

            // Handle sprinting
            float currentSpeed = sprint.IsInProgress() ? moveSpeed * speedMult : moveSpeed;

            // Calculate movement direction
            Vector3 moveDirection = (transform.right * initMove.x + transform.forward * initMove.y).normalized;

            // Apply movement speed
            Vector3 horizontalVelocity = moveDirection * currentSpeed;

            // Apply gravity
            if (IsGrounded() && jumpVelocity < 0)
            {
                jumpVelocity = -2f;

                if (isJumping)
                {
                    OnLanded?.Invoke();
                    airTime = Time.time - jumpStartTime;
                    isJumping = false;
                }
            }

            jumpVelocity += gravity * Time.deltaTime;

            // Handle jumping (only if not in inventory)
            if (jump.WasPerformedThisFrame() && !isInventoryOpen && IsGrounded())
            {
                Jump();
            }

            // Combine horizontal and vertical velocities
            playerVelocity = horizontalVelocity;
            playerVelocity.y = jumpVelocity;

            // Move the character
            characterController.Move(playerVelocity * Time.deltaTime);
        }

        void Jump()
        {
            jumpVelocity = Mathf.Sqrt(2f * -gravity * jumpForce);
            jumpStartTime = Time.time;
            isJumping = true;
            OnJump?.Invoke();
        }

        public bool IsGrounded()
        {
            bool isGrounded = Physics.SphereCast(
                transform.position,
                sphereRadius,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer
            );

            if (!isGrounded)
            {
                isGrounded = Physics.SphereCast(
                    transform.position + Vector3.up * 0.01f,
                    sphereRadius,
                    Vector3.down,
                    out hit,
                    groundCheckDistance + 0.01f,
                    groundLayer
                );
            }

            return isGrounded;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, sphereRadius);
            Vector3 endPoint = transform.position + Vector3.down * groundCheckDistance;
            Gizmos.DrawWireSphere(endPoint, sphereRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, endPoint);
        }

        void Update()
        {
            MovementLogic();

            if (IsGrounded())
            {
                lastGroundedTime = Time.time;
            }

            if (jump.WasPerformedThisFrame() &&
                (IsGrounded() || Time.time - lastGroundedTime < coyoteTime))
            {
                Jump();
            }

            if(!IsGrounded())
            {
                fallTime += Time.deltaTime;
            }
            else
            {
                fallTime = 0;
            }
        }

        #region Inventory Integration

        private void OnInventoryToggle(InputAction.CallbackContext context)
        {
            ToggleInventory();
        }

        private void ToggleInventory()
        {
            isInventoryOpen = !isInventoryOpen;
            SetInventoryState(isInventoryOpen);
        }

        public void SetInventoryState(bool open)
        {
            isInventoryOpen = open;
            
            // Show/hide inventory UI
            if (inventoryUI != null)
            {
                inventoryUI.gameObject.SetActive(open);
            }
            
            // Control player movement and camera
            SetPlayerControls(!open);
            
            // Cursor management
            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SetPlayerControls(bool enabled)
        {
            // Enable/disable movement
            characterController.enabled = enabled;
            
            // Enable/disable camera look
            if (cameraLook != null)
            {
                cameraLook.enabled = enabled;
            }
            
            // Optionally pause time or other game systems
            if (!enabled)
            {
                // Time.timeScale = 0f; // Uncomment to pause game
            }
            else
            {
                // Time.timeScale = 1f; // Uncomment to resume game
            }
        }

        private void OnQuickSlot(int slotIndex)
        {
            // Don't use quick slots while inventory is open
            if (isInventoryOpen) return;
            
            if (inventoryManager != null)
            {
                inventoryManager.UseQuickSlot(slotIndex);
            }
        }

        // Public method for other systems to open/close inventory
        public void OpenInventory()
        {
            if (!isInventoryOpen)
            {
                ToggleInventory();
            }
        }

        public void CloseInventory()
        {
            if (isInventoryOpen)
            {
                ToggleInventory();
            }
        }

        // Check if inventory is open (useful for other systems)
        public bool IsInventoryOpen()
        {
            return isInventoryOpen;
        }

        #endregion
    }
}