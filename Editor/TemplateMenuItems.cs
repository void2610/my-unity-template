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
            // Ensure Scenes folder exists
            CreateFolderRecursively("Assets/Scenes");
            
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
            var scenePath = EditorUtility.SaveFilePanel("新しい2D URPシーンを保存", "Assets/Scenes", "NewScene", "unity");
            if (!string.IsNullOrEmpty(scenePath))
            {
                scenePath = FileUtil.GetProjectRelativePath(scenePath);
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.Refresh();
                Debug.Log($"新しい2D URPシーンを作成しました: {scenePath}");
                EditorUtility.DisplayDialog("シーン作成完了", 
                    $"新しい2D URPシーンを作成しました:\n{scenePath}", "OK");
            }
        }
        
        [MenuItem(MENU_ROOT + "Setup Project Settings")]
        public static void SetupProjectSettings()
        {
            // Configure common project settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            
            // Configure graphics settings for 2D
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            
            // Configure quality settings
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 2;
            
            Debug.Log("2D開発用にプロジェクト設定を最適化しました");
            EditorUtility.DisplayDialog("プロジェクト設定完了", 
                "2D開発とURPに最適なプロジェクト設定を適用しました。\n\n" +
                "・Linear色空間\n" +
                "・GPU Skinning有効\n" +
                "・V-Sync有効\n" +
                "・アンチエイリアシング設定", "OK");
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
            
            int createdCount = 0;
            
            foreach (var folder in folders)
            {
                if (CreateFolderRecursively(folder))
                {
                    createdCount++;
                }
            }
            
            AssetDatabase.Refresh();
            
            if (createdCount > 0)
            {
                Debug.Log($"フォルダ構造を作成しました: {createdCount}個のフォルダを作成");
                EditorUtility.DisplayDialog("フォルダ構造作成完了", 
                    $"{createdCount}個のフォルダを作成しました。\nProjectウィンドウで確認してください。", "OK");
            }
            else
            {
                Debug.Log("フォルダ構造は既に存在しています");
                EditorUtility.DisplayDialog("フォルダ構造", 
                    "フォルダ構造は既に存在しています。", "OK");
            }
        }
        
        private static bool CreateFolderRecursively(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return false; // Already exists
            }
            
            var parentPath = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(folderPath);
            
            // Ensure parent folder exists
            if (!string.IsNullOrEmpty(parentPath) && parentPath != "Assets")
            {
                CreateFolderRecursively(parentPath);
            }
            
            // Create the folder
            var guid = AssetDatabase.CreateFolder(parentPath, folderName);
            return !string.IsNullOrEmpty(guid);
        }
        
        [MenuItem(MENU_ROOT + "Create Example Scripts")]
        public static void CreateExampleScripts()
        {
            // Ensure Scripts/Utils folder exists
            var scriptsPath = "Assets/Scripts/Utils";
            CreateFolderRecursively(scriptsPath);
            
            // Check if scripts already exist
            var gameManagerPath = Path.Combine(scriptsPath, "GameManager.cs");
            var inputHandlerPath = Path.Combine(scriptsPath, "InputHandler.cs");
            
            bool gameManagerExists = File.Exists(gameManagerPath);
            bool inputHandlerExists = File.Exists(inputHandlerPath);
            
            if (gameManagerExists && inputHandlerExists)
            {
                bool overwrite = EditorUtility.DisplayDialog("スクリプトが既に存在します", 
                    "GameManager.cs と InputHandler.cs が既に存在します。\n上書きしますか？", 
                    "上書き", "キャンセル");
                
                if (!overwrite)
                {
                    return;
                }
            }
            
            // Create scripts
            CreateGameManagerScript(scriptsPath);
            CreateInputHandlerScript(scriptsPath);
            
            AssetDatabase.Refresh();
            
            Debug.Log("R3とInput Systemの統合例スクリプトを作成しました");
            EditorUtility.DisplayDialog("サンプルスクリプト作成完了", 
                "GameManager.cs と InputHandler.cs を作成しました。\n\n" +
                "これらはR3リアクティブプログラミングと\nInput Systemの使用例です。", "OK");
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