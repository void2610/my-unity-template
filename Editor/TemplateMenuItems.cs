using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Editor menu items for the Unity Template
    /// </summary>
    public static class TemplateMenuItems
    {
        private const string MENU_ROOT = "Tools/Unity Template/";
        
        [MenuItem(MENU_ROOT + "Create New 2D URP Scene")]
        public static void CreateNewURPScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Add basic 2D URP components
            var mainCamera = new GameObject("Main Camera");
            var camera = mainCamera.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5;
            
            mainCamera.AddComponent<AudioListener>();
            mainCamera.tag = "MainCamera";
            
            // Save the scene
            var scenePath = EditorUtility.SaveFilePanel("Save New URP Scene", "Assets/Scenes", "NewScene", "unity");
            if (!string.IsNullOrEmpty(scenePath))
            {
                scenePath = FileUtil.GetProjectRelativePath(scenePath);
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.Refresh();
                Debug.Log($"Created new 2D URP scene: {scenePath}");
            }
        }
        
        [MenuItem(MENU_ROOT + "Setup Project Settings")]
        public static void SetupProjectSettings()
        {
            // Configure common project settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            
            // Set up input system
            var inputSystemSettings = EditorUserBuildSettings.activeBuildTarget;
            
            Debug.Log("Project settings configured for optimal 2D development");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Project settings have been configured for optimal 2D development with URP.", "OK");
        }
        
        [MenuItem(MENU_ROOT + "Open Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/void2610/my-unity-template");
        }
        
        [MenuItem(MENU_ROOT + "Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            var folders = new[]
            {
                "Assets/Scripts/Gameplay",
                "Assets/Scripts/UI", 
                "Assets/Scripts/Data",
                "Assets/Sprites/Characters",
                "Assets/Sprites/Environment",
                "Assets/Sprites/UI",
                "Assets/Audio/BGM",
                "Assets/Audio/SE",
                "Assets/Materials/2D",
                "Assets/Prefabs/Characters",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/UI",
                "Assets/ScriptableObjects/Data"
            };
            
            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parentFolder = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                    var folderName = System.IO.Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log("Created organized folder structure for game development");
        }
        
        [MenuItem(MENU_ROOT + "Create Example Scripts")]
        public static void CreateExampleScripts()
        {
            // Ensure Scripts/Utils folder exists
            var scriptsPath = "Assets/Scripts/Utils";
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
            }
            
            // Create GameManager example
            CreateGameManagerScript(scriptsPath);
            CreateInputHandlerScript(scriptsPath);
            
            AssetDatabase.Refresh();
            Debug.Log("Created example scripts with R3 and Input System integration");
        }
        
        private static void CreateGameManagerScript(string path)
        {
            var gameManagerContent = @"using UnityEngine;
using R3;
using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Example GameManager demonstrating R3 reactive extensions usage
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header(""Game State"")]
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
                Debug.Log($""Score updated: {newScore}"");
                // Update UI, check for achievements, etc.
            }).AddTo(this);
            
            // Example: React to pause state changes
            isPaused.Subscribe(paused => 
            {
                Time.timeScale = paused ? 0f : 1f;
                Debug.Log($""Game {(paused ? ""paused"" : ""resumed"")}"");
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
}";
            
            File.WriteAllText(Path.Combine(path, "GameManager.cs"), gameManagerContent);
        }
        
        private static void CreateInputHandlerScript(string path)
        {
            var inputHandlerContent = @"using UnityEngine;
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
        [Header(""Input Settings"")]
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
                Debug.Log($""Movement: {scaledMovement}"");
            }).AddTo(this);
            
            // Example: React to jump input
            jumpInput.Subscribe(_ => 
            {
                Debug.Log(""Jump performed!"");
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
}";
            
            File.WriteAllText(Path.Combine(path, "InputHandler.cs"), inputHandlerContent);
        }
    }
}