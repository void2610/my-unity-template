using UnityEngine;
using UnityEngine.InputSystem;
using R3;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example input handler using Unity Input System and R3
    /// Requires InputActions asset to be created first
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