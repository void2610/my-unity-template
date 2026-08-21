using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Unity 外のリポジトリ構造 (Knowledge / .claude / CI workflow / symlink / .gitignore) の整備
    /// </summary>
    internal static class RepoScaffoldingService
    {
        internal static void Setup(TemplateConfigData config)
        {
            var projectRoot = TemplateConfig.GetProjectRoot();
            var packagePath = TemplateConfig.GetPackagePath();
            if (packagePath == null) return;
            var templatesPath = Path.Combine(packagePath, Path.Combine("Templates", "Repo~"));

            foreach (var entry in config.repoFiles)
            {
                CopyRepoFileIfAbsent(templatesPath, projectRoot, entry);
            }

            CreateKnowledgeLogIfAbsent(projectRoot);
            SetupKnowledgeSubmodule(config.knowledgeSubmodule, projectRoot);

            foreach (var entry in config.rootSymlinks)
            {
                EnsureRootSymlink(projectRoot, entry.link, entry.target);
            }

            AppendGitignoreRules(projectRoot, config.gitignoreRules);
            SetupDeployWorkflows(config.deployTargets, templatesPath, projectRoot);
            Debug.Log("✓ リポジトリスキャフォールドの整備が完了しました");
        }

        private static void CopyRepoFileIfAbsent(string templatesPath, string projectRoot, RepoFileEntry entry)
        {
            var destPath = Path.Combine(projectRoot, entry.target);
            if (File.Exists(destPath))
            {
                Debug.Log($"  - スキップ (既存): {entry.target}");
                return;
            }

            var sourcePath = Path.Combine(templatesPath, entry.source);
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"  ⚠ テンプレートが見つかりません: {entry.source}");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            var content = File.ReadAllText(sourcePath);
            // Cloudflare のプロジェクト名や Steam のビルド名等をリポジトリ名で埋める
            content = content.Replace("__PROJECT_NAME__", Path.GetFileName(projectRoot));
            File.WriteAllText(destPath, content);
            Debug.Log($"  ✓ 作成しました: {entry.target}");
        }

        private static void CreateKnowledgeLogIfAbsent(string projectRoot)
        {
            var logPath = Path.Combine(projectRoot, "Knowledge", "log.md");
            if (File.Exists(logPath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            var today = System.DateTime.Now.ToString("yyyy-MM-dd");
            File.WriteAllText(logPath,
                "# Update Log\n\n更新履歴を新しい順に記録する。日付は `YYYY-MM-DD`。\n\n" +
                $"## {today}\n* **Add**: Knowledge ベースを初期化。\n");
            Debug.Log("  ✓ 作成しました: Knowledge/log.md");
        }

        private static void SetupKnowledgeSubmodule(KnowledgeSubmoduleConfig knowledge, string projectRoot)
        {
            if (knowledge == null || string.IsNullOrEmpty(knowledge.url) || string.IsNullOrEmpty(knowledge.path))
                return;

            if (SubmoduleService.IsSubmoduleRegistered(knowledge.path))
            {
                Debug.Log($"  ✓ {knowledge.path} サブモジュールは既に登録されています");
                ProcessUtility.ExecuteGitCommandSync(projectRoot, $"submodule update --init \"{knowledge.path}\"");
                return;
            }

            if (Directory.Exists(Path.Combine(projectRoot, knowledge.path)))
            {
                Debug.LogWarning($"  ⚠ {knowledge.path} がサブモジュール定義なしで存在します。手動で確認してください。");
                return;
            }

            var exitCode = ProcessUtility.ExecuteGitCommandSync(projectRoot, $"submodule add {knowledge.url} \"{knowledge.path}\"");
            if (exitCode == 0)
                Debug.Log($"  ✓ サブモジュールを追加しました: {knowledge.path}");
            else
                Debug.LogError($"  ✗ サブモジュールの追加に失敗しました: {knowledge.path}");
        }

        private static void EnsureRootSymlink(string projectRoot, string link, string target)
        {
            var linkPath = Path.Combine(projectRoot, link);
            if (File.Exists(linkPath) || Directory.Exists(linkPath))
            {
                Debug.Log($"  - スキップ (既存): {link}");
                return;
            }

            if (SymlinkUtility.CreateRootSymlink(projectRoot, linkPath, target))
                Debug.Log($"  ✓ symlink を作成しました: {link} -> {target}");
            else
                Debug.LogError($"  ✗ symlink の作成に失敗しました: {link} (Windows はファイル symlink に Developer Mode か管理者権限が必要)");
        }

        private static void AppendGitignoreRules(string projectRoot, string[] rules)
        {
            if (rules == null || rules.Length == 0) return;

            // マーカー行の有無で冪等性を担保する
            const string marker = "# --- my-unity-template scaffolding ---";
            var gitignorePath = Path.Combine(projectRoot, ".gitignore");
            var existing = File.Exists(gitignorePath) ? File.ReadAllText(gitignorePath) : "";
            if (existing.Contains(marker))
            {
                Debug.Log("  - スキップ (追記済み): .gitignore");
                return;
            }

            File.AppendAllText(gitignorePath, "\n" + marker + "\n" + string.Join("\n", rules) + "\n");
            Debug.Log("  ✓ .gitignore にルールを追記しました");
        }

        private static void SetupDeployWorkflows(DeployTargetConfig[] targets, string templatesPath, string projectRoot)
        {
            if (targets == null) return;

            // Web デプロイ先と Steam は排他でないため、ターゲットごとに個別確認する
            foreach (var target in targets)
            {
                if (target.files == null || target.files.Length == 0) continue;

                if (target.files.Any(e => File.Exists(Path.Combine(projectRoot, e.target))))
                {
                    Debug.Log($"  - スキップ (既存): {target.name} workflow");
                    continue;
                }

                if (!EditorUtility.DisplayDialog("デプロイ workflow",
                        $"{target.name} のデプロイ workflow を配置しますか？", "配置する", "スキップ"))
                    continue;

                foreach (var entry in target.files)
                {
                    CopyRepoFileIfAbsent(templatesPath, projectRoot, entry);
                }
            }
        }
    }
}
