# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Package Manager (UPM) compatible template package for rapid Unity game development. The package provides automated setup for common Unity development patterns including URP, R3 reactive extensions, Input System, and structured project organization.

## Core Architecture

### Package Structure
- **Unity Package**: Follows UPM conventions with `package.json`, assembly definitions, and proper folder structure
- **Editor-Only Installation**: All setup tools run in Unity Editor via custom menu items (`Tools > Unity Template`)
- **Template-Based Approach**: Uses `.cs.template` files to avoid compile errors before dependencies are installed
- **Samples Integration**: Interactive tutorials demonstrate library usage in `Samples~/Tutorials/`

### Key Components

**TemplateMenuItems.cs** (`Editor/TemplateMenuItems.cs`)
- Main entry point for all package functionality (775 lines)
- Implements automated dependency installation with domain reload resilience
- Uses AssetDatabase for file access (not Resources folder to avoid build inclusion)
- Handles Unity 6 compatibility by automatically skipping incompatible packages
- Namespace: `Void2610.UnityTemplate.Editor`
- Classes: `TemplateManifestData`, `ManifestData`, `InstallationState`, `TemplateMenuItems`

**template-manifest.json** (`Editor/template-manifest.json`)
- Defines packages to install in structured format:
  - `packages`: Standard Unity packages (URP, TextMeshPro, Input System, Rider IDE)
  - `gitPackages`: Git-based packages (R3, UniTask, VContainer, LitMotion, UI Effect libraries)
  - `testables`: Packages available for testing

**ScriptTemplates** (`Editor/ScriptTemplates/`)
- Contains utility script templates for common Unity patterns:
  - `SingletonMonoBehaviour.cs.template`: Thread-safe singleton with automatic instantiation
  - `SerializableDictionary.cs.template`: Unity-serializable dictionary implementation
  - UI utilities: `MyButton`, `ButtonSe`, `TMPInputFieldCaretFixer`
  - Animation: `SpriteSheetAnimator`, `FloatMove`, `LitMotion` integration
  - System utilities: `ExtendedMethods`, `DebugLogDisplay`, camera/canvas aspect ratio handlers

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
Tools > Unity Template > Install Dependencies     # Install all packages from template-manifest.json
Tools > Unity Template > Create Folder Structure  # Create standard project folders
Tools > Unity Template > Copy Utility Scripts     # Copy .cs.template files to Assets/Scripts/Utils/
Tools > Unity Template > Copy Editor Scripts      # Copy editor-specific templates to Assets/Editor/
Tools > Unity Template > Copy License Files       # Copy license assets for LicenseMaster
```

### Package Development Workflow
```bash
# Local testing
# Unity Editor > Window > Package Manager > + > Add package from disk > select package.json

# Git URL installation for end users
# Unity Editor > Window > Package Manager > + > Add package from git URL
# Use: https://github.com/void2610/my-unity-template.git

# Modify dependencies
# Edit Editor/template-manifest.json to add/remove packages
# Test with "Install Dependencies" menu item
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