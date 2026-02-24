# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Package Manager (UPM) compatible template package for rapid Unity game development. The package provides automated setup for common Unity development patterns including URP, R3 reactive extensions, Input System, and structured project organization.

**This package is part of a 2-repository architecture:**
- **my-unity-template** (this repository): Automated installation tools and setup workflows
- **[my-unity-utils](https://github.com/void2610/my-unity-utils)**: Reusable utility scripts (integrated via Git Submodule)

## Core Architecture

### Package Structure
- **Unity Package**: Follows UPM conventions with `package.json`, assembly definitions, and proper folder structure
- **Editor-Only Installation**: All setup tools run in Unity Editor via custom menu items (`Tools > Unity Template`)
- **Git Submodule Integration**: Utility scripts are managed in a separate repository and added as a Git Submodule
- **Symbolic Link**: `Assets/Scripts/Utils/` symlink points to the submodule for seamless Unity integration
- **Samples Integration**: Interactive tutorials demonstrate library usage in `Samples~/Tutorials/`

### Key Components

**TemplateMenuItems.cs** (`Editor/TemplateMenuItems.cs`)
- Main entry point for all package functionality (970+ lines)
- Implements automated dependency installation with domain reload resilience
- **New:** Git Submodule setup with symbolic link creation (Windows/macOS/Linux compatible)
- Uses AssetDatabase for file access (not Resources folder to avoid build inclusion)
- Handles Unity 6 compatibility by automatically skipping incompatible packages
- Namespace: `Void2610.UnityTemplate.Editor`
- Classes: `TemplateManifestData`, `ManifestData`, `InstallationState`, `TemplateConfigData`, `SubmoduleConfig`, `AnalyzersConfig`, `ConfigFileEntry`, `TemplateMenuItems`
- Key Methods:
  - `InstallDependencies()`: Package installation
  - `SetupUtilsSubmodule()`: **New** - Automated submodule + symlink setup
  - `CreateSymlink()`: **New** - Cross-platform symlink creation
  - `ExecuteGitCommandSync()`: **New** - Git command execution helper

**template-config.json** (`Editor/template-config.json`)
- Central configuration file for all template settings (editable by other developers)
- Settings:
  - `packages`: Standard Unity packages (URP, TextMeshPro, Input System, Rider IDE)
  - `gitPackages`: Git-based packages (R3, UniTask, VContainer, LitMotion, UI Effect libraries)
  - `testables`: Packages available for testing
  - `folderStructure`: Array of folder paths to create (e.g. `Assets/Scripts`, `Assets/Sprites`)
  - `submodules`: Array of Git submodules with `name`, `url`, `linkName` for each
  - `analyzers`: Analyzer submodule settings (`submoduleName`, `url`, `projectPath`)
  - `nugetPackages`: NuGet packages with `id` and `version` (e.g. R3, ZLogger)
  - `configFiles`: Array of config files to copy with `source` and `destination` (`projectRoot` or `assets`)
  - `licenseFolderPath`: Destination folder for license files
- All values have sensible defaults in C# classes; if the file is missing or incomplete, defaults are used
- Loaded by `LoadTemplateConfig()` method with fallback to `TemplateConfigData` defaults

**my-unity-utils Submodule** (external repository)
- **Repository:** https://github.com/void2610/my-unity-utils
- **34 reusable Unity scripts** organized by category:
  - `UI/`: ButtonSe, MyButton, MultiImageButton, CanvasGroupSwitcher, etc.
  - `Animation/`: SpriteSheetAnimator, FloatMove
  - `Core/`: SingletonMonoBehaviour, ExtendedMethods, SerializableDictionary, Utils
  - `Audio/`: BgmManager, SeManager
  - `Debug/`: DebugLogDisplay, GameViewCapture, CurrentSelectedGameObjectChecker
  - `System/`: DataPersistence, RandomManager, IrisShot, VersionText, etc.
- Integrated via Git Submodule at project root: `<project>/my-unity-utils/`
- Linked to Unity via symlink: `Assets/Scripts/Utils/` → `../../my-unity-utils/`

**LicenseTemplates** (`Editor/LicenseTemplates/`)
- Pre-configured license assets for LicenseMaster integration
- Covers all included third-party libraries (R3, UniTask, VContainer, etc.)

### Installation Flow Architecture

1. **Package Detection**: Uses `AssetDatabase.FindAssets("TemplateMenuItems t:Script")` to locate package root
2. **Dependency Resolution**: Compares template manifest against current project manifest, skipping already-installed packages
3. **Sequential Installation**: Installs NuGetForUnity first (critical for R3), then other packages in queue
4. **State Persistence**: Uses EditorPrefs to survive domain reloads during package installation
5. **Error Resilience**: Automatically skips incompatible packages and continues installation
6. **Unity 6 Compatibility**: Detects built-in packages (`TextMeshPro`, `UGUI`) and skips installation

### Unity Version Compatibility

- **Unity 2022.3+**: Primary target with full feature support
- **Unity 6**: Full compatibility with automatic built-in package detection
- **Version Detection**: `Application.unityVersion` parsing with string/numeric comparison

## Development Commands

### Unity Editor Menu Items
All functionality accessed via Unity Editor menus under `Tools > Unity Template`:

```
Tools > Unity Template > Install Dependencies      # Install all packages from template-config.json
Tools > Unity Template > Create Folder Structure   # Create standard project folders
Tools > Unity Template > Setup Utils Submodule     # **NEW:** Add my-unity-utils submodule + create symlink
Tools > Unity Template > Copy Utility Scripts      # (Legacy) Copy .cs.template files - deprecated in favor of submodule
Tools > Unity Template > Copy Editor Scripts       # Copy editor-specific templates to Assets/Editor/
Tools > Unity Template > Copy License Files        # Copy license assets for LicenseMaster
```

**Recommended workflow:**
1. Install Dependencies
2. **Setup Utils Submodule** (instead of Copy Utility Scripts)
3. Create Folder Structure (optional)
4. Copy License Files (optional)

### Package Development Workflow
```bash
# Local testing
# Unity Editor > Window > Package Manager > + > Add package from disk > select package.json

# Git URL installation for end users
# Unity Editor > Window > Package Manager > + > Add package from git URL
# Use: https://github.com/void2610/my-unity-template.git

# Customize project settings
# Edit Editor/template-config.json to change packages, folders, submodules, config files, etc.
# Test with "Full Setup" or individual menu items

# Commit and push changes to update template
```

### Tutorial System
Interactive tutorials located in `Samples~/Tutorials/` demonstrate:
- **R3**: Reactive programming with ReactiveProperty, Subscribe patterns
- **UniTask**: Async/await patterns for Unity
- **VContainer**: Dependency injection with LifetimeScope
- **LitMotion**: High-performance animation library

## Technical Implementation Details

### File Access Pattern
AssetDatabase-based file location prevents build inclusion:

```csharp
// Pattern used throughout TemplateMenuItems.cs:783
var scriptFiles = AssetDatabase.FindAssets("TemplateMenuItems t:Script");
var scriptPath = AssetDatabase.GUIDToAssetPath(scriptFiles[0]);
var packagePath = Path.GetDirectoryName(scriptPath);
var filePath = Path.Combine(packagePath, "filename");
```

### State Management During Installation
Installation state persisted using EditorPrefs for domain reload survival:
- Key: `UnityTemplate_InstallState`
- Data: JSON serialized `InstallationState` class
- Restoration: `[InitializeOnLoad]` static constructor in TemplateMenuItems:709

### Git Package Deduplication
Advanced duplicate detection for Git packages using regex pattern matching:
```csharp
// TemplateMenuItems.cs:497 - IsSameGitPackage method
// Extracts "owner/repo/path" format from Git URLs for comparison
// Prevents duplicate installations of same package from different Git URLs
```

### Assembly Definition Structure
Three assembly definitions following Unity conventions:
- `com.void2610.unity-template.Runtime.asmdef`: Runtime scripts
- `com.void2610.unity-template.Editor.asmdef`: Editor-only scripts
- `com.void2610.unity-template.Tests.asmdef`: Test scripts

### Submodule and Symlink Architecture

**Overview:**
Utility scripts are managed in a separate Git repository (`my-unity-utils`) and integrated into Unity projects via Git Submodule + Symbolic Link.

**Directory Structure:**
```
<Unity Project>/
├─ my-unity-utils/                 ← Git Submodule (project root)
│   ├─ UI/
│   ├─ Animation/
│   ├─ Core/
│   ├─ Audio/
│   ├─ Debug/
│   └─ System/
├─ Assets/
│   └─ Scripts/
│       └─ Utils/                  ← Symbolic Link → ../../my-unity-utils
└─ Packages/
    └─ manifest.json
```

**Setup Process (TemplateMenuItems.cs:149-229):**
1. **Create Assets/Scripts folder** if it doesn't exist
2. **Add Git Submodule:**
   ```bash
   git submodule add https://github.com/void2610/my-unity-utils.git my-unity-utils
   git submodule update --init --recursive
   ```
3. **Create Symbolic Link:**
   - **Windows (Junction):** `mklink /J Assets\Scripts\Utils ..\..\my-unity-utils`
   - **macOS/Linux:** `ln -s ../../my-unity-utils Assets/Scripts/Utils`
4. **Refresh AssetDatabase** to make Unity recognize the scripts

**Cross-Platform Symlink Creation:**
```csharp
// Windows uses Junction (no admin rights required)
#if UNITY_EDITOR_WIN
    var args = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"";
    ExecuteShellCommand("cmd.exe", args);
#else
    // macOS/Linux use standard symlink
    ExecuteShellCommand("ln", $"-s \"{targetPath}\" \"{linkPath}\"");
#endif
```

**Git Considerations:**
- **Submodule tracked in `.gitmodules`** - committed to repository
- **Symlink NOT tracked** - OS-dependent, recreated locally
- **`.gitignore` entry:** `/Assets/Scripts/Utils` (but keep `Utils.meta`)

**Advantages:**
- ✅ Scripts managed in dedicated repository
- ✅ Shared across multiple projects
- ✅ Version controlled with Git tags/commits
- ✅ Unity treats them as regular Assets scripts
- ✅ Direct editing in Unity Editor
- ✅ No UPM complexity for simple script collection

**Workflow for Updates:**
```bash
# Edit scripts in Unity (Assets/Scripts/Utils/)
cd my-unity-utils
git add .
git commit -m "Update scripts"
git push

# Update parent project's submodule reference
cd ..
git add my-unity-utils
git commit -m "Update my-unity-utils submodule"
git push
```

## Namespace and Naming Conventions

- **Package ID**: `com.void2610.unity-template`
- **Editor Namespace**: `Void2610.UnityTemplate.Editor`
- **Runtime Namespace**: `Void2610.UnityTemplate`
- **Tutorial Namespace**: `void2610.UnityTemplate.Tutorials`
- **Assembly Naming**: Follows package ID with suffix (`.Runtime`, `.Editor`, `.Tests`)

## Error Handling and Diagnostics

### Installation Progress Logging
- Success: `✓ インストール成功: {packageName}`
- Warning: `⚠ 互換性の問題によりスキップしました`
- Domain reload: `=== パッケージインストールを再開します（残り: {count}個）===`

### Common Issues and Debugging
1. **Package Installation Failures**: Check Unity Console for detailed error messages
2. **Template File Missing**: Verify `ScriptTemplates/` folder structure and file permissions
3. **License Integration**: Ensure LicenseMaster is manually installed before using license features
4. **R3 Setup**: Requires manual NuGetForUnity installation after automated package setup

## External Dependencies and Setup Requirements

### Manual Installation Required
1. **LicenseMaster**: Download from GitHub releases, import UnityPackage manually
2. **R3**: Install via NuGetForUnity after automated package installation
3. **Unity Version**: Minimum Unity 2022.3 for full compatibility