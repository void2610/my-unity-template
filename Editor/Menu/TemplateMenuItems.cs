using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Tools > Unity Template メニューのエントリポイント。処理は各サービスに委譲する
    /// </summary>
    public static class TemplateMenuItems
    {
        private const string MENU_ROOT = "Tools/Unity Template/";

        [MenuItem(MENU_ROOT + "Full Setup", false, 0)]
        public static void FullSetup()
        {
            FullSetupRunner.StartFullSetup();
        }

        [MenuItem(MENU_ROOT + "Install UPM Packages")]
        public static void InstallDependencies()
        {
            if (UpmPackageInstaller.IsInstalling)
            {
                bool cancel = EditorUtility.DisplayDialog("インストール中",
                    "依存関係のインストールが進行中です。\n\nキャンセルしますか？",
                    "キャンセル", "待機");

                if (cancel)
                {
                    UpmPackageInstaller.CancelInstallation();
                }
                return;
            }

            var config = TemplateConfig.Load();
            var currentManifest = TemplateConfig.LoadCurrentManifest();
            var packagesToInstall = UpmPackageInstaller.GetPackagesToInstall(config, currentManifest);

            if (packagesToInstall.Count == 0)
            {
                EditorUtility.DisplayDialog("依存関係",
                    "すべての依存関係は既にインストール済みです。", "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog("依存関係のインストール",
                $"以下の{packagesToInstall.Count}個のパッケージをインストールします:\n\n" +
                string.Join("\n", packagesToInstall.Take(5).Select(p => $"• {UpmPackageInstaller.GetPackageDisplayName(p)}")) +
                (packagesToInstall.Count > 5 ? $"\n...他{packagesToInstall.Count - 5}個" : "") +
                "\n\nこの処理には時間がかかる場合があります。\n続行しますか？",
                "インストール", "キャンセル");

            if (!proceed) return;

            UpmPackageInstaller.StartInstallation(packagesToInstall);
        }

        [MenuItem(MENU_ROOT + "Install NuGet Packages")]
        public static void InstallNugetPackages()
        {
            if (!NugetPackageService.IsNugetForUnityInstalled())
            {
                EditorUtility.DisplayDialog("NuGetForUnity未インストール",
                    "NuGetForUnityがインストールされていません。\n\n" +
                    "先に 'Install Dependencies' を実行して\nNuGetForUnityをインストールしてください。",
                    "OK");
                return;
            }

            var templatePackages = NugetPackageService.LoadTemplatePackages(TemplateConfig.Load());
            if (templatePackages == null || templatePackages.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー",
                    "NuGetパッケージテンプレートの読み込みに失敗しました。",
                    "OK");
                return;
            }

            var packagesConfigPath = NugetPackageService.GetPackagesConfigPath();
            if (string.IsNullOrEmpty(packagesConfigPath))
            {
                EditorUtility.DisplayDialog("エラー",
                    "NuGetForUnityの設定からpackages.configのパスを取得できませんでした。",
                    "OK");
                return;
            }

            var installedPackages = NugetPackageService.GetInstalledPackages(packagesConfigPath);
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

            var packageList = string.Join("\n", packagesToInstall.Select(p => $"• {p.Key} ({p.Value})"));
            bool proceed = EditorUtility.DisplayDialog("NuGetパッケージのインストール",
                $"以下の{packagesToInstall.Count}個のNuGetパッケージをインストールします:\n\n" +
                packageList +
                "\n\n依存パッケージも自動的にインストールされます。\n続行しますか？",
                "インストール", "キャンセル");

            if (!proceed) return;

            int successCount = 0;
            int failCount = 0;

            foreach (var package in packagesToInstall)
            {
                Debug.Log($"インストール中: {package.Key} ({package.Value})...");
                bool success = NugetPackageService.InstallPackage(package.Key, package.Value);
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

        [MenuItem(MENU_ROOT + "Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            var config = TemplateConfig.Load();
            int createdCount = ProjectStructureService.CreateFolderStructure(config);

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
            SetupSubmoduleByLinkName("Utils");
        }

        [MenuItem(MENU_ROOT + "Setup SettingsSystem Submodule")]
        public static void SetupSettingsSystemSubmodule()
        {
            SetupSubmoduleByLinkName("SettingsSystem");
        }

        private static void SetupSubmoduleByLinkName(string linkName)
        {
            var config = TemplateConfig.Load();
            var sub = config.submodules.FirstOrDefault(s => s.linkName == linkName);
            if (sub != null)
                SubmoduleService.SetupSubmodule(sub.name, sub.url, sub.linkName);
            else
                Debug.LogWarning($"{linkName} サブモジュールの設定が template-config.json に見つかりません");
        }

        [MenuItem(MENU_ROOT + "Setup Analyzers Submodule")]
        public static void SetupAnalyzersSubmodule()
        {
            var config = TemplateConfig.Load();
            SubmoduleService.SetupAnalyzersSubmodule(config.analyzers, interactive: true);
        }

        [MenuItem(MENU_ROOT + "Setup Repo Scaffolding")]
        public static void SetupRepoScaffolding()
        {
            RepoScaffoldingService.Setup(TemplateConfig.Load());
            AssetDatabase.Refresh();
        }

        [MenuItem(MENU_ROOT + "Copy Config Files")]
        public static void CopyConfigFiles()
        {
            ProjectStructureService.CopyConfigFilesInteractive();
        }

        [MenuItem(MENU_ROOT + "Copy License Files")]
        public static void CopyLicenseFiles()
        {
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

            var config = TemplateConfig.Load();
            int copiedCount = ProjectStructureService.CopyLicenseFilesFromTemplate(config.licenseFolderPath);

            AssetDatabase.Refresh();

            if (copiedCount > 0)
            {
                EditorUtility.DisplayDialog("ライセンスファイルコピー完了",
                    $"{copiedCount}個のライセンスファイルをコピーしました。\n" +
                    $"{config.licenseFolderPath}/フォルダで確認してください。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("ライセンスファイル",
                    "ライセンスファイルは既に存在しているか、テンプレートが見つかりません。", "OK");
            }
        }
    }
}
