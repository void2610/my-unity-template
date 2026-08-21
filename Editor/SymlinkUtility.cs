using UnityEngine;
using System.IO;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// symlink 作成 (Windows は Junction / file symlink を使い分ける)
    /// </summary>
    internal static class SymlinkUtility
    {
        internal static bool CreateDirectorySymlink(string linkPath, string targetPath)
        {
            try
            {
#if UNITY_EDITOR_WIN
                // Windows: Junction（管理者権限不要）
                var args = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"";
                return ProcessUtility.ExecuteShellCommand("cmd.exe", args) == 0;
#else
                // macOS/Linux: シンボリックリンク
                return ProcessUtility.ExecuteShellCommand("ln", $"-s \"{targetPath}\" \"{linkPath}\"") == 0;
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create symlink: {e.Message}");
                return false;
            }
        }

        internal static bool CreateRootSymlink(string projectRoot, string linkPath, string target)
        {
            try
            {
#if UNITY_EDITOR_WIN
                var targetFull = Path.GetFullPath(Path.Combine(projectRoot, target));
                // Junction はディレクトリ専用かつ絶対パスが必要。ファイルは file symlink (相対 target はリンク位置基準で解決される)
                var args = Directory.Exists(targetFull)
                    ? $"/c mklink /J \"{linkPath}\" \"{targetFull}\""
                    : $"/c mklink \"{linkPath}\" \"{target}\"";
                return ProcessUtility.ExecuteShellCommand("cmd.exe", args) == 0;
#else
                return ProcessUtility.ExecuteShellCommand("ln", $"-s \"{target}\" \"{linkPath}\"") == 0;
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create symlink: {e.Message}");
                return false;
            }
        }
    }
}
