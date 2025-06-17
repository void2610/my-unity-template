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

            // Load template manifest
            var templateManifest = LoadTemplateManifest();
            if (templateManifest == null)
            {
                Debug.LogError("Failed to load template manifest");
                EditorUtility.DisplayDialog("エラー", 
                    "テンプレートマニフェストの読み込みに失敗しました。", "OK");
                return;
            }

            // Count packages to install
            var currentManifest = LoadCurrentManifest();
            var packagesToInstall = GetPackagesToInstall(templateManifest, currentManifest);

            if (packagesToInstall.Count == 0)
            {
                EditorUtility.DisplayDialog("依存関係", 
                    "すべての依存関係は既にインストール済みです。", "OK");
                return;
            }

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
                "Assets/ScriptableObjects",
                "Assets/Editor",
                "Assets/Others"
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
                EditorUtility.DisplayDialog("フォルダ構造作成完了", 
                    $"{createdCount}個のフォルダを作成しました。\nProjectウィンドウで確認してください。", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("フォルダ構造", 
                    "フォルダ構造は既に存在しています。", "OK");
            }
        }
        
        [MenuItem(MENU_ROOT + "Copy Utility Scripts")]
        public static void CopyUtilityScriptsMenuItem()
        {
            int copiedScripts = CopyUtilityScripts();
            
            AssetDatabase.Refresh();
            
            if (copiedScripts > 0)
            {
                EditorUtility.DisplayDialog("スクリプトコピー完了", 
                    $"{copiedScripts}個のユーティリティスクリプトをコピーしました。\n" +
                    "Projectウィンドウで確認してください。\n\n" +
                    "注意: R3ライブラリが必要です。先に'Install Dependencies'を実行してください。", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("スクリプトコピー", 
                    "ユーティリティスクリプトは既に存在しています。", "OK");
            }
        }
        
        [MenuItem(MENU_ROOT + "Copy Editor Scripts")]
        public static void CopyEditorScriptsMenuItem()
        {
            int copiedScripts = CopyEditorScripts();
            
            AssetDatabase.Refresh();
            
            if (copiedScripts > 0)
            {
                EditorUtility.DisplayDialog("エディタスクリプトコピー完了", 
                    $"{copiedScripts}個のエディタスクリプトをコピーしました。\n" +
                    "Assets/Editorフォルダで確認してください。\n\n" +
                    "注意: Unity Toolbar Extenderライブラリが必要です。先に'Install Dependencies'を実行してください。", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("エディタスクリプトコピー", 
                    "エディタスクリプトは既に存在しています。", "OK");
            }
        }
        
        [MenuItem(MENU_ROOT + "Copy License Files")]
        public static void CopyLicenseFiles()
        {
            // Check if LicenseMaster is installed (manual installation)
            var licenseMasterAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Contains("LicenseMaster"));
                
            if (licenseMasterAssembly == null)
            {
                bool openGitHub = EditorUtility.DisplayDialog("LicenseMaster未インストール", 
                    "LicenseMasterが手動でインストールされていません。\n\n" +
                    "1. 以下のGitHubページからUnityPackageをダウンロード\n" +
                    "2. Unityプロジェクトにインポート\n" +
                    "3. 再度この機能を実行\n\n" +
                    "GitHubページを開きますか？", 
                    "GitHubを開く", "キャンセル");
                
                if (openGitHub)
                {
                    Application.OpenURL("https://github.com/syskentokyo/unitylicensemaster/releases");
                }
                return;
            }
            
            int copiedCount = CopyLicenseFilesFromTemplate();
            
            AssetDatabase.Refresh();
            
            if (copiedCount > 0)
            {
                EditorUtility.DisplayDialog("ライセンスファイルコピー完了", 
                    $"{copiedCount}個のライセンスファイルをコピーしました。\n" +
                    "Assets/LicenseMaster/フォルダで確認してください。", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("ライセンスファイル", 
                    "ライセンスファイルは既に存在しているか、テンプレートが見つかりません。", "OK");
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
        
        private static string GetPackagePath()
        {
            var scriptFiles = AssetDatabase.FindAssets("TemplateMenuItems t:Script");
            if (scriptFiles.Length == 0)
            {
                Debug.LogError("TemplateMenuItems スクリプトが見つかりません");
                return null;
            }
            
            var scriptPath = AssetDatabase.GUIDToAssetPath(scriptFiles[0]);
            return Path.GetDirectoryName(scriptPath);
        }
        
        private static int CopyUtilityScripts()
        {
            var targetPath = "Assets/Scripts/Utils";
            
            // Ensure target directory exists
            CreateFolderRecursively(targetPath);
            
            var scriptTemplates = new[] 
            { 
                ("SingletonMonoBehaviour.cs", "SingletonMonoBehaviour.cs.template"),
                ("SerializableDictionary.cs", "SerializableDictionary.cs.template"),
                ("CameraAspectRatioHandler.cs", "CameraAspectRatioHandler.cs.template"),
                ("ExtendedMethods.cs", "ExtendedMethods.cs.template"),
                ("DebugLogDisplay.cs", "DebugLogDisplay.cs.template"),
                ("CanvasAspectRatioFitter.cs", "CanvasAspectRatioFitter.cs.template"),
                ("FloatMove.cs", "FloatMove.cs.template"),
                ("MyButton.cs", "MyButton.cs.template"),
                ("TMPInputFieldCaretFixer.cs", "TMPInputFieldCaretFixer.cs.template"),
                ("ButtonSe.cs", "ButtonSe.cs.template"),
                ("SpriteSheetAnimator.cs", "SpriteSheetAnimator.cs.template")
            };
            int copiedCount = 0;
            
            var packagePath = GetPackagePath();
            if (packagePath == null) return 0;
            
            var templatesPath = Path.Combine(packagePath, "ScriptTemplates");
            
            foreach (var (fileName, templateFileName) in scriptTemplates)
            {
                var destPath = $"{targetPath}/{fileName}";
                
                // Check if destination doesn't exist
                if (!File.Exists(destPath))
                {
                    var templatePath = Path.Combine(templatesPath, templateFileName);
                    if (File.Exists(templatePath))
                    {
                        var templateContent = File.ReadAllText(templatePath);
                        File.WriteAllText(destPath, templateContent);
                        copiedCount++;
                        Debug.Log($"テンプレートからスクリプトをコピーしました: {templateFileName} → {fileName}");
                    }
                    else
                    {
                        Debug.LogWarning($"テンプレート未発見: {templatePath}");
                    }
                }
            }
            
            return copiedCount;
        }
        
        private static int CopyEditorScripts()
        {
            var targetPath = "Assets/Editor";
            
            // Ensure target directory exists
            CreateFolderRecursively(targetPath);
            
            var editorTemplates = new[] 
            { 
                ("SceneSwitchLeftButton.cs", "SceneSwitchLeftButton.cs.template")
            };
            int copiedCount = 0;
            
            var packagePath = GetPackagePath();
            if (packagePath == null) return 0;
            
            var templatesPath = Path.Combine(packagePath, "ScriptTemplates");
            
            foreach (var (fileName, templateFileName) in editorTemplates)
            {
                var destPath = $"{targetPath}/{fileName}";
                
                // Check if destination doesn't exist
                if (!File.Exists(destPath))
                {
                    var templatePath = Path.Combine(templatesPath, templateFileName);
                    if (File.Exists(templatePath))
                    {
                        var templateContent = File.ReadAllText(templatePath);
                        File.WriteAllText(destPath, templateContent);
                        copiedCount++;
                        Debug.Log($"テンプレートからエディタスクリプトをコピーしました: {templateFileName} → {fileName}");
                    }
                    else
                    {
                        Debug.LogWarning($"エディタテンプレート未発見: {templatePath}");
                    }
                }
            }
            
            return copiedCount;
        }
        
        private static int CopyLicenseFilesFromTemplate()
        {
            var packagePath = GetPackagePath();
            if (packagePath == null) return 0;
            
            var licenseTemplatesPath = Path.Combine(packagePath, "LicenseTemplates");
            
            // LicenseMasterのライセンスファイル保存先ディレクトリを作成
            var targetPath = "Assets/LicenseMaster";
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                CreateFolderRecursively(targetPath);
            }
            
            // .assetファイルを検索してコピー
            var sourceDir = new DirectoryInfo(licenseTemplatesPath);
            if (!sourceDir.Exists)
            {
                Debug.LogWarning($"ライセンステンプレートフォルダが見つかりません: {licenseTemplatesPath}");
                return 0;
            }
            
            var licenseFiles = sourceDir.GetFiles("*.asset");
            int copiedCount = 0;
            
            foreach (var file in licenseFiles)
            {
                var destPath = Path.Combine(targetPath, file.Name);
                
                if (!File.Exists(destPath))
                {
                    File.Copy(file.FullName, destPath);
                    copiedCount++;
                    Debug.Log($"ライセンスファイルをコピーしました: {file.Name}");
                }
            }
            
            return copiedCount;
        }
        
        private static TemplateManifestData LoadTemplateManifest()
        {
            var packagePath = GetPackagePath();
            if (packagePath == null) return null;
            
            var manifestPath = Path.Combine(packagePath, "template-manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"template-manifest.json が見つかりません: {manifestPath}");
                return null;
            }

            try
            {
                var manifestText = File.ReadAllText(manifestPath);
                return JsonUtility.FromJson<TemplateManifestData>(manifestText);
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

            // Unity 6で組み込みになったパッケージのリスト
            var unity6BuiltInPackages = new HashSet<string>
            {
                "com.unity.textmeshpro", // Unity 6では組み込み
                "com.unity.ugui" // Unity 6では組み込み
            };
            
            // Unityバージョンチェック
            bool isUnity6OrNewer = Application.unityVersion.StartsWith("6") || 
                                  Application.unityVersion.CompareTo("6000") >= 0;

            // 通常のUnityパッケージ（バージョン不指定）
            foreach (var packageId in templateManifest.packages)
            {
                // Unity 6以降で組み込みパッケージはスキップ
                if (isUnity6OrNewer && unity6BuiltInPackages.Contains(packageId))
                {
                    continue;
                }
                
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
                
                if (!isAlreadyInstalled)
                {
                    packagesToInstall.Add(gitPackage);
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
            isInstallingPackages = true;
            skippedPackagesCount = 0; // リセット
            
            packageQueue.Clear();
            
            // NuGetForUnityを最初にインストール（重要）
            var nugetPackage = packagesToInstall.FirstOrDefault(p => p.Contains("NuGetForUnity"));
            if (!string.IsNullOrEmpty(nugetPackage))
            {
                packageQueue.Enqueue(nugetPackage);
                packagesToInstall.Remove(nugetPackage);
            }
            
            // 残りのパッケージをキューに追加
            foreach (var package in packagesToInstall)
            {
                packageQueue.Enqueue(package);
            }
            
            totalPackagesToInstall = packageQueue.Count;
            
            Debug.Log($"依存関係のインストールを開始します... ({packageQueue.Count}個のパッケージ)");
            
            // インストール状態を保存
            SaveInstallationState();
            
            EditorUtility.DisplayProgressBar("依存関係インストール", "インストール開始...", 0f);
            
            InstallNextPackage();
        }
        
        private static int skippedPackagesCount = 0;
        
        private static void InstallNextPackage()
        {
            if (packageQueue.Count == 0)
            {
                // 全てのパッケージインストール完了
                EditorUtility.ClearProgressBar();
                isInstallingPackages = false;
                
                // インストール状態をクリア
                ClearInstallationState();
                
                Debug.Log("依存関係のインストールが完了しました");
                
                var message = "依存関係のインストールが完了しました。\n\n";
                if (skippedPackagesCount > 0)
                {
                    message += $"注意: {skippedPackagesCount}個のパッケージは互換性の問題でスキップされました。\n\n";
                }
                
                message += "次の手順:\n" +
                          "1. NuGetForUnityが追加されました\n" +
                          "2. Window > NuGetForUnity を開いて 'R3' をインストール\n" +
                          "3. LicenseMasterを手動でインストール:\n" +
                          "   - https://github.com/syskentokyo/unitylicensemaster/releases\n" +
                          "   - UnityPackageをダウンロード・インポート\n" +
                          "4. 'Copy License Files'でライセンス管理開始";
                
                bool openLicenseMaster = EditorUtility.DisplayDialog("インストール完了", 
                    message + "\n\nLicenseMasterのダウンロードページを開きますか？", 
                    "ページを開く", "後で");
                
                if (openLicenseMaster)
                {
                    Application.OpenURL("https://github.com/syskentokyo/unitylicensemaster/releases");
                }
                
                ShowPostInstallInstructions();
                skippedPackagesCount = 0; // リセット
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
            
            Debug.Log($"[{currentIndex}/{totalPackagesToInstall}] インストール中: {packageName}");
            
            currentAddRequest = Client.Add(packageId);
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
            
            if (currentAddRequest.IsCompleted)
            {
                EditorApplication.update -= PackageInstallProgress;
                
                if (currentAddRequest.Status == StatusCode.Success)
                {
                    Debug.Log($"✓ インストール成功: {currentAddRequest.Result.displayName}");
                    
                    // 少し待ってから次のパッケージをインストール
                    EditorApplication.delayCall += () => {
                        System.Threading.Thread.Sleep(500); // 0.5秒待機
                        InstallNextPackage();
                    };
                }
                else
                {
                    var errorMessage = currentAddRequest.Error?.message ?? "Unknown error";
                    Debug.LogError($"パッケージインストールエラー: {errorMessage}");
                    
                    // Unity 6でのTextMeshPro互換性問題などを検出
                    bool isCompatibilityError = errorMessage.Contains("Cannot find a version") || 
                                               errorMessage.Contains("compatible with this Unity version");
                    
                    if (isCompatibilityError)
                    {
                        Debug.LogWarning($"⚠ 互換性の問題によりスキップしました");
                        skippedPackagesCount++;
                        
                        // 次のパッケージのインストールを継続
                        EditorApplication.delayCall += () => {
                            InstallNextPackage();
                        };
                    }
                    else
                    {
                        // 重大なエラーの場合は停止
                        EditorUtility.ClearProgressBar();
                        isInstallingPackages = false;
                        
                        // エラー時もインストール状態をクリア
                        ClearInstallationState();
                        
                        EditorUtility.DisplayDialog("インストールエラー", 
                            $"パッケージのインストールに失敗しました:\n{errorMessage}\n\n" +
                            "手動でインストールしてください。", "OK");
                    }
                }
                
                currentAddRequest = null;
            }
        }
        
        
        private static void ShowPostInstallInstructions()
        {
            Debug.Log("=== R3セットアップ手順 ===\n" +
                     "1. Window > NuGetForUnity を開く\n" +
                     "2. 'R3' を検索してインストール\n" +
                     "3. Unityを再起動");
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
                    Debug.Log($"=== パッケージインストールを再開します（残り: {state.remainingPackages.Count}個）===");
                    
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
        }
        
        private static void CancelInstallation()
        {
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