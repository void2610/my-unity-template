using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Void2610.UnityTemplate.Editor
{
    [System.Serializable]
    public class ManifestData
    {
        public Dictionary<string, string> dependencies = new();
        public string[] scopedRegistries = new string[0];
        public string[] testables = new string[0];
    }

    [System.Serializable]
    public class InstallationState
    {
        public List<string> remainingPackages = new();
        public bool isInstalling = false;
        public int totalPackages = 0;
    }

    [System.Serializable]
    public class SubmoduleConfig
    {
        public string name = "";
        public string url = "";
        public string linkName = "";
    }

    [System.Serializable]
    public class AnalyzersConfig
    {
        public string submoduleName = "unity-analyzers";
        public string url = "https://github.com/void2610/unity-analyzers.git";
        public string projectPath = "src/Void2610.Unity.Analyzers";
    }

    [System.Serializable]
    public class ConfigFileEntry
    {
        public string source = "";
        public string destination = "projectRoot";
    }

    [System.Serializable]
    public class NugetPackageEntry
    {
        public string id = "";
        public string version = "";
    }

    [System.Serializable]
    public class RepoFileEntry
    {
        public string source = "";
        public string target = "";
    }

    [System.Serializable]
    public class RootSymlinkEntry
    {
        public string link = "";
        public string target = "";
    }

    [System.Serializable]
    public class KnowledgeSubmoduleConfig
    {
        public string path = "Knowledge/conventions";
        public string url = "https://github.com/void2610/okf-conventions.git";
    }

    [System.Serializable]
    public class DeployTargetConfig
    {
        public string name = "";
        public RepoFileEntry[] files = new RepoFileEntry[0];
    }

    [System.Serializable]
    public class TemplateConfigData
    {
        public string[] packages = new[]
        {
            "com.unity.render-pipelines.universal",
            "com.unity.textmeshpro",
            "com.unity.ide.rider",
            "com.unity.inputsystem"
        };
        public string[] gitPackages = new[]
        {
            "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
            "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
            "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
            "https://github.com/mob-sakai/UIEffect.git?path=Packages/src",
            "https://github.com/mob-sakai/UnmaskForUGUI.git",
            "https://github.com/naichilab/unityroom-client-library.git?path=Assets/unityroom",
            "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
            "https://github.com/AnnulusGames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
            "https://github.com/Yusuke57/UnityToolbarExtension.git",
            "https://github.com/Cysharp/ZLogger.git?path=src/ZLogger.Unity/Assets/ZLogger.Unity",
            "https://github.com/Cysharp/CsprojModifier.git?path=src/CsprojModifier/Assets/CsprojModifier",
            "https://github.com/hatayama/uLoopMCP.git?path=/Packages/src"
        };
        public string[] testables = new[]
        {
            "com.unity.inputsystem",
            "com.unity.ugui"
        };
        public string[] folderStructure = new[]
        {
            "Assets/Scripts",
            "Assets/Sprites",
            "Assets/Audio/BGM",
            "Assets/Audio/SE",
            "Assets/Materials",
            "Assets/Prefabs",
            "Assets/ScriptableObjects",
            "Assets/Editor",
            "Assets/Others"
        };
        public SubmoduleConfig[] submodules = new[]
        {
            new SubmoduleConfig { name = "my-unity-utils", url = "https://github.com/void2610/my-unity-utils.git", linkName = "Utils" },
            new SubmoduleConfig { name = "my-unity-settings", url = "https://github.com/void2610/my-unity-settings.git", linkName = "SettingsSystem" }
        };
        public AnalyzersConfig analyzers = new AnalyzersConfig();
        public ConfigFileEntry[] configFiles = new[]
        {
            new ConfigFileEntry { source = "Directory.Build.props", destination = "projectRoot" },
            new ConfigFileEntry { source = "csc.rsp", destination = "assets" },
            new ConfigFileEntry { source = ".editorconfig", destination = "projectRoot" },
            new ConfigFileEntry { source = "FormatCheck.csproj", destination = "projectRoot" },
            new ConfigFileEntry { source = "CLAUDE.md", destination = "projectRoot" }
        };
        public NugetPackageEntry[] nugetPackages = new[]
        {
            new NugetPackageEntry { id = "R3", version = "1.3.0" },
            new NugetPackageEntry { id = "ZLogger", version = "2.5.10" }
        };
        public string licenseFolderPath = "Assets/LicenseMaster";
        public RepoFileEntry[] repoFiles = new RepoFileEntry[0];
        public RootSymlinkEntry[] rootSymlinks = new RootSymlinkEntry[0];
        public KnowledgeSubmoduleConfig knowledgeSubmodule = new KnowledgeSubmoduleConfig();
        public DeployTargetConfig[] deployTargets = new DeployTargetConfig[0];
        public string[] gitignoreRules = new string[0];
    }
}
