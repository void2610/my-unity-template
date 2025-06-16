# Unity Project Template Package

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive Unity project template package featuring URP 2D, R3 reactive extensions, Input System, and organized project structure for rapid game development.

## ✨ Features

- 🎨 **URP 2D Pipeline** - Optimized Universal Render Pipeline for 2D games
- 🎮 **Input System** - Modern input handling with pre-configured actions
- ⚡ **R3 Reactive Extensions** - Reactive programming for Unity
- 📝 **TextMesh Pro** - Enhanced text rendering
- 🗂️ **Organized Structure** - Clean folder organization for sprites, audio, scripts
- 🛠️ **Editor Tools** - Custom menu items and utilities
- 📋 **Scene Templates** - Pre-configured 2D URP scene templates

## 📦 Installation

### Via Unity Package Manager (Git URL)

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button in the top-left corner
3. Select **Add package from git URL...**
4. Enter the following URL:
   ```
   https://github.com/your-username/my-unity-template.git
   ```
5. Click **Add**

### Via Package Manager (Local)

1. Clone or download this repository
2. Open Unity and go to **Window > Package Manager**
3. Click the **+** button and select **Add package from disk...**
4. Navigate to the downloaded folder and select `package.json`

## 🚀 Quick Start

### 1. Import Project Template

After installing the package:

1. Go to **Window > Package Manager**
2. Find "Shuya's Unity Project Template" in the list
3. Expand **Samples** section
4. Click **Import** next to "Project Template Files"

### 2. Use Template Tools

Access the template tools via **Tools > Unity Template** menu:

- **Create New Scene from Template** - Creates a new scene with URP 2D setup
- **Setup Project Settings** - Configures optimal settings for 2D development
- **Create Folder Structure** - Creates organized folder hierarchy
- **Open Documentation** - Opens this documentation

### 3. Start Developing

The template provides example scripts demonstrating:

- `GameManager.cs` - R3 reactive programming patterns
- `InputHandler.cs` - Input System integration with R3
- Scene templates for quick scene creation
- Organized folder structure for assets

## 📁 Package Structure

```
├── package.json                    # Package manifest
├── README.md                      # This file
├── LICENSE                        # MIT License
├── Runtime/                       # Runtime scripts and assets
│   └── com.shuya.unity-template.Runtime.asmdef
├── Editor/                        # Editor scripts and tools
│   ├── TemplateMenuItems.cs      # Custom menu items
│   └── com.shuya.unity-template.Editor.asmdef
├── Tests/                         # Test scripts
│   └── com.shuya.unity-template.Tests.asmdef
└── Samples~/
    └── ProjectTemplate/           # Complete project template
        ├── Scripts/               # Example scripts
        ├── Scenes/               # Template scenes
        ├── Settings/             # Project settings
        └── ...                   # Other template assets
```

## 🛠️ Dependencies

This package automatically includes:

- **Universal Render Pipeline** (com.unity.render-pipelines.universal)
- **TextMesh Pro** (com.unity.textmeshpro)
- **Input System** (com.unity.inputsystem)
- **R3** (via NuGet)

## 📖 Usage Examples

### Reactive Game Manager

```csharp
using R3;
using UnityEngine;

public class Example : MonoBehaviour
{
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        // React to score changes
        gameManager.Score.Subscribe(score => 
        {
            Debug.Log($"Score: {score}");
        }).AddTo(this);
    }
}
```

### Input Handling

```csharp
using R3;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputHandler inputHandler;
    
    void Start()
    {
        inputHandler = FindObjectOfType<InputHandler>();
        
        // React to movement input
        inputHandler.MoveInput.Subscribe(movement => 
        {
            transform.Translate(movement * Time.deltaTime);
        }).AddTo(this);
    }
}
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- [Unity Package Manager Documentation](https://docs.unity3d.com/Manual/upm-ui.html)
- [R3 Reactive Extensions](https://github.com/Cysharp/R3)
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)

---

Made with ❤️ for the Unity community