using UnityEngine;
using UnityEditor;
using System.IO;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// git submodule の追加・symlink 連携・アナライザービルド
    /// </summary>
    internal static class SubmoduleService
    {
        internal static bool IsSubmoduleRegistered(string submoduleName)
        {
            var gitModulesPath = Path.Combine(Application.dataPath, "..", ".gitmodules");
            if (!File.Exists(gitModulesPath))
                return false;

            var content = File.ReadAllText(gitModulesPath);
            return content.Contains($"[submodule \"{submoduleName}\"]");
        }

        internal static bool IsGitRepository(string path)
        {
            if (!Directory.Exists(path))
                return false;

            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir) || File.Exists(gitDir);
        }

        internal static void CleanupGitModules(string submoduleName)
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
        /// 汎用サブモジュールセットアップ (Assets/Scripts/ への symlink 込み)
        /// </summary>
        internal static void SetupSubmodule(string submoduleName, string repoUrl, string linkName)
        {
            var projectRoot = TemplateConfig.GetProjectRoot();
            var submodulePath = Path.Combine(projectRoot, submoduleName);

            if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
            {
                ProjectStructureService.CreateFolderRecursively("Assets/Scripts");
                AssetDatabase.Refresh();
            }

            if (IsSubmoduleRegistered(submoduleName))
            {
                Debug.Log($"✓ {linkName} submodule already registered");
                ProcessUtility.ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                CreateSubmoduleSymbolicLinkIfNeeded(submoduleName, linkName);
                return;
            }

            if (Directory.Exists(submodulePath))
            {
                if (!FullSetupRunner.IsRunning)
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

            CleanupGitModules(submoduleName);

            Debug.Log($"Adding {submoduleName} as submodule...");
            int exitCode = ProcessUtility.ExecuteGitCommandSync(projectRoot, $"submodule add {repoUrl} {submoduleName}");

            if (exitCode == 0)
            {
                Debug.Log($"✓ {linkName} submodule added");
                ProcessUtility.ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                CreateSubmoduleSymbolicLinkIfNeeded(submoduleName, linkName);
            }
            else
            {
                Debug.LogError($"{linkName} Submoduleの追加に失敗しました");
                if (!FullSetupRunner.IsRunning)
                {
                    EditorUtility.DisplayDialog("エラー",
                        $"{linkName} Submoduleの追加に失敗しました。\nGitリポジトリが初期化されているか確認してください。",
                        "OK");
                }
            }
        }

        /// <summary>
        /// Analyzers サブモジュールのセットアップ (interactive=true でダイアログ確認あり)
        /// </summary>
        internal static void SetupAnalyzersSubmodule(AnalyzersConfig analyzersConfig, bool interactive)
        {
            var projectRoot = TemplateConfig.GetProjectRoot();
            var submoduleName = analyzersConfig.submoduleName;
            var repoUrl = analyzersConfig.url;
            var submodulePath = Path.Combine(projectRoot, submoduleName);

            if (IsSubmoduleRegistered(submoduleName))
            {
                Debug.Log($"✓ {submoduleName} サブモジュールは既に登録されています");
                ProcessUtility.ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
                BuildAnalyzerDll(projectRoot, submoduleName, analyzersConfig.projectPath);
                return;
            }

            if (Directory.Exists(submodulePath))
            {
                if (interactive)
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
                }

                try
                {
                    Directory.Delete(submodulePath, true);
                    Debug.Log($"既存のディレクトリを削除しました: {submodulePath}");
                }
                catch (System.Exception e)
                {
                    if (interactive)
                        EditorUtility.DisplayDialog("エラー", $"ディレクトリの削除に失敗しました:\n{e.Message}", "OK");
                    else
                        Debug.LogError($"ディレクトリの削除に失敗しました: {e.Message}");
                    return;
                }
            }

            CleanupGitModules(submoduleName);

            Debug.Log($"{submoduleName} をサブモジュールとして追加中...");
            var exitCode = ProcessUtility.ExecuteGitCommandSync(projectRoot, $"submodule add {repoUrl} {submoduleName}");

            if (exitCode != 0)
            {
                if (interactive)
                    EditorUtility.DisplayDialog("エラー",
                        "Analyzers Submoduleの追加に失敗しました。\nGitリポジトリが初期化されているか確認してください。",
                        "OK");
                else
                    Debug.LogError("Analyzers Submoduleの追加に失敗しました");
                return;
            }

            Debug.Log($"✓ {submoduleName} サブモジュールを追加しました");
            ProcessUtility.ExecuteGitCommandSync(projectRoot, "submodule update --init --recursive");
            BuildAnalyzerDll(projectRoot, submoduleName, analyzersConfig.projectPath);
        }

        private static void BuildAnalyzerDll(string projectRoot, string submoduleName, string projectPath)
        {
            var pathParts = projectPath.Split('/');
            var analyzerProjectPath = Path.Combine(projectRoot, submoduleName);
            foreach (var part in pathParts)
            {
                analyzerProjectPath = Path.Combine(analyzerProjectPath, part);
            }

            if (!Directory.Exists(analyzerProjectPath))
            {
                if (!FullSetupRunner.IsRunning)
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
            var exitCode = ProcessUtility.ExecuteShellCommand("dotnet", $"build \"{analyzerProjectPath}\" -c Release");

            if (exitCode == 0)
            {
                Debug.Log("✓ アナライザーDLLのビルドが完了しました");
                if (!FullSetupRunner.IsRunning)
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
                if (!FullSetupRunner.IsRunning)
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

        private static void CreateSubmoduleSymbolicLinkIfNeeded(string submoduleName, string linkName)
        {
            var projectRoot = TemplateConfig.GetProjectRoot();
            var submodulePath = Path.Combine(projectRoot, submoduleName);
            var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            var symlinkPath = Path.Combine(scriptsPath, linkName);

            if (Directory.Exists(symlinkPath) || File.Exists(symlinkPath))
            {
                try
                {
                    // GetFullPath はリンク先を解決しないため、ReparsePoint (symlink/junction) ならセットアップ済みとみなす
                    var attributes = File.GetAttributes(symlinkPath);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        Debug.Log($"✓ {linkName} symbolic link already exists");
                        ShowSubmoduleSetupCompletedDialog(submoduleName, linkName);
                        return;
                    }
                }
                catch
                {
                    // 属性の取得に失敗した場合は既存ファイル/ディレクトリとして扱う
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
            bool symlinkCreated = SymlinkUtility.CreateDirectorySymlink(symlinkPath, relativePath);

            if (symlinkCreated)
            {
                Debug.Log($"✓ Symbolic link created: Assets/Scripts/{linkName} -> {submoduleName}");
                if (!FullSetupRunner.IsRunning)
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

        private static void ShowSubmoduleSetupCompletedDialog(string submoduleName, string linkName)
        {
            if (FullSetupRunner.IsRunning) return;

            EditorUtility.DisplayDialog("セットアップ完了",
                $"{linkName} のセットアップが完了しました！\n\n" +
                $"✓ Submodule追加: {submoduleName}/\n" +
                $"✓ シンボリックリンク作成: Assets/Scripts/{linkName}/\n\n" +
                $"{linkName}スクリプトが利用可能になりました。",
                "OK");
        }
    }
}
