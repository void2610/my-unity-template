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
                    var (output, error) = ReadOutputsAndWait(process);

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
                    var (_, error) = ReadOutputsAndWait(process);

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

        // WaitForExit 前に同期読み取りするとバッファ満杯でデッドロックするため、非同期で吸い上げてから待つ
        private static (string output, string error) ReadOutputsAndWait(System.Diagnostics.Process process)
        {
            var output = new System.Text.StringBuilder();
            var error = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return (output.ToString(), error.ToString());
        }
    }
}
