#if R3_INSTALLED
using UnityEngine;
using R3;
using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example GameManager demonstrating R3 reactive extensions usage
    /// R3 is automatically installed via NuGet packages.config
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private bool isGameActive = false;
        
        // Reactive properties using R3
        private readonly ReactiveProperty<int> score = new(0);
        private readonly ReactiveProperty<bool> isPaused = new(false);
        private readonly ReactiveProperty<float> timeRemaining = new(60f);
        
        // Public observables
        public ReadOnlyReactiveProperty<int> Score => score;
        public ReadOnlyReactiveProperty<bool> IsPaused => isPaused;
        public ReadOnlyReactiveProperty<float> TimeRemaining => timeRemaining;
        
        // Events
        public readonly Subject<Unit> OnGameStart = new();
        public readonly Subject<Unit> OnGameEnd = new();
        
        private void Start()
        {
            SetupReactiveBindings();
        }
        
        private void SetupReactiveBindings()
        {
            // Example: React to score changes
            score.Subscribe(newScore => 
            {
                Debug.Log($"Score updated: {newScore}");
                // Update UI, check for achievements, etc.
            }).AddTo(this);
            
            // Example: React to pause state changes
            isPaused.Subscribe(paused => 
            {
                Time.timeScale = paused ? 0f : 1f;
                Debug.Log($"Game {(paused ? "paused" : "resumed")}");
            }).AddTo(this);
        }
        
        public void StartGame()
        {
            isGameActive = true;
            score.Value = 0;
            timeRemaining.Value = 60f;
            isPaused.Value = false;
            OnGameStart.OnNext(Unit.Default);
        }
        
        public void EndGame()
        {
            isGameActive = false;
            OnGameEnd.OnNext(Unit.Default);
        }
        
        public void TogglePause()
        {
            isPaused.Value = !isPaused.Value;
        }
        
        public void AddScore(int points)
        {
            score.Value += points;
        }
        
        private void OnDestroy()
        {
            OnGameStart?.Dispose();
            OnGameEnd?.Dispose();
        }
    }
}
#else
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Fallback GameManager when R3 is not available
    /// Install R3 via NuGet to use the reactive version
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private bool isGameActive = false;
        [SerializeField] private int score = 0;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private float timeRemaining = 60f;
        
        public int Score => score;
        public bool IsPaused => isPaused;
        public float TimeRemaining => timeRemaining;
        
        public System.Action OnGameStart;
        public System.Action OnGameEnd;
        
        private void Start()
        {
            Debug.LogWarning("GameManager: R3 package not found. Using fallback implementation. Install R3 via NuGet for reactive features.");
        }
        
        public void StartGame()
        {
            isGameActive = true;
            score = 0;
            timeRemaining = 60f;
            isPaused = false;
            OnGameStart?.Invoke();
        }
        
        public void EndGame()
        {
            isGameActive = false;
            OnGameEnd?.Invoke();
        }
        
        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }
        
        public void AddScore(int points)
        {
            score += points;
            Debug.Log($"Score updated: {score}");
        }
    }
}
#endif