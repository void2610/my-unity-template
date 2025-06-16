# Unity プロジェクトテンプレートパッケージ

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

URP 2D、R3 リアクティブ拡張、Input System、整理されたプロジェクト構造を含む、高速ゲーム開発のための包括的なUnityプロジェクトテンプレートパッケージです。

## ✨ 機能

- 🎨 **URP 2D パイプライン** - 2Dゲーム用に最適化されたUniversal Render Pipeline
- 🎮 **Input System** - 事前設定されたアクションを持つモダンな入力処理
- ⚡ **R3 リアクティブ拡張** - Unity用リアクティブプログラミング
- 📝 **TextMesh Pro** - 強化されたテキストレンダリング
- 🗂️ **整理された構造** - スプライト、オーディオ、スクリプト用のクリーンなフォルダ構成
- 🛠️ **エディタツール** - カスタムメニュー項目とユーティリティ
- 📋 **シーンテンプレート** - 事前設定済みの2D URPシーンテンプレート

## 📦 インストール

### Unity Package Manager経由（Git URL）

1. Unityを開き、**Window > Package Manager**に移動
2. 左上の**「+」ボタン**をクリック
3. **「Add package from git URL...」**を選択
4. 以下のURLを入力:
   ```
   https://github.com/void2610/my-unity-template.git
   ```
5. **「Add」**をクリック

### Package Manager経由（ローカル）

1. このリポジトリをクローンまたはダウンロード
2. Unityを開き、**Window > Package Manager**に移動
3. **「+」ボタン**をクリックし、**「Add package from disk...」**を選択
4. ダウンロードしたフォルダに移動し、`package.json`を選択

## 🚀 クイックスタート

### 1. テンプレートツールの使用

**Tools > Unity Template**メニューからテンプレートツールにアクセス:

- **Install Dependencies** - R3とNuGetForUnityを自動インストール
- **Create New 2D URP Scene** - 2D URP設定で新しいシーンを作成
- **Setup Project Settings** - 2D開発用の最適な設定を構成
- **Create Folder Structure** - 整理されたフォルダ階層を作成
- **Create Example Scripts** - GameManagerとInputHandlerの例を生成
- **Open Documentation** - このドキュメントを開く

### 2. 開発開始

テンプレートツールは以下の実例スクリプトを生成します:

- `GameManager.cs` - R3リアクティブプログラミングパターン
- `InputHandler.cs` - Input SystemとR3の統合
- 2D URPシーンの作成
- アセット用の整理されたフォルダ構造

## 📁 パッケージ構造

```
├── package.json                    # パッケージマニフェスト
├── README.md                      # このファイル
├── LICENSE                        # MITライセンス
├── Runtime/                       # ランタイムスクリプトとアセット
│   └── com.void2610.unity-template.Runtime.asmdef
├── Editor/                        # エディタスクリプトとツール
│   ├── TemplateMenuItems.cs      # カスタムメニュー項目
│   └── com.void2610.unity-template.Editor.asmdef
└── Tests/                         # テストスクリプト
    └── com.void2610.unity-template.Tests.asmdef
```

## 🛠️ 依存関係

このパッケージは以下を自動的に含みます:

- **Universal Render Pipeline** (com.unity.render-pipelines.universal)
- **TextMesh Pro** (com.unity.textmeshpro)
- **Input System** (com.unity.inputsystem)
- **R3** (NuGet経由)

## 📖 使用例

### リアクティブゲームマネージャー

```csharp
using R3;
using UnityEngine;

public class Example : MonoBehaviour
{
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        // スコア変更に反応
        gameManager.Score.Subscribe(score => 
        {
            Debug.Log($"スコア: {score}");
        }).AddTo(this);
    }
}
```

### 入力処理

```csharp
using R3;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputHandler inputHandler;
    
    void Start()
    {
        inputHandler = FindObjectOfType<InputHandler>();
        
        // 移動入力に反応
        inputHandler.MoveInput.Subscribe(movement => 
        {
            transform.Translate(movement * Time.deltaTime);
        }).AddTo(this);
    }
}
```

## 🚀 ワークフロー例

1. **パッケージをインストール**
2. **Tools > Unity Template > Install Dependencies** で依存関係をインストール
   - NuGetForUnityとR3 Unityモジュールが自動インストール
   - Window > NuGetForUnity からR3コアモジュールをインストール
3. **Tools > Unity Template > Create Folder Structure** でフォルダ構造を作成
4. **Tools > Unity Template > Create New 2D URP Scene** で新しいシーンを作成
5. **Tools > Unity Template > Setup Project Settings** でプロジェクト設定を最適化
6. 開発開始！

## 📋 依存関係のインストール手順

### 自動インストール
1. **Tools > Unity Template > Install Dependencies** を実行
2. NuGetForUnityとR3 Unityモジュールが自動でインストールされます

### 手動でR3コアモジュールをインストール
1. **Window > NuGetForUnity** を開く
2. 検索ボックスに「**R3**」と入力
3. **R3** パッケージをインストール
4. **Microsoft.Bcl.AsyncInterfaces** もインストール（依存関係）
5. Unityを再起動

これでR3リアクティブプログラミングが利用可能になります！

## 🤝 コントリビューション

コントリビューションを歓迎します！プルリクエストをお気軽に送信してください。

## 📄 ライセンス

このプロジェクトはMITライセンスの下でライセンスされています - 詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 🔗 リンク

- [Unity Package Manager ドキュメント](https://docs.unity3d.com/Manual/upm-ui.html)
- [R3 リアクティブ拡張](https://github.com/Cysharp/R3)
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)

---

❤️ Unityコミュニティのために作成