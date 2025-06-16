using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Editor menu items for the Unity Template
    /// </summary>
    public static class TemplateMenuItems
    {
        private const string MENU_ROOT = "Tools/Unity Template/";
        
        private static AddRequest currentAddRequest;
        private static System.Collections.Generic.Queue<string> packageQueue = new();
        private static bool isInstallingPackages = false;
        
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
        
        [MenuItem(MENU_ROOT + "Install Dependencies")]
        public static void InstallDependencies()
        {
            if (isInstallingPackages)
            {
                EditorUtility.DisplayDialog("インストール中", 
                    "依存関係のインストールが進行中です。\n完了までお待ちください。", "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog("依存関係のインストール", 
                "以下のパッケージをインストールします:\n\n" +
                "• R3 (Unity用リアクティブ拡張)\n" +
                "• NuGetForUnity (NuGetパッケージ管理)\n" +
                "• 必要なUnityパッケージ\n\n" +
                "この処理には時間がかかる場合があります。\n続行しますか？", 
                "インストール", "キャンセル");

            if (!proceed) return;

            StartDependencyInstallation();
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
                "Assets/Scripts",
                "Assets/Scripts/Utils",
                "Assets/Sprites",
                "Assets/Audio/BGM",
                "Assets/Audio/SE",
                "Assets/Materials",
                "Assets/Prefabs",
                "Assets/ScriptableObjects"
            };
            
            int createdCount = 0;
            
            foreach (var folder in folders)
            {
                if (CreateFolderRecursively(folder))
                {
                    createdCount++;
                }
            }
            
            // Copy utility scripts from package
            int copiedScripts = CopyUtilityScripts();
            
            AssetDatabase.Refresh();
            
            var message = "";
            if (createdCount > 0)
            {
                message += $"{createdCount}個のフォルダを作成しました。\n";
            }
            if (copiedScripts > 0)
            {
                message += $"{copiedScripts}個のユーティリティスクリプトをコピーしました。\n";
            }
            
            if (createdCount > 0 || copiedScripts > 0)
            {
                message += "Projectウィンドウで確認してください。";
                if (copiedScripts > 0)
                {
                    message += "\n\n注意: スクリプトはR3ライブラリを使用します。\n'Install Dependencies'を実行してください。";
                }
                Debug.Log($"フォルダ構造作成完了: {message}");
                EditorUtility.DisplayDialog("フォルダ構造作成完了", message, "OK");
            }
            else
            {
                Debug.Log("フォルダ構造とスクリプトは既に存在しています");
                EditorUtility.DisplayDialog("フォルダ構造", 
                    "フォルダ構造とスクリプトは既に存在しています。", "OK");
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
        
        private static int CopyUtilityScripts()
        {
            var targetPath = "Assets/Scripts/Utils";
            
            // Ensure target directory exists
            CreateFolderRecursively(targetPath);
            
            var scriptTemplates = new[] 
            { 
                ("GameManager.cs", "ScriptTemplates/GameManager.cs"),
                ("InputHandler.cs", "ScriptTemplates/InputHandler.cs")
            };
            int copiedCount = 0;
            
            foreach (var (fileName, resourcePath) in scriptTemplates)
            {
                var destPath = $"{targetPath}/{fileName}";
                
                // Check if destination doesn't exist
                if (!File.Exists(destPath))
                {
                    // Load template from Resources
                    var templateAsset = Resources.Load<TextAsset>(resourcePath);
                    if (templateAsset != null)
                    {
                        File.WriteAllText(destPath, templateAsset.text);
                        copiedCount++;
                        Debug.Log($"テンプレートからスクリプトをコピーしました: {fileName}");
                    }
                    else
                    {
                        Debug.LogWarning($"テンプレートファイルが見つかりません: {resourcePath}");
                    }
                }
            }
            
            return copiedCount;
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
                
                // Delete existing files for overwrite
                if (gameManagerExists) File.Delete(gameManagerPath);
                if (inputHandlerExists) File.Delete(inputHandlerPath);
            }
            
            // Copy from templates
            int copiedCount = CopyUtilityScripts();
            
            AssetDatabase.Refresh();
            
            if (copiedCount > 0)
            {
                Debug.Log("R3とInput Systemの統合例スクリプトを作成しました");
                EditorUtility.DisplayDialog("サンプルスクリプト作成完了", 
                    $"{copiedCount}個のスクリプトを作成しました:\n" +
                    "GameManager.cs と InputHandler.cs\n\n" +
                    "注意: R3ライブラリが必要です。\n" +
                    "'Install Dependencies'を先に実行してください。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", 
                    "スクリプトテンプレートの読み込みに失敗しました。", "OK");
            }
        }
        
        
        private static void StartDependencyInstallation()
        {
            isInstallingPackages = true;
            
            // パッケージのインストール順序（重要：NuGetForUnityを最初にインストール）
            var packagesToInstall = new[]
            {
                "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
                "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity"
            };
            
            packageQueue.Clear();
            foreach (var package in packagesToInstall)
            {
                packageQueue.Enqueue(package);
            }
            
            Debug.Log("依存関係のインストールを開始します...");
            EditorUtility.DisplayProgressBar("依存関係インストール", "インストール開始...", 0f);
            
            InstallNextPackage();
        }
        
        private static void InstallNextPackage()
        {
            if (packageQueue.Count == 0)
            {
                // 全てのパッケージインストール完了
                EditorUtility.ClearProgressBar();
                isInstallingPackages = false;
                
                Debug.Log("依存関係のインストールが完了しました");
                EditorUtility.DisplayDialog("インストール完了", 
                    "依存関係のインストールが完了しました。\n\n" +
                    "次の手順:\n" +
                    "1. NuGetForUnityが追加されました\n" +
                    "2. Window > NuGetForUnity を開いてください\n" +
                    "3. 'R3' を検索してインストールしてください\n" +
                    "4. その後、テンプレートツールが使用可能になります", "OK");
                
                ShowPostInstallInstructions();
                return;
            }
            
            var packageUrl = packageQueue.Dequeue();
            var packageName = GetPackageNameFromUrl(packageUrl);
            var progress = 1f - (packageQueue.Count + 1) / 2f;
            
            EditorUtility.DisplayProgressBar("依存関係インストール", 
                $"インストール中: {packageName}", progress);
            
            Debug.Log($"パッケージをインストール中: {packageName}");
            currentAddRequest = Client.Add(packageUrl);
            EditorApplication.update += PackageInstallProgress;
        }
        
        private static void PackageInstallProgress()
        {
            if (currentAddRequest.IsCompleted)
            {
                EditorApplication.update -= PackageInstallProgress;
                
                if (currentAddRequest.Status == StatusCode.Success)
                {
                    Debug.Log($"パッケージインストール成功: {currentAddRequest.Result.displayName}");
                    
                    // 少し待ってから次のパッケージをインストール
                    EditorApplication.delayCall += () => {
                        System.Threading.Thread.Sleep(1000); // 1秒待機
                        InstallNextPackage();
                    };
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    isInstallingPackages = false;
                    
                    Debug.LogError($"パッケージインストールエラー: {currentAddRequest.Error.message}");
                    EditorUtility.DisplayDialog("インストールエラー", 
                        $"パッケージのインストールに失敗しました:\n{currentAddRequest.Error.message}\n\n" +
                        "手動でインストールしてください。", "OK");
                }
                
                currentAddRequest = null;
            }
        }
        
        private static string GetPackageNameFromUrl(string url)
        {
            if (url.Contains("NuGetForUnity"))
                return "NuGetForUnity";
            if (url.Contains("R3"))
                return "R3 Unity";
            return "Unknown Package";
        }
        
        private static void ShowPostInstallInstructions()
        {
            var message = "=== R3使用のための追加手順 ===\n\n" +
                         "1. Window > NuGetForUnity を開く\n" +
                         "2. 検索ボックスに 'R3' と入力\n" +
                         "3. 'R3' パッケージを見つけてインストール\n" +
                         "4. 'Microsoft.Bcl.AsyncInterfaces' もインストール\n" +
                         "5. インストール完了後、Unityを再起動\n\n" +
                         "これで Templates ツールが正常に動作します！";
                         
            Debug.Log(message);
        }
    }
}