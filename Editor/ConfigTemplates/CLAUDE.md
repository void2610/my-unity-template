# CLAUDE.md

Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンス。

## プロジェクト概要

<!-- プロジェクトの概要をここに記述してください -->
<!-- 例: ○○ は Unity 6000.x.xxf1 で開発されている○○プロジェクト。 -->

## 開発ワークフロー

1. コードを変更する
2. `mcp__uLoopMCP__compile` (ForceRecompile=true) でコンパイル
3. `mcp__uLoopMCP__get-logs` (LogType=Error) でコンパイルエラーがないことを確認
4. フォーマット自動修正を実行:
   - `dotnet format whitespace FormatCheck.csproj`
   - `dotnet format style FormatCheck.csproj --severity warn`
   - `dotnet format analyzers FormatCheck.csproj --severity warn`
5. 結果をユーザーに報告する

**YAGNI (You Aren't Gonna Need It) 原則:**
- 将来必要になるかもしれない機能を先回りして実装しない
- 現在の要求を満たす最小限の実装に留める
- 過度な抽象化や汎用化を避ける
- 実際に必要になった時点で機能を追加する

**KISS (Keep It Simple, Stupid) 原則:**
- 可能な限りシンプルで理解しやすいコードを書く
- 複雑な設計パターンは本当に必要な場合のみ使用
- 1つのクラス・メソッドには1つの責任のみを持たせる
- 誰でも理解できる明快な実装を優先する

## レスポンスガイドライン

- SerializeFieldで設定されるべきコンポーネントのnullチェックは不要。コンポーネントが正しく設定されていることを前提としたコードを記述する
- プログラム内の全てのコメントは日本語で記述する
- 日本語で応答し、過剰なコメントは避ける
- リファクタリング時は後方互換性を維持せず、クリーンな置き換えを実装する
- YAGNI・KISS原則に従い、今必要なものだけを実装し、シンプルに保つ

## 主要コマンド

```bash
# フォーマット自動修正（whitespace + コードスタイル + カスタムアナライザー）
dotnet format whitespace FormatCheck.csproj
dotnet format style FormatCheck.csproj --severity warn
dotnet format analyzers FormatCheck.csproj --severity warn
```

コンパイルは `mcp__uLoopMCP__compile` (ForceRecompile=true) を使用し、その後 `mcp__uLoopMCP__get-logs` (LogType=Error) でエラーを確認する。

## 設計原則

- **明確なファイル構造**: 機能別ディレクトリで保守性向上
- **責任の明確化**: 各レイヤーの責任を明確に分離
- **Unity-First**: MonoBehaviourパターンの自然な活用
- **リアクティブ設計**: R3による状態変更の伝播

## 実装詳細

<!-- プロジェクト固有の実装詳細をここに記述してください -->
<!-- 例: VContainer設定、シーン構成、シーン遷移フローなど -->

## 依存パッケージ

- VContainer (hadashiA/VContainer) - 依存性注入
- R3 (Cysharp/R3) - リアクティブプログラミング
- UniTask (Cysharp/UniTask) - 非同期処理
- LitMotion (AnnulusGames/LitMotion) - アニメーション
- Unity Template (void2610/my-unity-template) - プロジェクトテンプレート
