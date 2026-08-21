using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// UPM パッケージの逐次インストール (ドメインリロード耐性のキュー管理)
    /// </summary>
    internal static class UpmPackageInstaller
    {
        private const string PREF_KEY_INSTALL_STATE = "UnityTemplate_InstallState";

        private static AddRequest currentAddRequest;
        private static Queue<string> packageQueue = new();
        private static int totalPackagesToInstall = 0;
        private static int skippedPackagesCount = 0;

        internal static bool IsInstalling { get; private set; }

        internal static List<string> GetPackagesToInstall(TemplateConfigData templateManifest, ManifestData currentManifest)
        {
            var packagesToInstall = new List<string>();

            // Unity 6で組み込みになったパッケージのリスト
            var unity6BuiltInPackages = new HashSet<string>
            {
                "com.unity.textmeshpro",
                "com.unity.ugui"
            };

            bool isUnity6OrNewer = Application.unityVersion.StartsWith("6") ||
                                  Application.unityVersion.CompareTo("6000") >= 0;

            foreach (var packageId in templateManifest.packages)
            {
                if (isUnity6OrNewer && unity6BuiltInPackages.Contains(packageId))
                {
                    continue;
                }

                if (!currentManifest.dependencies.ContainsKey(packageId))
                {
                    packagesToInstall.Add(packageId);
                }
            }

            foreach (var gitPackage in templateManifest.gitPackages)
            {
                // 完全な git URL または同じパスを持つパッケージのみスキップする厳密な重複チェック
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

        internal static string GetPackageDisplayName(string packageId)
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

        internal static void StartInstallation(List<string> packagesToInstall)
        {
            IsInstalling = true;
            skippedPackagesCount = 0;

            packageQueue.Clear();

            // NuGetForUnity を最初にインストール (後続の NuGet ステップが依存するため)
            var nugetPackage = packagesToInstall.FirstOrDefault(p => p.Contains("NuGetForUnity"));
            if (!string.IsNullOrEmpty(nugetPackage))
            {
                packageQueue.Enqueue(nugetPackage);
                packagesToInstall.Remove(nugetPackage);
            }

            foreach (var package in packagesToInstall)
            {
                packageQueue.Enqueue(package);
            }

            totalPackagesToInstall = packageQueue.Count;

            Debug.Log($"依存関係のインストールを開始します... ({packageQueue.Count}個のパッケージ)");

            SaveInstallationState();

            EditorUtility.DisplayProgressBar("依存関係インストール", "インストール開始...", 0f);

            InstallNextPackage();
        }

        private static void InstallNextPackage()
        {
            if (packageQueue.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                IsInstalling = false;

                ClearInstallationState();

                Debug.Log("✓ UPMパッケージのインストールが完了しました");

                // フルセットアップ中の場合は残りのステップへ継続
                if (FullSetupRunner.IsRunningOrPersisted)
                {
                    skippedPackagesCount = 0;
                    FullSetupRunner.ScheduleContinuationAfterUpm();
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
                skippedPackagesCount = 0;
                return;
            }

            var packageId = packageQueue.Dequeue();
            var packageName = GetPackageDisplayName(packageId);

            // キューから取り出した後に保存 (リロード時に同じパッケージを二重処理しない)
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

                    EditorApplication.delayCall += () => {
                        System.Threading.Thread.Sleep(500); // 連続 Client.Add の失敗を避けるための待機
                        InstallNextPackage();
                    };
                }
                else
                {
                    var errorMessage = currentAddRequest.Error?.message ?? "Unknown error";
                    Debug.LogError($"パッケージインストールエラー: {errorMessage}");

                    // Unity 6 での TextMeshPro 互換性問題などを検出
                    bool isCompatibilityError = errorMessage.Contains("Cannot find a version") ||
                                               errorMessage.Contains("compatible with this Unity version");

                    if (isCompatibilityError)
                    {
                        Debug.LogWarning($"⚠ 互換性の問題によりスキップしました");
                        skippedPackagesCount++;

                        EditorApplication.delayCall += () => {
                            InstallNextPackage();
                        };
                    }
                    else
                    {
                        EditorUtility.ClearProgressBar();
                        IsInstalling = false;

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

        /// <summary>
        /// ドメインリロード後にインストールキューを復元する
        /// </summary>
        internal static void RestoreStateAfterReload()
        {
            var stateJson = EditorPrefs.GetString(PREF_KEY_INSTALL_STATE, "");
            if (string.IsNullOrEmpty(stateJson))
            {
                // UPM キューは空だがフルセットアップが進行中なら残りのステップへ継続
                if (FullSetupRunner.IsRunning)
                {
                    FullSetupRunner.ScheduleContinuationAfterUpm();
                }
                return;
            }

            try
            {
                var state = JsonUtility.FromJson<InstallationState>(stateJson);
                if (state != null && state.isInstalling && state.remainingPackages.Count > 0)
                {
                    Debug.Log($"=== パッケージインストールを再開します（残り: {state.remainingPackages.Count}個）===");

                    packageQueue.Clear();
                    foreach (var package in state.remainingPackages)
                    {
                        packageQueue.Enqueue(package);
                    }

                    IsInstalling = true;
                    totalPackagesToInstall = state.totalPackages;

                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.DisplayProgressBar("依存関係インストール", "インストールを再開中...", 0f);
                        InstallNextPackage();
                    };
                }
                else if (FullSetupRunner.IsRunning)
                {
                    ClearInstallationState();
                    FullSetupRunner.ScheduleContinuationAfterUpm();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"インストール状態の復元に失敗しました: {e.Message}");
                ClearInstallationState();
                if (FullSetupRunner.IsRunning)
                {
                    FullSetupRunner.CleanupState();
                }
            }
        }

        private static void SaveInstallationState()
        {
            var state = new InstallationState
            {
                remainingPackages = packageQueue.ToList(),
                isInstalling = IsInstalling,
                totalPackages = totalPackagesToInstall
            };

            var stateJson = JsonUtility.ToJson(state);
            EditorPrefs.SetString(PREF_KEY_INSTALL_STATE, stateJson);
        }

        private static void ClearInstallationState()
        {
            EditorPrefs.DeleteKey(PREF_KEY_INSTALL_STATE);
        }

        internal static void CancelInstallation()
        {
            IsInstalling = false;
            packageQueue.Clear();
            currentAddRequest = null;

            EditorApplication.update -= PackageInstallProgress;
            EditorUtility.ClearProgressBar();

            ClearInstallationState();
            FullSetupRunner.CleanupState();

            EditorUtility.DisplayDialog("キャンセル完了",
                "依存関係のインストールをキャンセルしました。", "OK");
        }
    }
}
