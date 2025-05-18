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
        [SerializeField] private float jumpForce = 0.75f; // Desired jump height in meters
        [SerializeField] private float gravity = -13f; // Negative gravity value
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
        [SerializeField] private float coyoteTime = 0.1f; // Time after leaving ground where jump is still allowed
        [SerializeField] private LayerMask groundLayer;
        private float lastGroundedTime;

        // Core Components
        [HideInInspector] public CharacterController characterController;
        public Transform raycastSource;

        // Input
        private PlayerInput input;
        private InputAction move;
        [HideInInspector] public InputAction sprint;
        private InputAction jump;
        
        // Pause state tracking
        private bool isInputPaused = false;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            input = GetComponent<PlayerInput>();
            move = input.actions["Move"];
            sprint = input.actions["Sprint"];
            jump = input.actions["Jump"];
        }
        
        private void OnEnable()
        {
            // Subscribe to global pause events
            PauseManager.OnGamePauseStateChanged += HandlePauseStateChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from global pause events
            PauseManager.OnGamePauseStateChanged -= HandlePauseStateChanged;
        }
        
        private void HandlePauseStateChanged(bool isPaused)
        {
            isInputPaused = isPaused;
            
            // The PlayerInput component will be deactivated by the PauseManager,
            // but this provides an extra layer of protection in our logic
        }

        void MovementLogic()
        {
            // Skip movement logic if paused - extra layer of protection
            if (isInputPaused || PauseManager.IsGamePaused) return;
            
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
                jumpVelocity = -2f; // Small negative value to keep the character grounded

                // If we've just landed after jumping
                if (isJumping)
                {
                    OnLanded?.Invoke();
                    airTime = Time.time - jumpStartTime;
                    isJumping = false;
                }
            }

            // Add gravity (gravity is negative)
            jumpVelocity += gravity * Time.deltaTime;

            // Handle jumping
            if (!isInputPaused && jump.WasPerformedThisFrame() && IsGrounded())
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
            // Don't jump if paused
            if (isInputPaused || PauseManager.IsGamePaused) return;
            
            // Calculate initial jump velocity based on desired jump height
            jumpVelocity = Mathf.Sqrt(2f * -gravity * jumpForce);

            // Record the start of the jump
            jumpStartTime = Time.time;
            isJumping = true;

            OnJump?.Invoke();
        }

        public bool IsGrounded()
        {
            // More precise ground check
            bool isGrounded = Physics.SphereCast(
                transform.position,
                sphereRadius,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer
            );

            // Additional checks for more responsive ground detection
            if (!isGrounded)
            {
                // Slightly offset the sphere cast to handle small terrain variations
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
            // Set the Gizmos color
            Gizmos.color = Color.red;

            // Draw the sphere at the starting point of the SphereCast
            Gizmos.DrawWireSphere(transform.position, sphereRadius);

            // Draw the sphere at the end point of the SphereCast
            Vector3 endPoint = transform.position + Vector3.down * groundCheckDistance;
            Gizmos.DrawWireSphere(endPoint, sphereRadius);

            // Draw a line between the start and end points
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, endPoint);
        }

        void Update()
        {
            // Skip all input processing if paused
            if (isInputPaused || PauseManager.IsGamePaused)
            {
                // We still want gravity to apply during pause if timeScale isn't zero
                if (Time.timeScale > 0 && !IsGrounded())
                {
                    // Just apply gravity
                    jumpVelocity += gravity * Time.deltaTime;
                    Vector3 gravityVelocity = Vector3.zero;
                    gravityVelocity.y = jumpVelocity;
                    characterController.Move(gravityVelocity * Time.deltaTime);
                }
                
                return;
            }
        
            MovementLogic();

            if (IsGrounded())
            {
                lastGroundedTime = Time.time;
            }

            // Modify jump condition
            if (!isInputPaused && jump.WasPerformedThisFrame() &&
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
    }
}