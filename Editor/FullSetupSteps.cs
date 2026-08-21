using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Full Setup のステップカタログ。実行順序と内容の定義のみを持ち、進行管理は FullSetupRunner が担う。
    /// UPM インストールだけはドメインリロードを跨ぐ非同期のため SetupStep にできず、Runner が直接扱う。
    /// </summary>
    internal static class FullSetupSteps
    {
        internal const string UpmStepTitle = "UPMパッケージのインストール";

        internal static List<SetupStep> BuildPreUpmSteps()
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

        internal static List<SetupStep> BuildPostUpmSteps(TemplateConfigData config)
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
    }
}
