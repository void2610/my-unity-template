using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Void2610.UnityTemplate.Editor
{
    [System.Serializable]
    public class TemplateManifestData
    {
        public string[] packages = new string[0];
        public string[] gitPackages = new string[0];
        public string[] testables = new string[0];
    }

    [System.Serializable]
    public class ManifestData
    {
        public Dictionary<string, string> dependencies = new();
        public string[] scopedRegistries = new string[0];
        public string[] testables = new string[0];
    }

    [System.Serializable]
    public class InstallationState
    {
        public List<string> remainingPackages = new();
        public bool isInstalling = false;
        public int totalPackages = 0;
    }

    /// <summary>
    /// Editor menu items for the Unity Template
    /// </summary>
    [InitializeOnLoad]
    public static class TemplateMenuItems
    {
        private const string MENU_ROOT = "Tools/Unity Template/";
        private const string PREF_KEY_INSTALL_STATE = "UnityTemplate_InstallState";
        private const string PREF_KEY_INSTALL_QUEUE = "UnityTemplate_InstallQueue";
        
        private static AddRequest currentAddRequest;
        private static System.Collections.Generic.Queue<string> packageQueue = new();
        private static bool isInstallingPackages = false;
        private static int totalPackagesToInstall = 0;
        
        static TemplateMenuItems()
        {
            // ドメインリロード後の状態復元
            EditorApplication.delayCall += RestoreInstallationStateAfterReload;
        }
        
        
        [MenuItem(MENU_ROOT + "Install Dependencies")]
        public static void InstallDependencies()
        {
            if (isInstallingPackages)
            {
                bool cancel = EditorUtility.DisplayDialog("インストール中", 
                    "依存関係のインストールが進行中です。\n\nキャンセルしますか？", 
                    "キャンセル", "待機");
                
                if (cancel)
                {
                    CancelInstallation();
                }
                return;
            }

            Debug.Log("=== Dependencies Installation Started ===");

            // Load template manifest
            var templateManifest = LoadTemplateManifest();
            if (templateManifest == null)
            {
                Debug.LogError("Failed to load template manifest");
                EditorUtility.DisplayDialog("エラー", 
                    "テンプレートマニフェストの読み込みに失敗しました。", "OK");
                return;
            }

            Debug.Log($"Template manifest loaded: {templateManifest.packages.Length} packages, {templateManifest.gitPackages.Length} git packages");

            // Count packages to install
            var currentManifest = LoadCurrentManifest();
            Debug.Log($"Current manifest loaded: {currentManifest.dependencies.Count} dependencies");

            var packagesToInstall = GetPackagesToInstall(templateManifest, currentManifest);
            Debug.Log($"Packages to install: {packagesToInstall.Count}");

            if (packagesToInstall.Count == 0)
            {
                Debug.Log("All dependencies already installed");
                EditorUtility.DisplayDialog("依存関係", 
                    "すべての依存関係は既にインストール済みです。", "OK");
                return;
            }

            Debug.Log($"Packages to install: {string.Join(", ", packagesToInstall)}");

            bool proceed = EditorUtility.DisplayDialog("依存関係のインストール", 
                $"以下の{packagesToInstall.Count}個のパッケージをインストールします:\n\n" +
                string.Join("\n", packagesToInstall.Take(5).Select(p => $"• {GetPackageDisplayName(p)}")) +
                (packagesToInstall.Count > 5 ? $"\n...他{packagesToInstall.Count - 5}個" : "") +
                "\n\nこの処理には時間がかかる場合があります。\n続行しますか？", 
                "インストール", "キャンセル");

            if (!proceed) return;

            StartDependencyInstallation(packagesToInstall);
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
        
        
        private static TemplateManifestData LoadTemplateManifest()
        {
            var templateAsset = Resources.Load<TextAsset>("template-manifest");
            if (templateAsset == null)
            {
                Debug.LogError("template-manifest.json が見つかりません");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<TemplateManifestData>(templateAsset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"template-manifest.json の解析に失敗しました: {e.Message}");
                return null;
            }
        }

        private static ManifestData LoadCurrentManifest()
        {
            var manifestPath = "Packages/manifest.json";
            if (!File.Exists(manifestPath))
            {
                return new ManifestData();
            }

            try
            {
                var manifestText = File.ReadAllText(manifestPath);
                return JsonUtility.FromJson<ManifestData>(manifestText);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"現在のmanifest.jsonの読み込みに失敗しました: {e.Message}");
                return new ManifestData();
            }
        }

        private static List<string> GetPackagesToInstall(TemplateManifestData templateManifest, ManifestData currentManifest)
        {
            var packagesToInstall = new List<string>();

            // 通常のUnityパッケージ（バージョン不指定）
            foreach (var packageId in templateManifest.packages)
            {
                if (!currentManifest.dependencies.ContainsKey(packageId))
                {
                    packagesToInstall.Add(packageId);
                }
            }

            // Gitパッケージ
            foreach (var gitPackage in templateManifest.gitPackages)
            {
                // より厳密な重複チェック：完全なgit URLまたは同じパスを持つパッケージのみスキップ
                var isAlreadyInstalled = currentManifest.dependencies.Keys.Any(key => 
                    key.Contains("github.com") && IsSameGitPackage(key, gitPackage));
                
                Debug.Log($"Checking git package: {gitPackage}, already installed: {isAlreadyInstalled}");
                
                if (!isAlreadyInstalled)
                {
                    packagesToInstall.Add(gitPackage);
                }
                else
                {
                    Debug.Log($"Skipping {gitPackage} - already installed");
                }
            }

            return packagesToInstall;
        }

        private static bool IsSameGitPackage(string installedUrl, string targetUrl)
        {
            try
            {
                // パッケージの完全なパスまで含めて比較
                var installedPath = ExtractGitPackagePath(installedUrl);
                var targetPath = ExtractGitPackagePath(targetUrl);
                
                Debug.Log($"Comparing git packages: installed='{installedPath}' vs target='{targetPath}'");
                
                return installedPath == targetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error comparing git packages: {e.Message}");
                return false;
            }
        }

        private static string ExtractGitPackagePath(string gitUrl)
        {
            // "https://github.com/owner/repo.git?path=/specific/path" から "owner/repo/specific/path" を抽出
            var repoMatch = Regex.Match(gitUrl, @"github\.com/([^/]+/[^/\?\.]+)");
            if (!repoMatch.Success) return gitUrl;
            
            var repo = repoMatch.Groups[1].Value;
            
            var pathMatch = Regex.Match(gitUrl, @"path=([^&]+)");
            if (pathMatch.Success)
            {
                var path = pathMatch.Groups[1].Value.TrimStart('/');
                return $"{repo}/{path}";
            }
            
            return repo;
        }

        private static string GetPackageDisplayName(string packageId)
        {
            if (packageId.Contains("github.com"))
            {
                if (packageId.Contains("NuGetForUnity"))
                    return "NuGetForUnity";
                if (packageId.Contains("R3"))
                    return "R3 Unity";
            }
            
            return packageId.Replace("com.unity.", "").Replace("com.", "");
        }

        private static void StartDependencyInstallation(List<string> packagesToInstall)
        {
            Debug.Log($"StartDependencyInstallation called with {packagesToInstall.Count} packages");
            
            isInstallingPackages = true;
            
            packageQueue.Clear();
            
            // NuGetForUnityを最初にインストール（重要）
            var nugetPackage = packagesToInstall.FirstOrDefault(p => p.Contains("NuGetForUnity"));
            if (!string.IsNullOrEmpty(nugetPackage))
            {
                Debug.Log($"Found NuGetForUnity package: {nugetPackage}");
                packageQueue.Enqueue(nugetPackage);
                packagesToInstall.Remove(nugetPackage);
            }
            else
            {
                Debug.Log("No NuGetForUnity package found");
            }
            
            // 残りのパッケージをキューに追加
            foreach (var package in packagesToInstall)
            {
                Debug.Log($"Adding package to queue: {package}");
                packageQueue.Enqueue(package);
            }
            
            totalPackagesToInstall = packageQueue.Count;
            
            Debug.Log($"Queue created with {packageQueue.Count} packages: {string.Join(", ", packageQueue)}");
            Debug.Log($"依存関係のインストールを開始します... ({packageQueue.Count}個のパッケージ)");
            
            // インストール状態を保存
            SaveInstallationState();
            
            EditorUtility.DisplayProgressBar("依存関係インストール", "インストール開始...", 0f);
            
            InstallNextPackage();
        }
        
        private static void InstallNextPackage()
        {
            Debug.Log($"InstallNextPackage called. Queue count: {packageQueue.Count}");
            
            if (packageQueue.Count == 0)
            {
                // 全てのパッケージインストール完了
                EditorUtility.ClearProgressBar();
                isInstallingPackages = false;
                
                // インストール状態をクリア
                ClearInstallationState();
                
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
            
            var packageId = packageQueue.Dequeue();
            var packageName = GetPackageDisplayName(packageId);
            
            // 状態を保存（キューから取り出した後）
            SaveInstallationState();
            
            var currentIndex = totalPackagesToInstall - packageQueue.Count;
            var progress = (float)currentIndex / totalPackagesToInstall;
            
            EditorUtility.DisplayProgressBar("依存関係インストール", 
                $"インストール中: {packageName} ({currentIndex}/{totalPackagesToInstall})", progress);
            
            Debug.Log($"パッケージをインストール中: {packageName} ({packageId})");
            Debug.Log($"Calling Client.Add with: {packageId}");
            
            currentAddRequest = Client.Add(packageId);
            Debug.Log($"AddRequest created. Is null: {currentAddRequest == null}");
            
            EditorApplication.update += PackageInstallProgress;
        }
        
        private static void PackageInstallProgress()
        {
            if (currentAddRequest == null)
            {
                Debug.LogError("currentAddRequest is null in PackageInstallProgress");
                EditorApplication.update -= PackageInstallProgress;
                return;
            }
            
            Debug.Log($"PackageInstallProgress - IsCompleted: {currentAddRequest.IsCompleted}, Status: {currentAddRequest.Status}");
            
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
                    
                    // エラー時もインストール状態をクリア
                    ClearInstallationState();
                    
                    var errorMessage = currentAddRequest.Error?.message ?? "Unknown error";
                    Debug.LogError($"パッケージインストールエラー: {errorMessage}");
                    EditorUtility.DisplayDialog("インストールエラー", 
                        $"パッケージのインストールに失敗しました:\n{errorMessage}\n\n" +
                        "手動でインストールしてください。", "OK");
                }
                
                currentAddRequest = null;
            }
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
        
        private static void RestoreInstallationStateAfterReload()
        {
            // EditorPrefsから状態を復元
            var stateJson = EditorPrefs.GetString(PREF_KEY_INSTALL_STATE, "");
            if (string.IsNullOrEmpty(stateJson))
            {
                return;
            }
            
            try
            {
                var state = JsonUtility.FromJson<InstallationState>(stateJson);
                if (state != null && state.isInstalling && state.remainingPackages.Count > 0)
                {
                    Debug.Log("=== パッケージインストールを再開します ===");
                    Debug.Log($"残りのパッケージ: {state.remainingPackages.Count}個");
                    
                    // キューを復元
                    packageQueue.Clear();
                    foreach (var package in state.remainingPackages)
                    {
                        packageQueue.Enqueue(package);
                    }
                    
                    isInstallingPackages = true;
                    totalPackagesToInstall = state.totalPackages;
                    
                    // 少し待ってからインストールを再開
                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.DisplayProgressBar("依存関係インストール", "インストールを再開中...", 0f);
                        InstallNextPackage();
                    };
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"インストール状態の復元に失敗しました: {e.Message}");
                ClearInstallationState();
            }
        }
        
        private static void SaveInstallationState()
        {
            var state = new InstallationState
            {
                remainingPackages = packageQueue.ToList(),
                isInstalling = isInstallingPackages,
                totalPackages = totalPackagesToInstall
            };
            
            var stateJson = JsonUtility.ToJson(state);
            EditorPrefs.SetString(PREF_KEY_INSTALL_STATE, stateJson);
        }
        
        private static void ClearInstallationState()
        {
            EditorPrefs.DeleteKey(PREF_KEY_INSTALL_STATE);
            EditorPrefs.DeleteKey(PREF_KEY_INSTALL_QUEUE);
        }
        
        private static void CancelInstallation()
        {
            Debug.Log("=== パッケージインストールをキャンセルしました ===");
            
            isInstallingPackages = false;
            packageQueue.Clear();
            currentAddRequest = null;
            
            EditorApplication.update -= PackageInstallProgress;
            EditorUtility.ClearProgressBar();
            
            ClearInstallationState();
            
            EditorUtility.DisplayDialog("キャンセル完了", 
                "依存関係のインストールをキャンセルしました。", "OK");
        }
    }
}