using UnityEngine;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// 外部プロセス (シェル / git) の同期実行
    /// </summary>
    internal static class ProcessUtility
    {
        internal static int ExecuteGitCommandSync(string workingDirectory, string arguments)
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

        internal static int ExecuteShellCommand(string command, string arguments)
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
    }
}
