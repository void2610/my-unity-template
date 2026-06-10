# Unity プロジェクトテンプレートパッケージ

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![Unity 6 Compatible](https://img.shields.io/badge/Unity_6-Compatible-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity開発を効率化するためのテンプレートパッケージ

## ✨ 機能

- **ワンクリック環境構築**: 必要なライブラリとフォルダ構造を自動セットアップ
- **Utilsスクリプト**: Git Submodule経由で34個の再利用可能なスクリプトを提供
- **ライセンス管理**: ライブラリのライセンス情報を自動管理
- Unity 2022.3以降（Unity 6対応）

## 📦 構成

このテンプレートは2つのリポジトリで構成されています：

- **my-unity-template** (このリポジトリ): 自動インストール機能とセットアップツール
- **[my-unity-utils](https://github.com/void2610/my-unity-utils)**: 再利用可能なUtilsスクリプト集（Git Submodule経由で導入）

## 🔧 インストール

### 1. テンプレートのインストール

Unity Package Managerから：
1. **「+ > Add package from git URL」**を選択
2. 以下のURLを入力:
   ```
   https://github.com/void2610/my-unity-template.git
   ```

## 🚀 セットアップ手順

### 1. Utilsスクリプトのセットアップ

**Tools > Unity Template > Setup Utils Submodule**

これにより自動的に：
- プロジェクトルートに`my-unity-utils` submoduleが追加されます
- `Assets/Scripts/Utils/`がシンボリックリンクとして作成されます
- 34個のUtilsスクリプトが利用可能になります

**含まれるスクリプト:**
- **UI**: ButtonSe, MyButton, MultiImageButton, CanvasGroupSwitcher等
- **Animation**: SpriteSheetAnimator, FloatMove等
- **Core**: SingletonMonoBehaviour, ExtendedMethods, SerializableDictionary等
- **Audio**: BgmManager, SeManager
- **Debug**: DebugLogDisplay, GameViewCapture等
- **System**: DataPersistence, RandomManager, IrisShot等

### 2. 依存関係をインストール

**Tools > Unity Template > Install Dependencies**

以下が自動インストールされます：
- URP、Input System等のUnity標準パッケージ
- R3、UniTask、VContainer、LitMotion等の汎用ライブラリ
- NuGetForUnity

### 3. R3をインストール（手動）

**Window > NuGetForUnity**で「R3」を検索してインストール

### 4. プロジェクト構造を作成（オプション）

**Tools > Unity Template > Create Folder Structure**

### 5. ライセンス管理（オプション）

1. [LicenseMaster](https://github.com/syskentokyo/unitylicensemaster/releases)をダウンロード・インポート
2. **Tools > Unity Template > Copy License Files**でライセンス管理開始

---

## 🔄 プロジェクトのクローン

既存のプロジェクトをクローンする場合：

```bash
# Submodule込みでクローン
git clone --recursive <プロジェクトURL>
cd <プロジェクト>

# シンボリックリンクを再作成（OS依存のためGitに含まれない）
# macOS/Linux:
ln -s ../../my-unity-utils Assets/Scripts/Utils

# Windows (コマンドプロンプト):
mklink /J Assets\Scripts\Utils ..\..\my-unity-utils
```

または：

```bash
# 通常クローン後にSubmodule初期化
git clone <プロジェクトURL>
cd <プロジェクト>
git submodule update --init --recursive

# シンボリックリンク作成（上記参照）
```

---

## 📝 Utilsスクリプトの更新

Utilsスクリプトを編集した場合：

```bash
# Assets/Scripts/Utils/で編集（実体はmy-unity-utils/）

# Submodule側でコミット
cd my-unity-utils
git add .
git commit -m "Update utility scripts"
git push

# プロジェクト側でSubmodule参照を更新
cd ..
git add my-unity-utils
git commit -m "Update my-unity-utils reference"
git push
```

最新のUtilsスクリプトを取得：

```bash
cd my-unity-utils
git pull origin main
cd ..
git add my-unity-utils
git commit -m "Update my-unity-utils submodule"
```

---

## 🛠 手動セットアップ（詳細）

自動セットアップが使えない場合：

### Submodule追加
```bash
git submodule add https://github.com/void2610/my-unity-utils.git my-unity-utils
```

### シンボリックリンク作成

**Windows (コマンドプロンプト):**
```cmd
mklink /J Assets\Scripts\Utils ..\..\my-unity-utils
```

**macOS/Linux:**
```bash
ln -s ../../my-unity-utils Assets/Scripts/Utils
```

---

## 📚 関連リポジトリ

- **[my-unity-utils](https://github.com/void2610/my-unity-utils)**: Utilsスクリプト集

---

## 📄 ライセンス

MIT License
