using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// セットアップ状況の一覧と個別実行をまとめたダッシュボード。
    /// 状態チェックは manifest / ファイル IO を伴うため OnGUI 毎ではなくキャッシュし、フォーカス時と操作後にだけ更新する。
    /// </summary>
    internal sealed class TemplateSetupWindow : EditorWindow
    {
        private TemplateConfigData _config;
        private int _upmMissingCount;
        private bool _nugetAvailable;
        private int _nugetMissingCount;
        private List<(string label, bool done)> _submoduleStatus = new();
        private List<(string label, bool done)> _configFileStatus = new();
        private bool _scaffoldingDone;
        private List<(DeployTargetConfig target, bool placed, bool selected)> _deployStatus = new();
        private Vector2 _scroll;

        [MenuItem("Tools/Unity Template/Setup Window", false, 1)]
        internal static void Open()
        {
            var window = GetWindow<TemplateSetupWindow>("Unity Template");
            window.minSize = new Vector2(380, 480);
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void OnFocus()
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            _config = TemplateConfig.Load();
            var projectRoot = TemplateConfig.GetProjectRoot();

            var manifest = TemplateConfig.LoadCurrentManifest();
            _upmMissingCount = UpmPackageInstaller.GetPackagesToInstall(_config, manifest).Count;

            _nugetAvailable = NugetPackageService.IsNugetForUnityInstalled();
            _nugetMissingCount = 0;
            if (_nugetAvailable)
            {
                var packagesConfigPath = NugetPackageService.GetPackagesConfigPath();
                if (!string.IsNullOrEmpty(packagesConfigPath))
                {
                    var installed = NugetPackageService.GetInstalledPackages(packagesConfigPath);
                    _nugetMissingCount = NugetPackageService.LoadTemplatePackages(_config)
                        .Count(p => !installed.ContainsKey(p.Key));
                }
            }

            _submoduleStatus = _config.submodules
                .Select(s => (s.linkName, SubmoduleService.IsSubmoduleRegistered(s.name)))
                .ToList();
            if (!string.IsNullOrEmpty(_config.analyzers.submoduleName))
            {
                _submoduleStatus.Add(("Analyzers", SubmoduleService.IsSubmoduleRegistered(_config.analyzers.submoduleName)));
            }

            _configFileStatus = _config.configFiles
                .Select(f =>
                {
                    var dest = f.destination == "assets"
                        ? Path.Combine(Application.dataPath, f.source)
                        : Path.Combine(projectRoot, f.source);
                    return (f.source, File.Exists(dest));
                })
                .ToList();

            _scaffoldingDone = File.Exists(Path.Combine(projectRoot, "Knowledge", "index.md")) &&
                               File.Exists(Path.Combine(projectRoot, ".gitattributes"));

            // 配置済みターゲットの選択状態は意味を持たないため false に戻す
            var previousSelection = _deployStatus.ToDictionary(d => d.target.name, d => d.selected);
            _deployStatus = (_config.deployTargets ?? new DeployTargetConfig[0])
                .Where(t => t.files != null && t.files.Length > 0)
                .Select(t =>
                {
                    var placed = RepoScaffoldingService.IsDeployTargetPlaced(t, projectRoot);
                    var selected = !placed && previousSelection.TryGetValue(t.name, out var s) && s;
                    return (t, placed, selected);
                })
                .ToList();

            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            DrawFullSetupSection();
            DrawPackagesSection();
            DrawSubmodulesSection();
            DrawFilesSection();
            DrawDeploySection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Unity Template セットアップ", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("更新", GUILayout.Width(60)))
                {
                    RefreshStatus();
                }
            }

            if (UpmPackageInstaller.IsInstalling || FullSetupRunner.IsRunning)
            {
                EditorGUILayout.HelpBox("セットアップが進行中です。完了までお待ちください。", MessageType.Info);
            }
        }

        private void DrawFullSetupSection()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(UpmPackageInstaller.IsInstalling || FullSetupRunner.IsRunning))
            {
                if (GUILayout.Button("Full Setup (全ステップ一括実行)", GUILayout.Height(32)))
                {
                    FullSetupRunner.StartFullSetup();
                    RefreshStatus();
                }
            }
        }

        private void DrawPackagesSection()
        {
            DrawSectionTitle("パッケージ");

            DrawStatusRow(
                _upmMissingCount == 0 ? "UPM: すべて導入済み" : $"UPM: 未導入 {_upmMissingCount} 件",
                _upmMissingCount == 0,
                "インストール",
                () => SetupActions.InstallDependencies());

            string nugetLabel;
            if (!_nugetAvailable)
                nugetLabel = "NuGet: NuGetForUnity 未導入 (先に UPM を導入)";
            else if (_nugetMissingCount == 0)
                nugetLabel = "NuGet: すべて導入済み";
            else
                nugetLabel = $"NuGet: 未導入 {_nugetMissingCount} 件";

            DrawStatusRow(
                nugetLabel,
                _nugetAvailable && _nugetMissingCount == 0,
                "インストール",
                () => SetupActions.InstallNugetPackages(),
                enabled: _nugetAvailable);
        }

        private void DrawSubmodulesSection()
        {
            DrawSectionTitle("サブモジュール");

            foreach (var (label, done) in _submoduleStatus)
            {
                var isAnalyzers = label == "Analyzers";
                DrawStatusRow(
                    label,
                    done,
                    "セットアップ",
                    () =>
                    {
                        if (isAnalyzers)
                        {
                            SubmoduleService.SetupAnalyzersSubmodule(_config.analyzers, interactive: true);
                        }
                        else
                        {
                            var sub = _config.submodules.First(s => s.linkName == label);
                            SubmoduleService.SetupSubmodule(sub.name, sub.url, sub.linkName);
                        }
                    });
            }
        }

        private void DrawFilesSection()
        {
            DrawSectionTitle("フォルダ / ファイル");

            DrawStatusRow("フォルダ構成", null, "作成", () => SetupActions.CreateFolderStructure());

            var allConfigDone = _configFileStatus.All(f => f.done);
            var missing = _configFileStatus.Where(f => !f.done).Select(f => f.label).ToList();
            DrawStatusRow(
                allConfigDone ? "設定ファイル: すべて配置済み" : $"設定ファイル: 未配置 {string.Join(", ", missing)}",
                allConfigDone,
                "コピー",
                () => SetupActions.CopyConfigFiles());

            DrawStatusRow(
                "リポジトリスキャフォールド (Knowledge / .claude / CI)",
                _scaffoldingDone,
                "整備",
                () => SetupActions.SetupRepoScaffolding());

            DrawStatusRow("ライセンスファイル (LicenseMaster)", null, "コピー", () => SetupActions.CopyLicenseFiles());
        }

        private void DrawDeploySection()
        {
            if (_deployStatus.Count == 0) return;

            DrawSectionTitle("デプロイ workflow");

            for (int i = 0; i < _deployStatus.Count; i++)
            {
                var (target, placed, selected) = _deployStatus[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(placed))
                    {
                        var newSelected = EditorGUILayout.ToggleLeft(
                            placed ? $"{target.name} (配置済み)" : target.name,
                            placed || selected);
                        if (!placed && newSelected != selected)
                        {
                            _deployStatus[i] = (target, placed, newSelected);
                        }
                    }
                }
            }

            var anySelected = _deployStatus.Any(d => !d.placed && d.selected);
            using (new EditorGUI.DisabledScope(!anySelected))
            {
                if (GUILayout.Button("選択したターゲットを配置"))
                {
                    var templatesPath = RepoScaffoldingService.GetRepoTemplatesPath();
                    var projectRoot = TemplateConfig.GetProjectRoot();
                    if (templatesPath != null)
                    {
                        foreach (var (target, placed, selected) in _deployStatus.Where(d => !d.placed && d.selected))
                        {
                            RepoScaffoldingService.PlaceDeployTarget(target, templatesPath, projectRoot);
                        }
                        AssetDatabase.Refresh();
                    }
                    RefreshStatus();
                }
            }
        }

        private void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        // done=null は状態を持たない常時実行アクション (例: フォルダ作成)
        private void DrawStatusRow(string label, bool? done, string buttonLabel, System.Action action, bool enabled = true)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = done switch
                {
                    true => "✓",
                    false => "–",
                    null => " ",
                };
                GUILayout.Label($"{icon} {label}", EditorStyles.label);
                GUILayout.FlexibleSpace();

                var busy = UpmPackageInstaller.IsInstalling || FullSetupRunner.IsRunning;
                using (new EditorGUI.DisabledScope(!enabled || busy))
                {
                    if (GUILayout.Button(buttonLabel, GUILayout.Width(110)))
                    {
                        action();
                        RefreshStatus();
                    }
                }
            }
        }
    }
}
