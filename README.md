# Unity プロジェクトテンプレートパッケージ

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![Unity 6 Compatible](https://img.shields.io/badge/Unity_6-Compatible-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity開発を効率化するための包括的なテンプレートパッケージです。

## ✨ 何ができるか

- **ワンクリック環境構築**: 必要なライブラリとフォルダ構造を自動セットアップ
- **13個の実用Utilsスクリプト**: シングルトン、拡張メソッド、UI管理、アニメーション等
- **ライセンス管理**: ライブラリのライセンス情報を自動管理
- **レスポンシブ対応**: 異なる画面サイズでの一貫した表示

## 📦 インストール

1. Unity Package Managerを開く
2. **「+ > Add package from git URL」**を選択
3. 以下のURLを入力:
   ```
   https://github.com/void2610/my-unity-template.git
   ```

## 🚀 使い方

### 1. 依存関係をインストール
**Tools > Unity Template > Install Dependencies**

以下が自動インストールされます：
- URP、Input System等のUnity標準パッケージ
- R3、UniTask、VContainer等の人気ライブラリ
- NuGetForUnity

### 2. プロジェクト構造を作成
**Tools > Unity Template > Create Folder Structure**

### 3. Utilsスクリプトをコピー
**Tools > Unity Template > Copy Utility Scripts**

以下の13個の便利スクリプトが利用可能になります：

#### コア機能
- **SingletonMonoBehaviour** - シングルトンパターン
- **SerializableDictionary** - Inspector編集可能な辞書
- **ExtendedMethods** - 便利な拡張メソッド集

#### UI機能
- **MyButton** - 拡張ボタンコンポーネント
- **TMPInputFieldCaretFixer** - TextMeshProの入力フィールド修正
- **ButtonSe** - ボタン効果音
- **CanvasAspectRatioFitter** - レスポンシブUI

#### 表示・アニメーション
- **CameraAspectRatioHandler** - 固定アスペクト比
- **FloatMove** - 浮遊アニメーション
- **SpriteSheetAnimator** - 2Dフレームアニメーション

#### デバッグ・管理
- **DebugLogDisplay** - 画面上ログ表示
- **GameManager** - ゲーム管理（R3使用）
- **InputHandler** - 入力管理（R3使用）

### 4. R3をインストール
**Window > NuGetForUnity**で「R3」を検索してインストール

### 5. ライセンス管理（オプション）
1. [LicenseMaster](https://github.com/syskentokyo/unitylicensemaster/releases)をダウンロード・インポート
2. **Tools > Unity Template > Copy License Files**でライセンス管理開始

## 🎯 対象

- Unity 2022.3以降（Unity 6対応）
- 効率的な開発環境を求める開発者
- 品質の高いUtilsスクリプトが欲しいチーム

## 📄 ライセンス

MIT License