# Unity プロジェクトテンプレートパッケージ

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![Unity 6 Compatible](https://img.shields.io/badge/Unity_6-Compatible-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

URP、R3 リアクティブ拡張、Input System、整理されたプロジェクト構造を含む、高速ゲーム開発のためのUnityプロジェクトテンプレートパッケージです。

## ✨ 機能

- 🎨 **Universal Render Pipeline** - 最適化されたレンダリングパイプライン
- 🎮 **Input System** - モダンな入力処理システム
- ⚡ **R3 リアクティブ拡張** - Unity用リアクティブプログラミング（自動インストール）
- 🔧 **NuGetForUnity** - .NETパッケージ管理（自動インストール）
- 🗂️ **整理された構造** - スプライト、オーディオ、スクリプト用のクリーンなフォルダ構成
- 🛠️ **自動セットアップ** - 依存関係の自動インストールとプロジェクト構築
- 🚀 **Unity 6対応** - Unity 2022.3以降、Unity 6にも対応

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

### 1. 依存関係の自動インストール

**Tools > Unity Template > Install Dependencies** を実行:

- Unity パッケージ（URP、Input System等）を自動インストール
- NuGetForUnity を自動インストール
- R3 Unity モジュールを自動インストール
- Unity 6では互換性のないパッケージは自動的にスキップ

### 2. プロジェクト構造の作成

**Tools > Unity Template > Create Folder Structure** を実行:

- 整理されたフォルダ階層を自動作成
- GameManagerとInputHandlerの例をコピー

### 3. R3の最終セットアップ

1. **Window > NuGetForUnity** を開く
2. 「R3」を検索してインストール
3. Unityを再起動

### 4. 開発開始！

生成されるサンプルスクリプト:
- `GameManager.cs` - R3リアクティブプログラミングパターン
- `InputHandler.cs` - Input SystemとR3の統合

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

## 🛠️ 自動インストールされる依存関係

### Unity公式パッケージ
- **Universal Render Pipeline** - レンダリングパイプライン
- **Input System** - 入力処理システム
- **Addressables** - アセット管理システム
- **Visual Effect Graph** - ビジュアルエフェクト
- **Localization** - 多言語対応

### 外部パッケージ
- **NuGetForUnity** - .NETパッケージマネージャー
- **R3 Unity** - リアクティブプログラミングライブラリ

### Unity 6での互換性
- TextMeshPro、UGUIなど組み込みパッケージは自動的にスキップ
- 互換性のないパッケージも自動スキップして継続

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

1. **パッケージをインストール** (Unity Package Manager経由)
2. **Tools > Unity Template > Install Dependencies** で依存関係を自動インストール
3. **Tools > Unity Template > Create Folder Structure** でプロジェクト構造を作成
4. **Window > NuGetForUnity** でR3をインストール
5. **開発開始！**

## 🔧 トラブルシューティング

### インストール中にエラーが発生した場合
- パッケージインストール中にエラーが発生しても、他のパッケージのインストールは継続されます
- Unity 6で互換性のないパッケージは自動的にスキップされます
- ドメインリロード後も自動的にインストールが再開されます

### キャンセルしたい場合
- インストール中に再度 **Install Dependencies** を実行するとキャンセルオプションが表示されます

## ⚙️ カスタマイズ

### 依存関係のカスタマイズ

テンプレートの依存関係をカスタマイズする場合：

1. `Editor/Resources/template-manifest.json`を編集
2. `packages`配列でUnityパッケージを指定
3. `gitPackages`配列でGitパッケージを指定
4. 変更をコミット・プッシュ

### サポートされるUnityバージョン
- Unity 2022.3以降
- Unity 6に完全対応
- 互換性のないパッケージは自動的にスキップ

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