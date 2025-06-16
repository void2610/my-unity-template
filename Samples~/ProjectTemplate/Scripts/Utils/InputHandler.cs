using UnityEngine;
using UnityEngine.InputSystem;
using R3;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example input handler using Unity Input System and R3
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float movementSensitivity = 1f;
        
        // Input Actions (generated from InputSystem_Actions.inputactions)
        private InputActions inputActions;
        
        // Reactive streams for input
        private readonly Subject<Vector2> moveInput = new();
        private readonly Subject<Unit> jumpInput = new();
        private readonly Subject<Unit> interactInput = new();
        
        // Public observables
        public Observable<Vector2> MoveInput => moveInput;
        public Observable<Unit> JumpInput => jumpInput;
        public Observable<Unit> InteractInput => interactInput;
        
        private void Awake()
        {
            inputActions = new InputActions();
        }
        
        private void OnEnable()
        {
            inputActions.Enable();
            SetupInputBindings();
        }
        
        private void OnDisable()
        {
            inputActions.Disable();
        }
        
        private void SetupInputBindings()
        {
            // Movement input
            inputActions.Player.Move.performed += OnMove;
            inputActions.Player.Move.canceled += OnMove;
            
            // Jump input
            inputActions.Player.Jump.performed += OnJump;
            
            // Interact input
            inputActions.Player.Interact.performed += OnInteract;
            
            // Example: React to movement input
            moveInput.Subscribe(movement => 
            {
                // Apply movement to player or camera
                var scaledMovement = movement * movementSensitivity;
                Debug.Log($"Movement: {scaledMovement}");
            }).AddTo(this);
            
            // Example: React to jump input
            jumpInput.Subscribe(_ => 
            {
                Debug.Log("Jump performed!");
            }).AddTo(this);
        }
        
        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();
            moveInput.OnNext(movement);
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            jumpInput.OnNext(Unit.Default);
        }
        
        private void OnInteract(InputAction.CallbackContext context)
        {
            interactInput.OnNext(Unit.Default);
        }
        
        private void OnDestroy()
        {
            moveInput?.Dispose();
            jumpInput?.Dispose();
            interactInput?.Dispose();
        }
    }
}