# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Package Manager (UPM) compatible template package for rapid Unity game development. The package provides automated setup for common Unity development patterns including URP, R3 reactive extensions, Input System, and structured project organization.

## Core Architecture

### Package Structure
- **Unity Package**: Follows UPM conventions with `package.json`, assembly definitions, and proper folder structure
- **Editor-Only Installation**: All setup tools run in Unity Editor via custom menu items (`Tools > Unity Template`)
- **Template-Based Approach**: Uses `.txt` template files to avoid compile errors before dependencies are installed

### Key Components

**TemplateMenuItems.cs** (`Editor/TemplateMenuItems.cs`)
- Main entry point for all package functionality
- Implements automated dependency installation with domain reload resilience
- Uses AssetDatabase for file access (not Resources folder to avoid build inclusion)
- Handles Unity 6 compatibility by automatically skipping incompatible packages

**template-manifest.json** (`Editor/template-manifest.json`)
- Defines packages to install in structured format:
  - `packages`: Standard Unity packages (installs latest stable versions)
  - `gitPackages`: Git-based packages with specific repository paths
  - `testables`: Packages that should be available for testing

**ScriptTemplates** (`Editor/ScriptTemplates/`)
- Contains `.txt` template files for GameManager and InputHandler
- Copied to user projects during folder structure creation
- Demonstrates R3 reactive programming patterns

### Installation Flow Architecture

1. **Package Detection**: Uses `AssetDatabase.FindAssets()` to locate template files relative to the TemplateMenuItems script
2. **Dependency Resolution**: Compares template manifest against current project manifest, skipping already-installed packages
3. **Sequential Installation**: Installs NuGetForUnity first, then other packages in queue
4. **State Persistence**: Uses EditorPrefs to survive domain reloads during package installation
5. **Error Resilience**: Automatically skips incompatible packages and continues installation

### Unity Version Compatibility

- **Unity 2022.3+**: Primary target
- **Unity 6**: Full compatibility with automatic built-in package detection
- **Version Detection**: `Application.unityVersion` parsing to adjust behavior

## Development Commands

### Testing Package Installation
```bash
# In Unity Editor, test the main functionality:
# Tools > Unity Template > Install Dependencies
# Tools > Unity Template > Create Folder Structure
```

### Package Development
```bash
# Install package locally for testing
# Unity Editor > Window > Package Manager > + > Add package from disk > select package.json

# Install via Git URL for testing
# Unity Editor > Window > Package Manager > + > Add package from git URL
# Use: https://github.com/void2610/my-unity-template.git
```

### Customizing Dependencies
```bash
# Edit Editor/template-manifest.json to modify packages
# Commit and push changes to update the template
```

## File Access Pattern

The package uses AssetDatabase instead of Resources folder to prevent template files from being included in builds:

```csharp
// Pattern used throughout TemplateMenuItems.cs
var scriptFiles = AssetDatabase.FindAssets("TemplateMenuItems t:Script");
var scriptPath = AssetDatabase.GUIDToAssetPath(scriptFiles[0]);
var packagePath = Path.GetDirectoryName(scriptPath);
var filePath = Path.Combine(packagePath, "filename");
```

## State Management

Installation state is persisted using EditorPrefs to handle Unity's domain reloads:
- `UnityTemplate_InstallState`: JSON serialized installation queue and progress
- Automatic restoration via `[InitializeOnLoad]` static constructor

## Package Naming Convention

- Namespace: `Void2610.UnityTemplate.Editor`
- Package ID: `com.void2610.unity-template`
- Assembly definitions follow Unity conventions with package ID prefix

## Debugging Installation Issues

- Check Unity Console for detailed installation logs
- Installation progress is logged with emoji indicators (✓, ⚠)
- Failed packages are skipped with warning messages
- Domain reload continuation is logged with "=== パッケージインストールを再開します ==="