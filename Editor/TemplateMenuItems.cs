using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml.Linq;

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
        private const string PREF_KEY_FULL_SETUP = "UnityTemplate_FullSetup";

        private static bool _isFullSetupRunning = false;
        private static AddRequest currentAddRequest;
        private static System.Collections.Generic.Queue<string> packageQueue = new();
        private static bool isInstallingPackages = false;
        private static int totalPackagesToInstall = 0;
        
        static TemplateMenuItems()
        {
            // ドメインリロード後の状態復元
            EditorApplication.delayCall += RestoreInstallationStateAfterReload;
        }
        
        
        [MenuItem(MENU_ROOT + "Full Setup", false, 0)]
        public static void FullSetup()
        {
            if (isInstallingPackages || _isFullSetupRunning)
            {
                EditorUtility.DisplayDialog("実行中",
                    "セットアップが進行中です。完了までお待ちください。", "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog("Full Setup",
                "以下の手順を一括で実行します:\n\n" +
                "1. フォルダ構成の作成\n" +
                "2. UPMパッケージのインストール\n" +
                "3. NuGetパッケージのインストール\n" +
                "4. 設定ファイルのコピー\n" +
                "5. Utils サブモジュールのセットアップ\n" +
                "6. SettingsSystem サブモジュールのセットアップ\n" +
                "7. Analyzers サブモジュールのセットアップ\n\n" +
                "※ 既存ファイルは上書きされます。\n" +
                "※ ドメインリロードが発生する場合があります。\n\n" +
                "続行しますか？",
                "開始", "キャンセル");

            if (!proceed) return;

            _isFullSetupRunning = true;
            EditorPrefs.SetBool(PREF_KEY_FULL_SETUP, true);

            Debug.Log("=== Full Setup を開始します ===");

            // ステップ1: フォルダ構成作成（同期）
            Debug.Log("[Full Setup 1/7] フォルダ構成を作成中...");
            var folders = new[]
            {
                "Assets/Scripts",
                "Assets/Sprites",
                "Assets/Audio/BGM",
                "Assets/Audio/SE",
                "Assets/Materials",
                "Assets/Prefabs",
                "Assets/ScriptableObjects",
                "Assets/Editor",
                "Assets/Others"
            };

            foreach (var folder in folders)
            {
                CreateFolderRecursively(folder);
            }
            AssetDatabase.Refresh();
            Debug.Log("✓ フォルダ構成の作成が完了しました");

            // ステップ2: UPMパッケージインストール（非同期）
            Debug.Log("[Full Setup 2/7] UPMパッケージのインストールを開始...");
            var templateManifest = LoadTemplateManifest();
            if (templateManifest == null)
            {
                Debug.LogError("テンプレートマニフェストの読み込みに失敗しました");
                CleanupFullSetupState();
                return;
            }

            var currentManifest = LoadCurrentManifest();
            var packagesToInstall = GetPackagesToInstall(templateManifest, currentManifest);

            if (packagesToInstall.Count > 0)
            {
                // UPMインストールが非同期で進む → 完了後にInstallNextPackageからContinueFullSetupAfterUpmが呼ばれる
                StartDependencyInstallation(packagesToInstall);
            }
            else
            {
                Debug.Log("✓ UPMパッケージはすべてインストール済みです");
                // UPM不要なら直接次のステップへ
                EditorApplication.delayCall += ContinueFullSetupAfterUpm;
            }
        }

        /// <summary>
        /// UPMインストール完了後に残りのセットアップを実行する
        /// </summary>
        private static void ContinueFullSetupAfterUpm()
        {
            if (!_isFullSetupRunning && !EditorPrefs.GetBool(PREF_KEY_FULL_SETUP, false))
                return;

            _isFullSetupRunning = true;

            try
            {
                // ステップ3: NuGetパッケージインストール
                Debug.Log("[Full Setup 3/7] NuGetパッケージのインストール中...");
                if (IsNugetForUnityInstalled())
                {
                    var templatePackages = LoadNugetTemplatePackages();
                    if (templatePackages != null && templatePackages.Count > 0)
                    {
                        var packagesConfigPath = GetNugetPackagesConfigPath();
                        if (!string.IsNullOrEmpty(packagesConfigPath))
                        {
                            var installedPackages = GetInstalledNugetPackages(packagesConfigPath);
                            var nugetToInstall = templatePackages
                                .Where(p => !installedPackages.ContainsKey(p.Key))
                                .ToList();

                            int nugetSuccess = 0;
                            int nugetFail = 0;
                            foreach (var package in nugetToInstall)
                            {
                                bool success = InstallNugetPackage(package.Key, package.Value);
                                if (success) nugetSuccess++;
                                else nugetFail++;
                            }

                            if (nugetToInstall.Count > 0)
                                Debug.Log($"✓ NuGetパッケージ: {nugetSuccess}個成功, {nugetFail}個失敗");
                            else
                                Debug.Log("✓ NuGetパッケージはすべてインストール済みです");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("⚠ NuGetForUnityが未インストールのため、NuGetパッケージのインストールをスキップしました");
                }

                // ステップ4: 設定ファイルコピー（上書き確認なし）
                Debug.Log("[Full Setup 4/7] 設定ファイルをコピー中...");
                var packagePath = GetPackagePath();
                if (packagePath != null)
                {
                    var configTemplatesPath = Path.Combine(packagePath, "ConfigTemplates");
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

                    var filesToCopy = new (string sourceName, string destPath)[]
                    {
                        ("Directory.Build.props", Path.Combine(projectRoot, "Directory.Build.props")),
                        ("csc.rsp", Path.Combine(Application.dataPath, "csc.rsp")),
                        (".editorconfig", Path.Combine(projectRoot, ".editorconfig")),
                        ("FormatCheck.csproj", Path.Combine(projectRoot, "FormatCheck.csproj"))
                    };

                    int configCopied = 0;
                    foreach (var (sourceName, destPath) in filesToCopy)
                    {
                        var sourcePath = Path.Combine(configTemplatesPath, sourceName);
                        try
                        {
                            if (File.Exists(sourcePath))
                            {
                                File.Copy(sourcePath, destPath, true);
                                Debug.Log($"  ✓ コピーしました: {sourceName}");
                                configCopied++;
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"  ✗ コピー失敗: {sourceName} - {e.Message}");
                        }
                    }
                    Debug.Log($"✓ 設定ファイル: {configCopied}個コピーしました");
                }

                // ステップ5: Utils サブモジュール
                Debug.Log("[Full Setup 5/7] Utils サブモジュールをセットアップ中...");
                SetupSubmodule("my-unity-utils", "https://github.com/void2610/my-unity-utils.git", "Utils");

                // ステップ6: SettingsSystem サブモジュール
                Debug.Log("[Full Setup 6/7] SettingsSystem サブモジュールをセットアップ中...");
                SetupSubmodule("my-unity-settings", "https://github.com/void2610/my-unity-settings.git", "SettingsSystem");

                // ステップ7: Analyzers サブモジュール
                Debug.Log("[Full Setup 7/7] Analyzers サブモジュールをセットアップ中...");
                SetupAnalyzersSubmoduleInternal();

                // 最終リフレッシュ
                AssetDatabase.Refresh();

                Debug.Log("=== Full Setup が完了しました ===");
                EditorUtility.DisplayDialog("Full Setup 完了",
                    "すべてのセットアップが完了しました！\n\n" +
                    "✓ フォルダ構成の作成\n" +
                    "✓ UPMパッケージのインストール\n" +
                    "✓ NuGetパッケージのインストール\n" +
                    "✓ 設定ファイルのコピー\n" +
                    "✓ Utils サブモジュール\n" +
                    "✓ SettingsSystem サブモジュール\n" +
                    "✓ Analyzers サブモジュール\n\n" +
                    "詳細はConsoleログを確認してください。",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Full Setup でエラーが発生しました: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Full Setup エラー",
                    $"セットアップ中にエラーが発生しました:\n{e.Message}\n\n" +
                    "詳細はConsoleログを確認してください。",
                    "OK");
            }
            finally
            {
                CleanupFullSetupState();
            }
        }

        /// <summary>
        /// Analyzersサブモジュールの内部セットアップ（Full Setup用）
        /// </summary>
        private static void SetupAnalyzersSubmoduleInternal()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var submoduleName = "unity-analyzers";
            var repoUrl = "https://github.com/void2610/unity-analyzers.git";
            var submodulePath = Path.Combine(projectRoot, submoduleName);

            if (IsSubmoduleRegistered(submoduleName))
            {
                Debug.Log($"✓ {submoduleName} サブモジュールは既に登録されています");
                ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                BuildAnalyzerDll(projectRoot, submoduleName);
                return;
            }

            if (Directory.Exists(submodulePath))
            {
                try
                {
                    Directory.Delete(submodulePath, true);
                    Debug.Log($"既存のディレクトリを削除しました: {submodulePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"ディレクトリの削除に失敗しました: {e.Message}");
                    return;
                }
            }

            CleanupGitModules(submoduleName);

            Debug.Log($"{submoduleName} をサブモジュールとして追加中...");
            var exitCode = ExecuteGitCommandSync(projectRoot, $"submodule add {repoUrl} {submoduleName}");

            if (exitCode != 0)
            {
                Debug.LogError("Analyzers Submoduleの追加に失敗しました");
                return;
            }

            Debug.Log($"✓ {submoduleName} サブモジュールを追加しました");
            ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
            BuildAnalyzerDll(projectRoot, submoduleName);
        }

        /// <summary>
        /// フルセットアップ状態をクリアする
        /// </summary>
        private static void CleanupFullSetupState()
        {
            _isFullSetupRunning = false;
            EditorPrefs.DeleteKey(PREF_KEY_FULL_SETUP);
        }

        [MenuItem(MENU_ROOT + "Install UPM Packages")]
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

        [MenuItem(MENU_ROOT + "Install NuGet Packages")]
        public static void InstallNugetPackages()
        {
            // NuGetForUnityがインストールされているか確認
            if (!IsNugetForUnityInstalled())
            {
                EditorUtility.DisplayDialog("NuGetForUnity未インストール",
                    "NuGetForUnityがインストールされていません。\n\n" +
                    "先に 'Install Dependencies' を実行して\nNuGetForUnityをインストールしてください。",
                    "OK");
                return;
            }

            // テンプレートからパッケージリストを読み込み
            var templatePackages = LoadNugetTemplatePackages();
            if (templatePackages == null || templatePackages.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー",
                    "NuGetパッケージテンプレートの読み込みに失敗しました。",
                    "OK");
                return;
            }

            // packages.configのパスをNuGetForUnityの設定から取得
            var packagesConfigPath = GetNugetPackagesConfigPath();
            if (string.IsNullOrEmpty(packagesConfigPath))
            {
                EditorUtility.DisplayDialog("エラー",
                    "NuGetForUnityの設定からpackages.configのパスを取得できませんでした。",
                    "OK");
                return;
            }

            // インストール済みパッケージを確認
            var installedPackages = GetInstalledNugetPackages(packagesConfigPath);
            var packagesToInstall = templatePackages
                .Where(p => !installedPackages.ContainsKey(p.Key))
                .ToList();

            if (packagesToInstall.Count == 0)
            {
                EditorUtility.DisplayDialog("NuGetパッケージ",
                    "すべてのNuGetパッケージは既にインストール済みです。",
                    "OK");
                return;
            }

            // 確認ダイアログ
            var packageList = string.Join("\n", packagesToInstall.Select(p => $"• {p.Key} ({p.Value})"));
            bool proceed = EditorUtility.DisplayDialog("NuGetパッケージのインストール",
                $"以下の{packagesToInstall.Count}個のNuGetパッケージをインストールします:\n\n" +
                packageList +
                "\n\n依存パッケージも自動的にインストールされます。\n続行しますか？",
                "インストール", "キャンセル");

            if (!proceed) return;

            // NugetPackageInstaller.InstallIdentifierを使用してインストール（依存関係含む）
            int successCount = 0;
            int failCount = 0;

            foreach (var package in packagesToInstall)
            {
                Debug.Log($"インストール中: {package.Key} ({package.Value})...");
                bool success = InstallNugetPackage(package.Key, package.Value);
                if (success)
                {
                    Debug.Log($"✓ インストール成功: {package.Key}");
                    successCount++;
                }
                else
                {
                    Debug.LogError($"✗ インストール失敗: {package.Key}");
                    failCount++;
                }
            }

            // 結果表示
            string resultMessage;
            if (failCount == 0)
            {
                resultMessage = $"{successCount}個のNuGetパッケージをインストールしました。\n\n" +
                    "依存パッケージも含めてインストールされています。\n" +
                    "Window > NuGet > Manage NuGet Packages で\nインストール状況を確認できます。";
            }
            else
            {
                resultMessage = $"成功: {successCount}個\n失敗: {failCount}個\n\n" +
                    "失敗したパッケージはConsoleログを確認してください。";
            }

            EditorUtility.DisplayDialog("インストール完了", resultMessage, "OK");
        }

        /// <summary>
        /// NuGetForUnityがインストールされているか確認
        /// </summary>
        private static bool IsNugetForUnityInstalled()
        {
            return System.AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a => a.GetName().Name.Contains("NuGetForUnity"));
        }

        /// <summary>
        /// NuGetテンプレートからパッケージリストを読み込み
        /// </summary>
        /// <returns>パッケージID -> バージョンのDictionary</returns>
        private static Dictionary<string, string> LoadNugetTemplatePackages()
        {
            var packagePath = GetPackagePath();
            if (packagePath == null) return null;

            var templatePath = Path.Combine(packagePath, "NugetTemplates", "packages.config.template");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"NuGetテンプレートが見つかりません: {templatePath}");
                return null;
            }

            try
            {
                var packages = new Dictionary<string, string>();
                var doc = XDocument.Load(templatePath);
                var packageElements = doc.Root?.Elements("package");

                if (packageElements == null) return packages;

                foreach (var element in packageElements)
                {
                    var id = element.Attribute("id")?.Value;
                    var version = element.Attribute("version")?.Value;

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                    {
                        packages[id] = version;
                    }
                }

                return packages;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NuGetテンプレートの読み込みに失敗しました: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// インストール済みのNuGetパッケージを取得
        /// </summary>
        private static Dictionary<string, string> GetInstalledNugetPackages(string packagesConfigPath)
        {
            var packages = new Dictionary<string, string>();

            if (!File.Exists(packagesConfigPath))
            {
                return packages;
            }

            try
            {
                var doc = XDocument.Load(packagesConfigPath);
                var packageElements = doc.Root?.Elements("package");

                if (packageElements == null) return packages;

                foreach (var element in packageElements)
                {
                    var id = element.Attribute("id")?.Value;
                    var version = element.Attribute("version")?.Value;

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                    {
                        packages[id] = version;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"packages.configの読み込みに失敗しました: {e.Message}");
            }

            return packages;
        }

        /// <summary>
        /// packages.configにパッケージを追加
        /// </summary>
        private static bool MergePackagesConfig(string packagesConfigPath, List<KeyValuePair<string, string>> packagesToAdd)
        {
            try
            {
                XDocument doc;

                if (File.Exists(packagesConfigPath))
                {
                    doc = XDocument.Load(packagesConfigPath);
                }
                else
                {
                    // 新規作成
                    doc = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("packages")
                    );
                }

                var root = doc.Root;
                if (root == null)
                {
                    root = new XElement("packages");
                    doc.Add(root);
                }

                // パッケージを追加
                foreach (var package in packagesToAdd)
                {
                    var element = new XElement("package",
                        new XAttribute("id", package.Key),
                        new XAttribute("version", package.Value),
                        new XAttribute("manuallyInstalled", "true")
                    );
                    root.Add(element);
                    Debug.Log($"packages.configに追加: {package.Key} ({package.Value})");
                }

                // 保存
                doc.Save(packagesConfigPath);
                Debug.Log($"✓ packages.configを保存しました: {packagesConfigPath}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"packages.configの更新に失敗しました: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// NugetPackageInstaller.Installを呼び出してパッケージをインストール（Reflection使用）
        /// 依存関係も含めてインストールされる
        /// </summary>
        private static bool InstallNugetPackage(string packageId, string version)
        {
            try
            {
                var allTypes = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .ToArray();

                // NugetPackageIdentifierクラスを探す
                var packageIdentifierType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.NugetPackageIdentifier");

                if (packageIdentifierType == null)
                {
                    Debug.LogWarning("NugetPackageIdentifier クラスが見つかりません");
                    return false;
                }

                // NugetPackageIdentifierのインスタンスを作成
                var constructor = packageIdentifierType.GetConstructor(new[] { typeof(string), typeof(string) });
                if (constructor == null)
                {
                    Debug.LogWarning("NugetPackageIdentifier コンストラクタが見つかりません");
                    return false;
                }

                var packageIdentifier = constructor.Invoke(new object[] { packageId, version });
                Debug.Log($"パッケージ識別子を作成: {packageId} ({version})");

                // PackageCacheManagerクラスを探してパッケージを取得
                var cacheManagerType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.PackageCacheManager");

                if (cacheManagerType == null)
                {
                    Debug.LogWarning("PackageCacheManager クラスが見つかりません");
                    return false;
                }

                // INugetPackageIdentifierインターフェースを探す
                var identifierInterfaceType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.INugetPackageIdentifier");

                if (identifierInterfaceType == null)
                {
                    Debug.LogWarning("INugetPackageIdentifier インターフェースが見つかりません");
                    return false;
                }

                // GetPackageFromCacheOrSourceメソッドを探す（internalメソッド）
                var getPackageMethod = cacheManagerType.GetMethod("GetPackageFromCacheOrSource",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new System.Type[] { identifierInterfaceType },
                    null);

                if (getPackageMethod == null)
                {
                    Debug.LogWarning("GetPackageFromCacheOrSource メソッドが見つかりません");
                    return false;
                }

                // パッケージを取得
                var nugetPackage = getPackageMethod.Invoke(null, new object[] { packageIdentifier });
                if (nugetPackage == null)
                {
                    Debug.LogWarning($"パッケージが見つかりません: {packageId}");
                    return false;
                }

                Debug.Log($"パッケージを取得: {nugetPackage}");

                // INugetPackageインターフェースを探す
                var packageInterfaceType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.INugetPackage");

                if (packageInterfaceType == null)
                {
                    Debug.LogWarning("INugetPackage インターフェースが見つかりません");
                    return false;
                }

                // NugetPackageInstallerクラスを探す
                var installerType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.NugetPackageInstaller");

                if (installerType == null)
                {
                    Debug.LogWarning("NugetPackageInstaller クラスが見つかりません");
                    return false;
                }

                // internal Install(INugetPackage package, bool refreshAssets, bool isSlimRestoreInstall, bool allowUpdateForExplicitlyInstalled)メソッドを探す
                var installMethod = installerType.GetMethod("Install",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new System.Type[] { packageInterfaceType, typeof(bool), typeof(bool), typeof(bool) },
                    null);

                if (installMethod == null)
                {
                    Debug.LogWarning("Install メソッドが見つかりません");
                    // 全メソッドを出力してデバッグ
                    var methods = installerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        Debug.Log($"  利用可能なメソッド: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                    }
                    return false;
                }

                // Install(package, refreshAssets: true, isSlimRestoreInstall: false, allowUpdateForExplicitlyInstalled: true)
                Debug.Log($"Installメソッドを呼び出します...");
                var result = installMethod.Invoke(null, new object[] { nugetPackage, true, false, true });

                // 結果を確認
                if (result != null)
                {
                    var successProperty = result.GetType().GetProperty("Successful");
                    if (successProperty != null)
                    {
                        var success = (bool)successProperty.GetValue(result);
                        Debug.Log($"インストール結果: {(success ? "成功" : "失敗")}");
                        return success;
                    }
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"パッケージのインストールに失敗しました ({packageId}): {e.Message}");
                if (e.InnerException != null)
                {
                    Debug.LogError($"内部エラー: {e.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// NuGetForUnityの設定からpackages.configのパスを取得（Reflection使用）
        /// </summary>
        private static string GetNugetPackagesConfigPath()
        {
            try
            {
                // ConfigurationManagerクラスを探す
                var configManagerType = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Configuration.ConfigurationManager");

                if (configManagerType == null)
                {
                    Debug.LogWarning("ConfigurationManager クラスが見つかりません");
                    return null;
                }

                // NugetConfigFileプロパティを取得
                var nugetConfigFileProperty = configManagerType.GetProperty("NugetConfigFile",
                    BindingFlags.Public | BindingFlags.Static);

                if (nugetConfigFileProperty == null)
                {
                    Debug.LogWarning("NugetConfigFile プロパティが見つかりません");
                    return null;
                }

                var nugetConfigFile = nugetConfigFileProperty.GetValue(null);
                if (nugetConfigFile == null)
                {
                    Debug.LogWarning("NugetConfigFile の値が null です");
                    return null;
                }

                // PackagesConfigFilePathプロパティを取得
                var packagesConfigFilePathProperty = nugetConfigFile.GetType().GetProperty("PackagesConfigFilePath",
                    BindingFlags.Public | BindingFlags.Instance);

                if (packagesConfigFilePathProperty == null)
                {
                    Debug.LogWarning("PackagesConfigFilePath プロパティが見つかりません");
                    return null;
                }

                var path = packagesConfigFilePathProperty.GetValue(nugetConfigFile) as string;
                Debug.Log($"NuGetForUnity packages.config パス: {path}");
                return path;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"packages.configパスの取得に失敗しました: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// PackageRestorer.Restoreを呼び出し（Reflection使用）
        /// </summary>
        private static bool RestoreNugetPackages()
        {
            try
            {
                // PackageRestorerクラスを探す
                var packageRestorerType = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.PackageRestorer");

                if (packageRestorerType == null)
                {
                    Debug.LogWarning("PackageRestorer クラスが見つかりません");
                    return false;
                }

                // Restore(bool slimRestore)メソッドを探す
                var restoreMethod = packageRestorerType.GetMethod("Restore",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(bool) },
                    null);

                if (restoreMethod == null)
                {
                    Debug.LogWarning("PackageRestorer.Restore メソッドが見つかりません");
                    return false;
                }

                // Restore(false)を呼び出し（依存関係も含めて完全復元）
                restoreMethod.Invoke(null, new object[] { false });
                Debug.Log("✓ NuGetForUnity Restore を実行しました");

                // AssetDatabaseをリフレッシュしてDLLを認識させる
                AssetDatabase.Refresh();
                Debug.Log("✓ AssetDatabase をリフレッシュしました");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NuGetForUnity Restore の呼び出しに失敗しました: {e.Message}");
                return false;
            }
        }


        [MenuItem(MENU_ROOT + "Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            var folders = new[]
            {
                "Assets/Scripts",
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

        [MenuItem(MENU_ROOT + "Setup Utils Submodule")]
        public static void SetupUtilsSubmodule()
        {
            SetupSubmodule("my-unity-utils", "https://github.com/void2610/my-unity-utils.git", "Utils");
        }

        [MenuItem(MENU_ROOT + "Setup SettingsSystem Submodule")]
        public static void SetupSettingsSystemSubmodule()
        {
            SetupSubmodule("my-unity-settings", "https://github.com/void2610/my-unity-settings.git", "SettingsSystem");
        }

        [MenuItem(MENU_ROOT + "Setup Analyzers Submodule")]
        public static void SetupAnalyzersSubmodule()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var submoduleName = "unity-analyzers";
            var repoUrl = "https://github.com/void2610/unity-analyzers.git";
            var submodulePath = Path.Combine(projectRoot, submoduleName);

            // 1. サブモジュール登録済みチェック
            if (IsSubmoduleRegistered(submoduleName))
            {
                Debug.Log($"✓ {submoduleName} サブモジュールは既に登録されています");
                ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                BuildAnalyzerDll(projectRoot, submoduleName);
                return;
            }

            // 2. 既存ディレクトリの処理
            if (Directory.Exists(submodulePath))
            {
                var isGitRepo = IsGitRepository(submodulePath);
                var message = isGitRepo
                    ? $"{submoduleName} ディレクトリが既にgitリポジトリとして存在しています。\n\n削除してsubmoduleとして再追加しますか?"
                    : $"{submoduleName} ディレクトリが既に存在しています。\n\n削除してsubmoduleとして追加しますか?";

                if (!EditorUtility.DisplayDialog("確認", message, "削除して追加", "キャンセル"))
                {
                    Debug.Log("Analyzers サブモジュールのセットアップがキャンセルされました");
                    return;
                }

                try
                {
                    Directory.Delete(submodulePath, true);
                    Debug.Log($"既存のディレクトリを削除しました: {submodulePath}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("エラー", $"ディレクトリの削除に失敗しました:\n{e.Message}", "OK");
                    return;
                }
            }

            // 3. .git/modules クリーンアップ
            CleanupGitModules(submoduleName);

            // 4. Git submoduleを追加
            Debug.Log($"{submoduleName} をサブモジュールとして追加中...");
            var exitCode = ExecuteGitCommandSync(projectRoot, $"submodule add {repoUrl} {submoduleName}");

            if (exitCode != 0)
            {
                EditorUtility.DisplayDialog("エラー",
                    "Analyzers Submoduleの追加に失敗しました。\nGitリポジトリが初期化されているか確認してください。",
                    "OK");
                return;
            }

            Debug.Log($"✓ {submoduleName} サブモジュールを追加しました");

            // 5. submodule update
            ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");

            // 6. アナライザーDLLをビルド
            BuildAnalyzerDll(projectRoot, submoduleName);
        }

        /// <summary>
        /// アナライザーDLLをビルドする
        /// </summary>
        /// <param name="projectRoot">Unityプロジェクトルート</param>
        /// <param name="submoduleName">サブモジュール名</param>
        private static void BuildAnalyzerDll(string projectRoot, string submoduleName)
        {
            var analyzerProjectPath = Path.Combine(projectRoot, submoduleName, "src", "Void2610.Unity.Analyzers");

            if (!Directory.Exists(analyzerProjectPath))
            {
                if (!_isFullSetupRunning)
                {
                    EditorUtility.DisplayDialog("警告",
                        $"アナライザープロジェクトが見つかりません:\n{analyzerProjectPath}\n\n" +
                        "Submoduleは追加されましたが、DLLのビルドはスキップされました。",
                        "OK");
                }
                else
                {
                    Debug.LogWarning($"⚠ アナライザープロジェクトが見つかりません: {analyzerProjectPath}");
                }
                return;
            }

            Debug.Log("アナライザーDLLをビルド中...");
            var exitCode = ExecuteShellCommand("dotnet", $"build \"{analyzerProjectPath}\" -c Release");

            if (exitCode == 0)
            {
                Debug.Log("✓ アナライザーDLLのビルドが完了しました");
                if (!_isFullSetupRunning)
                {
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("セットアップ完了",
                        "Analyzers のセットアップが完了しました！\n\n" +
                        $"✓ Submodule追加: {submoduleName}/\n" +
                        "✓ アナライザーDLLビルド完了\n\n" +
                        "Directory.Build.propsにアナライザー参照が設定されていれば、\n" +
                        "IDEでカスタムアナライザーの警告が表示されるようになります。\n\n" +
                        "「Copy Config Files」でDirectory.Build.propsを\nコピーしてください。",
                        "OK");
                }
            }
            else
            {
                if (!_isFullSetupRunning)
                {
                    EditorUtility.DisplayDialog("警告",
                        "アナライザーDLLのビルドに失敗しました。\n\n" +
                        "Submoduleは追加されましたが、手動でビルドが必要です:\n" +
                        $"cd {analyzerProjectPath}\n" +
                        "dotnet build -c Release",
                        "OK");
                }
                else
                {
                    Debug.LogWarning("⚠ アナライザーDLLのビルドに失敗しました。手動でビルドしてください。");
                }
            }
        }

        /// <summary>
        /// 汎用サブモジュールセットアップ
        /// </summary>
        /// <param name="submoduleName">サブモジュール名（リポジトリルートのフォルダ名）</param>
        /// <param name="repoUrl">GitリポジトリURL</param>
        /// <param name="linkName">Assets/Scripts/以下のシンボリックリンク名 兼 表示名</param>
        private static void SetupSubmodule(string submoduleName, string repoUrl, string linkName)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var submodulePath = Path.Combine(projectRoot, submoduleName);

            // 1. Assets/Scriptsフォルダ作成
            if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
            {
                CreateFolderRecursively("Assets/Scripts");
                AssetDatabase.Refresh();
            }

            // 2. submoduleとして既に登録済みか確認
            if (IsSubmoduleRegistered(submoduleName))
            {
                Debug.Log($"✓ {linkName} submodule already registered");
                ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                CreateSubmoduleSymbolicLinkIfNeeded(submoduleName, linkName);
                return;
            }

            // 3. ワーキングディレクトリが存在する場合の処理
            if (Directory.Exists(submodulePath))
            {
                if (!_isFullSetupRunning)
                {
                    bool isGitRepo = IsGitRepository(submodulePath);
                    string message = isGitRepo
                        ? $"{submoduleName} ディレクトリが既にgitリポジトリとして存在しています。\n\n削除してsubmoduleとして再追加しますか?"
                        : $"{submoduleName} ディレクトリが既に存在しています。\n\n削除してsubmoduleとして追加しますか?";

                    if (!EditorUtility.DisplayDialog("確認", message, "削除して追加", "キャンセル"))
                    {
                        Debug.Log($"{linkName} submodule setup cancelled by user");
                        return;
                    }
                }

                try
                {
                    Directory.Delete(submodulePath, true);
                    Debug.Log($"Deleted existing directory: {submodulePath}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("エラー", $"ディレクトリの削除に失敗しました:\n{e.Message}", "OK");
                    return;
                }
            }

            // 4. .git/modules のクリーンアップ
            CleanupGitModules(submoduleName);

            // 5. Git submoduleを追加
            Debug.Log($"Adding {submoduleName} as submodule...");
            int exitCode = ExecuteGitCommandSync(projectRoot, $"submodule add {repoUrl} {submoduleName}");

            if (exitCode == 0)
            {
                Debug.Log($"✓ {linkName} submodule added");
                ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                CreateSubmoduleSymbolicLinkIfNeeded(submoduleName, linkName);
            }
            else
            {
                Debug.LogError($"{linkName} Submoduleの追加に失敗しました");
                if (!_isFullSetupRunning)
                {
                    EditorUtility.DisplayDialog("エラー",
                        $"{linkName} Submoduleの追加に失敗しました。\nGitリポジトリが初期化されているか確認してください。",
                        "OK");
                }
            }
        }

        [MenuItem(MENU_ROOT + "Copy Config Files")]
        public static void CopyConfigFiles()
        {
            var packagePath = GetPackagePath();

            var configTemplatesPath = Path.Combine(packagePath, "ConfigTemplates");
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            // コピー対象ファイルの定義（ソースファイル名, コピー先パス）
            var filesToCopy = new (string sourceName, string destPath)[]
            {
                ("Directory.Build.props", Path.Combine(projectRoot, "Directory.Build.props")),
                ("csc.rsp", Path.Combine(Application.dataPath, "csc.rsp")),
                (".editorconfig", Path.Combine(projectRoot, ".editorconfig")),
                ("FormatCheck.csproj", Path.Combine(projectRoot, "FormatCheck.csproj"))
            };

            var copiedCount = 0;
            var skippedCount = 0;

            foreach (var (sourceName, destPath) in filesToCopy)
            {
                var sourcePath = Path.Combine(configTemplatesPath, sourceName);
                var shouldCopy = CopyConfigFile(sourcePath, destPath, sourceName, ref skippedCount);
                if (shouldCopy)
                {
                    copiedCount++;
                }
            }

            AssetDatabase.Refresh();
            ShowCopyConfigFilesResult(copiedCount, skippedCount);
        }

        /// <summary>
        /// 設定ファイルを個別にコピーする
        /// </summary>
        /// <param name="sourcePath">コピー元パス</param>
        /// <param name="destPath">コピー先パス</param>
        /// <param name="fileName">ファイル名（表示用）</param>
        /// <param name="skippedCount">スキップカウント（参照）</param>
        /// <returns>コピーした場合true</returns>
        private static bool CopyConfigFile(string sourcePath, string destPath, string fileName, ref int skippedCount)
        {
            if (File.Exists(destPath) && !_isFullSetupRunning)
            {
                var overwrite = EditorUtility.DisplayDialog("ファイルが既に存在します",
                    $"{fileName} は既に存在しています。\n\n上書きしますか？",
                    "上書き", "スキップ");

                if (!overwrite)
                {
                    Debug.Log($"スキップしました: {fileName}");
                    skippedCount++;
                    return false;
                }
            }

            try
            {
                File.Copy(sourcePath, destPath, true);
                Debug.Log($"✓ コピーしました: {fileName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ファイルのコピーに失敗しました: {fileName}\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 設定ファイルコピー結果を表示する
        /// </summary>
        /// <param name="copiedCount">コピーしたファイル数</param>
        /// <param name="skippedCount">スキップしたファイル数</param>
        private static void ShowCopyConfigFilesResult(int copiedCount, int skippedCount)
        {
            if (_isFullSetupRunning) return;

            string message;

            if (copiedCount > 0 && skippedCount > 0)
            {
                message = $"{copiedCount}個のファイルをコピーしました。\n{skippedCount}個のファイルをスキップしました。";
            }
            else if (copiedCount > 0)
            {
                message = $"{copiedCount}個の設定ファイルをコピーしました。\n\n" +
                          "• Directory.Build.props: プロジェクトルート\n" +
                          "• csc.rsp: Assets/\n" +
                          "• .editorconfig: プロジェクトルート\n" +
                          "• FormatCheck.csproj: プロジェクトルート";
            }
            else if (skippedCount > 0)
            {
                message = "すべてのファイルをスキップしました。";
            }
            else
            {
                message = "コピーするファイルがありませんでした。";
            }

            EditorUtility.DisplayDialog("設定ファイルコピー", message, "OK");
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

                Debug.Log("✓ UPMパッケージのインストールが完了しました");

                // フルセットアップ中の場合は残りのステップへ継続
                if (_isFullSetupRunning || EditorPrefs.GetBool(PREF_KEY_FULL_SETUP, false))
                {
                    _isFullSetupRunning = true;
                    skippedPackagesCount = 0;
                    EditorApplication.delayCall += ContinueFullSetupAfterUpm;
                    return;
                }

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
            // フルセットアップフラグの復元
            if (EditorPrefs.GetBool(PREF_KEY_FULL_SETUP, false))
            {
                _isFullSetupRunning = true;
            }

            // EditorPrefsから状態を復元
            var stateJson = EditorPrefs.GetString(PREF_KEY_INSTALL_STATE, "");
            if (string.IsNullOrEmpty(stateJson))
            {
                // UPMキューが空だがフルセットアップが進行中の場合、残りのステップへ継続
                if (_isFullSetupRunning)
                {
                    EditorApplication.delayCall += ContinueFullSetupAfterUpm;
                }
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
                else if (_isFullSetupRunning)
                {
                    // UPMキューは空だがフルセットアップが進行中
                    ClearInstallationState();
                    EditorApplication.delayCall += ContinueFullSetupAfterUpm;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"インストール状態の復元に失敗しました: {e.Message}");
                ClearInstallationState();
                if (_isFullSetupRunning)
                {
                    CleanupFullSetupState();
                }
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
            CleanupFullSetupState();

            EditorUtility.DisplayDialog("キャンセル完了",
                "依存関係のインストールをキャンセルしました。", "OK");
        }

        /// <summary>
        /// Gitコマンドを同期実行する
        /// </summary>
        /// <param name="workingDirectory">作業ディレクトリ</param>
        /// <param name="arguments">Gitコマンドの引数</param>
        /// <returns>終了コード (0=成功)</returns>
        private static int ExecuteGitCommandSync(string workingDirectory, string arguments)
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Debug.Log($"Git: {output}");
                    }

                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"Git error: {error}");
                    }

                    return process.ExitCode;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Git command failed: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// シンボリックリンクを作成する
        /// </summary>
        /// <param name="linkPath">リンクのパス</param>
        /// <param name="targetPath">ターゲットのパス（相対パス）</param>
        /// <returns>成功した場合true</returns>
        private static bool CreateSymlink(string linkPath, string targetPath)
        {
            try
            {
#if UNITY_EDITOR_WIN
                // Windows: Junction（管理者権限不要）
                var args = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"";
                return ExecuteShellCommand("cmd.exe", args) == 0;
#else
                // macOS/Linux: シンボリックリンク
                return ExecuteShellCommand("ln", $"-s \"{targetPath}\" \"{linkPath}\"") == 0;
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create symlink: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// シェルコマンドを実行する
        /// </summary>
        /// <param name="command">コマンド</param>
        /// <param name="arguments">引数</param>
        /// <returns>終了コード (0=成功)</returns>
        private static int ExecuteShellCommand(string command, string arguments)
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();
                    var error = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"Command error: {error}");
                    }

                    return process.ExitCode;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Command execution failed: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// submoduleとして既に登録されているか確認
        /// </summary>
        private static bool IsSubmoduleRegistered(string submoduleName)
        {
            // .gitmodulesファイルの存在確認
            var gitModulesPath = Path.Combine(Application.dataPath, "..", ".gitmodules");
            if (!File.Exists(gitModulesPath))
                return false;

            // .gitmodulesファイルの内容を確認
            var content = File.ReadAllText(gitModulesPath);
            return content.Contains($"[submodule \"{submoduleName}\"]");
        }

        /// <summary>
        /// 指定パスがgitリポジトリか確認
        /// </summary>
        private static bool IsGitRepository(string path)
        {
            if (!Directory.Exists(path))
                return false;

            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir) || File.Exists(gitDir);
        }

        /// <summary>
        /// .git/modules内の不要なsubmoduleデータを削除
        /// </summary>
        private static void CleanupGitModules(string submoduleName)
        {
            var modulesPath = Path.Combine(Application.dataPath, "..", ".git", "modules", submoduleName);
            if (Directory.Exists(modulesPath))
            {
                Debug.Log($"Cleaning up stale git modules: {modulesPath}");
                try
                {
                    Directory.Delete(modulesPath, true);
                    Debug.Log("✓ Cleanup completed");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to cleanup git modules: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 汎用シンボリックリンク作成
        /// </summary>
        /// <param name="submoduleName">サブモジュール名（リポジトリルートのフォルダ名）</param>
        /// <param name="linkName">Assets/Scripts/以下のシンボリックリンク名 兼 表示名</param>
        private static void CreateSubmoduleSymbolicLinkIfNeeded(string submoduleName, string linkName)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var submodulePath = Path.Combine(projectRoot, submoduleName);
            var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            var symlinkPath = Path.Combine(scriptsPath, linkName);

            // シンボリックリンクの存在確認
            if (Directory.Exists(symlinkPath) || File.Exists(symlinkPath))
            {
                try
                {
                    var realPath = Path.GetFullPath(symlinkPath);
                    var expectedPath = Path.GetFullPath(submodulePath);

                    if (realPath.Equals(expectedPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"✓ {linkName} symbolic link already exists and is correct");
                        ShowSubmoduleSetupCompletedDialog(submoduleName, linkName);
                        return;
                    }
                }
                catch
                {
                    // リンク先の取得に失敗した場合は既存ファイル/ディレクトリとして扱う
                }

                EditorUtility.DisplayDialog("警告",
                    $"Assets/Scripts/{linkName} は既に存在しています。\n\n" +
                    "シンボリックリンク作成をスキップしました。\n" +
                    "手動で削除してから再実行してください。",
                    "OK");
                return;
            }

            Debug.Log($"Creating {linkName} symbolic link...");
            var relativePath = Path.Combine("..", "..", submoduleName);
            bool symlinkCreated = CreateSymlink(symlinkPath, relativePath);

            if (symlinkCreated)
            {
                Debug.Log($"✓ Symbolic link created: Assets/Scripts/{linkName} -> {submoduleName}");
                if (!_isFullSetupRunning)
                {
                    AssetDatabase.Refresh();
                }
                ShowSubmoduleSetupCompletedDialog(submoduleName, linkName);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー",
                    "シンボリックリンクの作成に失敗しました。\n\n" +
                    "Windows: 管理者権限が必要な場合があります\n" +
                    "macOS/Linux: ターミナルで手動実行してください\n\n" +
                    "手動コマンド:\n" +
                    $"ln -s ../../{submoduleName} Assets/Scripts/{linkName}",
                    "OK");
            }
        }

        /// <summary>
        /// 汎用セットアップ完了ダイアログを表示
        /// </summary>
        private static void ShowSubmoduleSetupCompletedDialog(string submoduleName, string linkName)
        {
            if (_isFullSetupRunning) return;

            EditorUtility.DisplayDialog("セットアップ完了",
                $"{linkName} のセットアップが完了しました！\n\n" +
                $"✓ Submodule追加: {submoduleName}/\n" +
                $"✓ シンボリックリンク作成: Assets/Scripts/{linkName}/\n\n" +
                $"{linkName}スクリプトが利用可能になりました。",
                "OK");
        }
    }
}