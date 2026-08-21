using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Assets フォルダ構成・設定ファイル・ライセンスファイルの配置
    /// </summary>
    internal static class ProjectStructureService
    {
        internal static bool CreateFolderRecursively(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return false;
            }

            var parentPath = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            var folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parentPath) && parentPath != "Assets")
            {
                CreateFolderRecursively(parentPath);
            }

            var guid = AssetDatabase.CreateFolder(parentPath, folderName);
            return !string.IsNullOrEmpty(guid);
        }

        internal static int CreateFolderStructure(TemplateConfigData config)
        {
            int createdCount = 0;
            foreach (var folder in config.folderStructure)
            {
                if (CreateFolderRecursively(folder))
                {
                    createdCount++;
                }
            }
            return createdCount;
        }

        /// <summary>
        /// 設定ファイルを ConfigTemplates から強制コピーする (Full Setup 用・確認なし)
        /// </summary>
        internal static void CopyConfigFilesOverwrite(TemplateConfigData config)
        {
            var packagePath = TemplateConfig.GetPackagePath();
            if (packagePath == null) return;

            var configTemplatesPath = Path.Combine(packagePath, "ConfigTemplates");
            var projectRoot = TemplateConfig.GetProjectRoot();

            int configCopied = 0;
            foreach (var entry in config.configFiles)
            {
                var destPath = GetConfigDestPath(entry, projectRoot);
                var sourcePath = Path.Combine(configTemplatesPath, entry.source);
                try
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, true);
                        Debug.Log($"  ✓ コピーしました: {entry.source}");
                        configCopied++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"  ✗ コピー失敗: {entry.source} - {e.Message}");
                }
            }
            Debug.Log($"✓ 設定ファイル: {configCopied}個コピーしました");
        }

        /// <summary>
        /// 設定ファイルを上書き確認付きでコピーする (単体メニュー用)
        /// </summary>
        internal static void CopyConfigFilesInteractive()
        {
            var packagePath = TemplateConfig.GetPackagePath();
            if (packagePath == null) return;
            var config = TemplateConfig.Load();

            var configTemplatesPath = Path.Combine(packagePath, "ConfigTemplates");
            var projectRoot = TemplateConfig.GetProjectRoot();

            var copiedCount = 0;
            var skippedCount = 0;

            foreach (var entry in config.configFiles)
            {
                var destPath = GetConfigDestPath(entry, projectRoot);
                var sourcePath = Path.Combine(configTemplatesPath, entry.source);
                var shouldCopy = CopyConfigFile(sourcePath, destPath, entry.source, ref skippedCount);
                if (shouldCopy)
                {
                    copiedCount++;
                }
            }

            AssetDatabase.Refresh();
            ShowCopyConfigFilesResult(copiedCount, skippedCount);
        }

        private static string GetConfigDestPath(ConfigFileEntry entry, string projectRoot)
        {
            return entry.destination == "assets"
                ? Path.Combine(Application.dataPath, entry.source)
                : Path.Combine(projectRoot, entry.source);
        }

        private static bool CopyConfigFile(string sourcePath, string destPath, string fileName, ref int skippedCount)
        {
            if (File.Exists(destPath) && !FullSetupRunner.IsRunning)
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

        private static void ShowCopyConfigFilesResult(int copiedCount, int skippedCount)
        {
            if (FullSetupRunner.IsRunning) return;

            string message;

            if (copiedCount > 0 && skippedCount > 0)
            {
                message = $"{copiedCount}個のファイルをコピーしました。\n{skippedCount}個のファイルをスキップしました。";
            }
            else if (copiedCount > 0)
            {
                var config = TemplateConfig.Load();
                var fileList = string.Join("\n", config.configFiles.Select(
                    f => $"• {f.source}: {(f.destination == "assets" ? "Assets/" : "プロジェクトルート")}"
                ));
                message = $"{copiedCount}個の設定ファイルをコピーしました。\n\n{fileList}";
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

        internal static int CopyLicenseFilesFromTemplate(string licenseFolderPath)
        {
            var packagePath = TemplateConfig.GetPackagePath();
            if (packagePath == null) return 0;

            var licenseTemplatesPath = Path.Combine(packagePath, "LicenseTemplates");

            var targetPath = licenseFolderPath;
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                CreateFolderRecursively(targetPath);
            }

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
    }
}
