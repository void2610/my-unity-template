using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// NuGetForUnity 連携 (パッケージ列挙・インストール・復元)。
    /// NugetForUnity 名前空間を直接参照するとパッケージ未導入時にコンパイルエラーになるため、全て Reflection 経由で呼び出す。
    /// </summary>
    internal static class NugetPackageService
    {
        internal static bool IsNugetForUnityInstalled()
        {
            return System.AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a => a.GetName().Name.Contains("NuGetForUnity"));
        }

        internal static Dictionary<string, string> LoadTemplatePackages(TemplateConfigData config)
        {
            var packages = new Dictionary<string, string>();
            foreach (var entry in config.nugetPackages)
            {
                if (!string.IsNullOrEmpty(entry.id) && !string.IsNullOrEmpty(entry.version))
                {
                    packages[entry.id] = entry.version;
                }
            }
            return packages;
        }

        internal static Dictionary<string, string> GetInstalledPackages(string packagesConfigPath)
        {
            var packages = new Dictionary<string, string>();

            if (!File.Exists(packagesConfigPath))
            {
                return packages;
            }

            try
            {
                var doc = XDocument.Load(packagesConfigPath);
                var packageElements = doc.Root?.Elements("package");

                if (packageElements == null) return packages;

                foreach (var element in packageElements)
                {
                    var id = element.Attribute("id")?.Value;
                    var version = element.Attribute("version")?.Value;

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                    {
                        packages[id] = version;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"packages.configの読み込みに失敗しました: {e.Message}");
            }

            return packages;
        }

        /// <summary>
        /// テンプレート定義のうち未導入の NuGet パッケージをインストールし、(成功数, 失敗数) を返す
        /// </summary>
        internal static (int success, int fail) InstallMissingPackages(TemplateConfigData config)
        {
            var templatePackages = LoadTemplatePackages(config);
            if (templatePackages.Count == 0) return (0, 0);

            var packagesConfigPath = GetPackagesConfigPath();
            if (string.IsNullOrEmpty(packagesConfigPath)) return (0, 0);

            var installedPackages = GetInstalledPackages(packagesConfigPath);
            var packagesToInstall = templatePackages
                .Where(p => !installedPackages.ContainsKey(p.Key))
                .ToList();

            int successCount = 0;
            int failCount = 0;
            foreach (var package in packagesToInstall)
            {
                bool success = InstallPackage(package.Key, package.Value);
                if (success) successCount++;
                else failCount++;
            }

            return (successCount, failCount);
        }

        internal static bool InstallPackage(string packageId, string version)
        {
            try
            {
                var allTypes = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .ToArray();

                var packageIdentifierType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.NugetPackageIdentifier");

                if (packageIdentifierType == null)
                {
                    Debug.LogWarning("NugetPackageIdentifier クラスが見つかりません");
                    return false;
                }

                var constructor = packageIdentifierType.GetConstructor(new[] { typeof(string), typeof(string) });
                if (constructor == null)
                {
                    Debug.LogWarning("NugetPackageIdentifier コンストラクタが見つかりません");
                    return false;
                }

                var packageIdentifier = constructor.Invoke(new object[] { packageId, version });
                Debug.Log($"パッケージ識別子を作成: {packageId} ({version})");

                var cacheManagerType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.PackageCacheManager");

                if (cacheManagerType == null)
                {
                    Debug.LogWarning("PackageCacheManager クラスが見つかりません");
                    return false;
                }

                var identifierInterfaceType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.INugetPackageIdentifier");

                if (identifierInterfaceType == null)
                {
                    Debug.LogWarning("INugetPackageIdentifier インターフェースが見つかりません");
                    return false;
                }

                var getPackageMethod = cacheManagerType.GetMethod("GetPackageFromCacheOrSource",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new System.Type[] { identifierInterfaceType },
                    null);

                if (getPackageMethod == null)
                {
                    Debug.LogWarning("GetPackageFromCacheOrSource メソッドが見つかりません");
                    return false;
                }

                var nugetPackage = getPackageMethod.Invoke(null, new object[] { packageIdentifier });
                if (nugetPackage == null)
                {
                    Debug.LogWarning($"パッケージが見つかりません: {packageId}");
                    return false;
                }

                Debug.Log($"パッケージを取得: {nugetPackage}");

                var packageInterfaceType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Models.INugetPackage");

                if (packageInterfaceType == null)
                {
                    Debug.LogWarning("INugetPackage インターフェースが見つかりません");
                    return false;
                }

                var installerType = allTypes
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.NugetPackageInstaller");

                if (installerType == null)
                {
                    Debug.LogWarning("NugetPackageInstaller クラスが見つかりません");
                    return false;
                }

                var installMethod = installerType.GetMethod("Install",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new System.Type[] { packageInterfaceType, typeof(bool), typeof(bool), typeof(bool) },
                    null);

                if (installMethod == null)
                {
                    Debug.LogWarning("Install メソッドが見つかりません");
                    var methods = installerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        Debug.Log($"  利用可能なメソッド: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                    }
                    return false;
                }

                Debug.Log($"Installメソッドを呼び出します...");
                var result = installMethod.Invoke(null, new object[] { nugetPackage, true, false, true });

                if (result != null)
                {
                    var successProperty = result.GetType().GetProperty("Successful");
                    if (successProperty != null)
                    {
                        var success = (bool)successProperty.GetValue(result);
                        Debug.Log($"インストール結果: {(success ? "成功" : "失敗")}");
                        return success;
                    }
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"パッケージのインストールに失敗しました ({packageId}): {e.Message}");
                if (e.InnerException != null)
                {
                    Debug.LogError($"内部エラー: {e.InnerException.Message}");
                }
                return false;
            }
        }

        internal static string GetPackagesConfigPath()
        {
            try
            {
                var configManagerType = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.Configuration.ConfigurationManager");

                if (configManagerType == null)
                {
                    Debug.LogWarning("ConfigurationManager クラスが見つかりません");
                    return null;
                }

                var nugetConfigFileProperty = configManagerType.GetProperty("NugetConfigFile",
                    BindingFlags.Public | BindingFlags.Static);

                if (nugetConfigFileProperty == null)
                {
                    Debug.LogWarning("NugetConfigFile プロパティが見つかりません");
                    return null;
                }

                var nugetConfigFile = nugetConfigFileProperty.GetValue(null);
                if (nugetConfigFile == null)
                {
                    Debug.LogWarning("NugetConfigFile の値が null です");
                    return null;
                }

                var packagesConfigFilePathProperty = nugetConfigFile.GetType().GetProperty("PackagesConfigFilePath",
                    BindingFlags.Public | BindingFlags.Instance);

                if (packagesConfigFilePathProperty == null)
                {
                    Debug.LogWarning("PackagesConfigFilePath プロパティが見つかりません");
                    return null;
                }

                var path = packagesConfigFilePathProperty.GetValue(nugetConfigFile) as string;
                Debug.Log($"NuGetForUnity packages.config パス: {path}");
                return path;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"packages.configパスの取得に失敗しました: {e.Message}");
                return null;
            }
        }

        internal static bool MergePackagesConfig(string packagesConfigPath, List<KeyValuePair<string, string>> packagesToAdd)
        {
            try
            {
                XDocument doc;

                if (File.Exists(packagesConfigPath))
                {
                    doc = XDocument.Load(packagesConfigPath);
                }
                else
                {
                    doc = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("packages")
                    );
                }

                var root = doc.Root;
                if (root == null)
                {
                    root = new XElement("packages");
                    doc.Add(root);
                }

                foreach (var package in packagesToAdd)
                {
                    var element = new XElement("package",
                        new XAttribute("id", package.Key),
                        new XAttribute("version", package.Value),
                        new XAttribute("manuallyInstalled", "true")
                    );
                    root.Add(element);
                    Debug.Log($"packages.configに追加: {package.Key} ({package.Value})");
                }

                doc.Save(packagesConfigPath);
                Debug.Log($"✓ packages.configを保存しました: {packagesConfigPath}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"packages.configの更新に失敗しました: {e.Message}");
                return false;
            }
        }

        internal static bool RestorePackages()
        {
            try
            {
                var packageRestorerType = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return System.Array.Empty<System.Type>();
                        }
                    })
                    .FirstOrDefault(t => t.FullName == "NugetForUnity.PackageRestorer");

                if (packageRestorerType == null)
                {
                    Debug.LogWarning("PackageRestorer クラスが見つかりません");
                    return false;
                }

                var restoreMethod = packageRestorerType.GetMethod("Restore",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(bool) },
                    null);

                if (restoreMethod == null)
                {
                    Debug.LogWarning("PackageRestorer.Restore メソッドが見つかりません");
                    return false;
                }

                // Restore(false): 依存関係も含めて完全復元
                restoreMethod.Invoke(null, new object[] { false });
                Debug.Log("✓ NuGetForUnity Restore を実行しました");

                AssetDatabase.Refresh();
                Debug.Log("✓ AssetDatabase をリフレッシュしました");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NuGetForUnity Restore の呼び出しに失敗しました: {e.Message}");
                return false;
            }
        }
    }
}
