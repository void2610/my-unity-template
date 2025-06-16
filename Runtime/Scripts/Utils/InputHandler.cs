#if R3_INSTALLED && ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using R3;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example input handler using Unity Input System and R3
    /// Requires InputActions asset to be created first
    /// R3 is automatically installed via NuGet packages.config
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float movementSensitivity = 1f;
        
        // Reactive streams for input
        private readonly Subject<Vector2> moveInput = new();
        private readonly Subject<Unit> jumpInput = new();
        private readonly Subject<Unit> interactInput = new();
        
        // Public observables
        public Observable<Vector2> MoveInput => moveInput;
        public Observable<Unit> JumpInput => jumpInput;
        public Observable<Unit> InteractInput => interactInput;
        
        private void Start()
        {
            SetupInputBindings();
        }
        
        private void SetupInputBindings()
        {
            // Example: React to movement input
            moveInput.Subscribe(movement => 
            {
                var scaledMovement = movement * movementSensitivity;
                Debug.Log($"Movement: {scaledMovement}");
            }).AddTo(this);
            
            // Example: React to jump input
            jumpInput.Subscribe(_ => 
            {
                Debug.Log("Jump performed!");
            }).AddTo(this);
        }
        
        // Call these methods from Unity Events or Input System callbacks
        public void OnMove(Vector2 movement)
        {
            moveInput.OnNext(movement);
        }
        
        public void OnJump()
        {
            jumpInput.OnNext(Unit.Default);
        }
        
        public void OnInteract()
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
#elif ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Input handler using Unity Input System (without R3)
    /// Install R3 via NuGet to use the reactive version
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float movementSensitivity = 1f;
        
        public System.Action<Vector2> OnMoveInput;
        public System.Action OnJumpInput;
        public System.Action OnInteractInput;
        
        private void Start()
        {
            Debug.LogWarning("InputHandler: R3 package not found. Using event-based implementation. Install R3 via NuGet for reactive features.");
        }
        
        public void OnMove(Vector2 movement)
        {
            var scaledMovement = movement * movementSensitivity;
            OnMoveInput?.Invoke(scaledMovement);
            Debug.Log($"Movement: {scaledMovement}");
        }
        
        public void OnJump()
        {
            OnJumpInput?.Invoke();
            Debug.Log("Jump performed!");
        }
        
        public void OnInteract()
        {
            OnInteractInput?.Invoke();
        }
    }
}
#else
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Fallback input handler using legacy Input Manager
    /// Install Input System and R3 packages for full functionality
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private float movementSensitivity = 1f;
        
        public System.Action<Vector2> OnMoveInput;
        public System.Action OnJumpInput;
        public System.Action OnInteractInput;
        
        private void Start()
        {
            Debug.LogWarning("InputHandler: Input System and R3 packages not found. Using legacy Input Manager.");
        }
        
        private void Update()
        {
            // Legacy input handling
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            var movement = new Vector2(horizontal, vertical) * movementSensitivity;
            
            if (movement.magnitude > 0.1f)
            {
                OnMoveInput?.Invoke(movement);
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnJumpInput?.Invoke();
                Debug.Log("Jump performed!");
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnInteractInput?.Invoke();
            }
        }
    }
}
#endif