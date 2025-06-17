# Unity プロジェクトテンプレートパッケージ

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-green.svg)](https://unity3d.com/get-unity/download)
[![Unity 6 Compatible](https://img.shields.io/badge/Unity_6-Compatible-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity開発を効率化するためのテンプレートパッケージ

## ✨ 機能

- **ワンクリック環境構築**: 必要なライブラリとフォルダ構造を自動セットアップ
- **Utilsスクリプト**: シングルトン、拡張メソッド、UI管理、アニメーション等
- **ライセンス管理**: ライブラリのライセンス情報を自動管理
- Unity 2022.3以降（Unity 6対応）

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
- R3、UniTask、VContainer等の汎用ライブラリ
- NuGetForUnity

### 2. R3をインストール
**Window > NuGetForUnity**で「R3」を検索してインストール

### 3. プロジェクト構造を作成
**Tools > Unity Template > Create Folder Structure**

### 4. Utilsスクリプトをコピー
**Tools > Unity Template > Copy Utility Scripts**


### 5. ライセンス管理（オプション）
1. [LicenseMaster](https://github.com/syskentokyo/unitylicensemaster/releases)をダウンロード・インポート
2. **Tools > Unity Template > Copy License Files**でライセンス管理開始


## 📄 ライセンス

MIT License
