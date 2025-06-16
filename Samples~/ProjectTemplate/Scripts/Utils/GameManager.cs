using UnityEngine;
using R3;
using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example GameManager demonstrating R3 reactive extensions usage
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
            
            // Example: Timer countdown
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Where(_ => isGameActive && !isPaused.Value)
                .Subscribe(_ => 
                {
                    if (timeRemaining.Value > 0)
                    {
                        timeRemaining.Value -= 1f;
                    }
                    else
                    {
                        EndGame();
                    }
                })
                .AddTo(this);
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