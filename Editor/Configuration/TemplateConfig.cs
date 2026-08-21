using UnityEngine;
using UnityEditor;
using System.IO;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// template-config.json とパッケージパスの解決
    /// </summary>
    internal static class TemplateConfig
    {
        internal static string GetPackagePath()
        {
            // スクリプトの配置に依存しないよう、template-config.json をアンカーにパッケージの Editor ルートを解決する
            foreach (var guid in AssetDatabase.FindAssets("template-config"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "template-config.json")
                {
                    return Path.GetDirectoryName(path);
                }
            }

            Debug.LogError("template-config.json が見つかりません");
            return null;
        }

        internal static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        internal static TemplateConfigData Load()
        {
            var packagePath = GetPackagePath();
            if (packagePath == null)
            {
                Debug.LogWarning("パッケージパスが見つかりません。デフォルト設定を使用します。");
                return new TemplateConfigData();
            }

            var configPath = Path.Combine(packagePath, "template-config.json");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"template-config.json が見つかりません: {configPath}\nデフォルト設定を使用します。");
                return new TemplateConfigData();
            }

            try
            {
                var configText = File.ReadAllText(configPath);
                var config = JsonUtility.FromJson<TemplateConfigData>(configText);
                return config ?? new TemplateConfigData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"template-config.json の解析に失敗しました: {e.Message}\nデフォルト設定を使用します。");
                return new TemplateConfigData();
            }
        }

        internal static ManifestData LoadCurrentManifest()
        {
            var manifestPath = "Packages/manifest.json";
            if (!File.Exists(manifestPath))
            {
                return new ManifestData();
            }

            try
            {
                // JsonUtility は Dictionary を扱えず dependencies が常に空になるため、手動でパースする
                var manifest = new ManifestData();
                var manifestText = File.ReadAllText(manifestPath);
                var depsMatch = System.Text.RegularExpressions.Regex.Match(
                    manifestText, "\"dependencies\"\\s*:\\s*\\{([^}]*)\\}");
                if (depsMatch.Success)
                {
                    var pairs = System.Text.RegularExpressions.Regex.Matches(
                        depsMatch.Groups[1].Value, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                    foreach (System.Text.RegularExpressions.Match pair in pairs)
                    {
                        manifest.dependencies[pair.Groups[1].Value] = pair.Groups[2].Value;
                    }
                }
                return manifest;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"現在のmanifest.jsonの読み込みに失敗しました: {e.Message}");
                return new ManifestData();
            }
        }
    }
}
