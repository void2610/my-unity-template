using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Full Setup の 1 ステップ (タイトル + 実行処理)
    /// </summary>
    internal sealed class SetupStep
    {
        internal string Title { get; }
        internal System.Action<TemplateConfigData> Run { get; }

        internal SetupStep(string title, System.Action<TemplateConfigData> run)
        {
            Title = title;
            Run = run;
        }
    }

    /// <summary>
    /// Full Setup のオーケストレーション。
    /// UPM インストールのみ非同期 (ドメインリロードを跨ぐ) ため、フローは
    /// 「同期プレステップ → UPM → 同期ポストステップ」の 3 区間に分かれる。
    /// </summary>
    [InitializeOnLoad]
    internal static class FullSetupRunner
    {
        private const string PREF_KEY_FULL_SETUP = "UnityTemplate_FullSetup";

        internal static bool IsRunning { get; private set; }

        internal static bool IsRunningOrPersisted =>
            IsRunning || EditorPrefs.GetBool(PREF_KEY_FULL_SETUP, false);

        static FullSetupRunner()
        {
            // ドメインリロード後の状態復元
            EditorApplication.delayCall += RestoreStateAfterReload;
        }

        private static List<SetupStep> BuildPreUpmSteps()
        {
            return new List<SetupStep>
            {
                new SetupStep("フォルダ構成の作成", config =>
                {
                    ProjectStructureService.CreateFolderStructure(config);
                    AssetDatabase.Refresh();
                    Debug.Log("✓ フォルダ構成の作成が完了しました");
                }),
            };
        }

        private static List<SetupStep> BuildPostUpmSteps(TemplateConfigData config)
        {
            var steps = new List<SetupStep>
            {
                new SetupStep("NuGetパッケージのインストール", RunNugetStep),
                new SetupStep("設定ファイルのコピー", ProjectStructureService.CopyConfigFilesOverwrite),
            };

            foreach (var sub in config.submodules)
            {
                steps.Add(new SetupStep($"{sub.linkName} サブモジュールのセットアップ",
                    _ => SubmoduleService.SetupSubmodule(sub.name, sub.url, sub.linkName)));
            }

            if (!string.IsNullOrEmpty(config.analyzers.submoduleName))
            {
                steps.Add(new SetupStep("Analyzers サブモジュールのセットアップ",
                    c => SubmoduleService.SetupAnalyzersSubmodule(c.analyzers, interactive: false)));
            }

            // Analyzers 後: Directory.Build.targets の symlink 先が submodule 配下のため
            steps.Add(new SetupStep("リポジトリスキャフォールドの整備", RepoScaffoldingService.Setup));

            return steps;
        }

        internal static void StartFullSetup()
        {
            if (UpmPackageInstaller.IsInstalling || IsRunning)
            {
                EditorUtility.DisplayDialog("実行中",
                    "セットアップが進行中です。完了までお待ちください。", "OK");
                return;
            }

            var config = TemplateConfig.Load();
            var preSteps = BuildPreUpmSteps();
            var postSteps = BuildPostUpmSteps(config);

            var allTitles = preSteps.Select(s => s.Title)
                .Concat(new[] { "UPMパッケージのインストール" })
                .Concat(postSteps.Select(s => s.Title))
                .ToList();
            int totalSteps = allTitles.Count;

            bool proceed = EditorUtility.DisplayDialog("Full Setup",
                "以下の手順を一括で実行します:\n\n" +
                string.Join("\n", allTitles.Select((t, i) => $"{i + 1}. {t}")) + "\n\n" +
                "※ 既存ファイルは上書きされます。\n" +
                "※ ドメインリロードが発生する場合があります。\n\n" +
                "続行しますか？",
                "開始", "キャンセル");

            if (!proceed) return;

            IsRunning = true;
            EditorPrefs.SetBool(PREF_KEY_FULL_SETUP, true);

            Debug.Log("=== Full Setup を開始します ===");

            int currentStep = 0;
            foreach (var step in preSteps)
            {
                currentStep++;
                Debug.Log($"[Full Setup {currentStep}/{totalSteps}] {step.Title}中...");
                step.Run(config);
            }

            // UPM インストールは非同期。完了後に UpmPackageInstaller から ScheduleContinuationAfterUpm が呼ばれる
            currentStep++;
            Debug.Log($"[Full Setup {currentStep}/{totalSteps}] UPMパッケージのインストールを開始...");

            var currentManifest = TemplateConfig.LoadCurrentManifest();
            var packagesToInstall = UpmPackageInstaller.GetPackagesToInstall(config, currentManifest);

            if (packagesToInstall.Count > 0)
            {
                UpmPackageInstaller.StartInstallation(packagesToInstall);
            }
            else
            {
                Debug.Log("✓ UPMパッケージはすべてインストール済みです");
                ScheduleContinuationAfterUpm();
            }
        }

        internal static void ScheduleContinuationAfterUpm()
        {
            EditorApplication.delayCall += ContinueAfterUpm;
        }

        private static void ContinueAfterUpm()
        {
            if (!IsRunningOrPersisted)
                return;

            IsRunning = true;

            var config = TemplateConfig.Load();
            var preStepCount = BuildPreUpmSteps().Count + 1; // +1 = UPM ステップ
            var postSteps = BuildPostUpmSteps(config);
            int totalSteps = preStepCount + postSteps.Count;

            try
            {
                int currentStep = preStepCount;
                foreach (var step in postSteps)
                {
                    currentStep++;
                    Debug.Log($"[Full Setup {currentStep}/{totalSteps}] {step.Title}中...");
                    step.Run(config);
                }

                AssetDatabase.Refresh();

                var completionMessage = "すべてのセットアップが完了しました！\n\n" +
                    "✓ フォルダ構成の作成\n" +
                    "✓ UPMパッケージのインストール\n" +
                    string.Join("\n", postSteps.Select(s => $"✓ {s.Title}")) +
                    "\n\n詳細はConsoleログを確認してください。";

                Debug.Log("=== Full Setup が完了しました ===");
                EditorUtility.DisplayDialog("Full Setup 完了", completionMessage, "OK");
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
                CleanupState();
            }
        }

        private static void RunNugetStep(TemplateConfigData config)
        {
            if (!NugetPackageService.IsNugetForUnityInstalled())
            {
                Debug.LogWarning("⚠ NuGetForUnityが未インストールのため、NuGetパッケージのインストールをスキップしました");
                return;
            }

            var (success, fail) = NugetPackageService.InstallMissingPackages(config);
            if (success + fail > 0)
                Debug.Log($"✓ NuGetパッケージ: {success}個成功, {fail}個失敗");
            else
                Debug.Log("✓ NuGetパッケージはすべてインストール済みです");
        }

        internal static void CleanupState()
        {
            IsRunning = false;
            EditorPrefs.DeleteKey(PREF_KEY_FULL_SETUP);
        }

        private static void RestoreStateAfterReload()
        {
            if (EditorPrefs.GetBool(PREF_KEY_FULL_SETUP, false))
            {
                IsRunning = true;
            }

            UpmPackageInstaller.RestoreStateAfterReload();
        }
    }
}
