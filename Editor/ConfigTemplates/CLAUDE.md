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

<!-- フォーマット標準を unity-coding-standards で管理する場合は、4 を ./unity-coding-standards/scripts/run-format.sh に置き換える -->
<!-- ロジック変更時は mcp__uLoopMCP__run-tests (EditMode / 必要なら PlayMode) を回す。ランタイム動作確認は LiminalPalette のシナリオに寄せる -->

## レスポンスガイドライン

<!-- プロジェクト固有のレスポンスガイドラインをここに記述してください -->
<!-- 例: フォーマッタ / アナライザ / コンパイル / テストの出力は末尾サマリだけで判断せず全文を確認する -->

## 主要コマンド

```bash
# フォーマット自動修正（whitespace + コードスタイル + カスタムアナライザー）
dotnet format whitespace FormatCheck.csproj
dotnet format style FormatCheck.csproj --severity warn
dotnet format analyzers FormatCheck.csproj --severity warn
```

コンパイルは `mcp__uLoopMCP__compile` (ForceRecompile=true) を使用し、その後 `mcp__uLoopMCP__get-logs` (LogType=Error) でエラーを確認する。

## 設計原則

<!-- プロジェクト固有の設計原則をここに記述してください -->
<!-- 例: VContainer による DI（View のみ MonoBehaviour、Model / Presenter はピュア C#、Presenter はコンストラクタ注入、Scene と LifetimeScope を 1:1） -->
<!-- 例: SerializeField の null チェック方針（unity-analyzers の VUA1001 に従い防御的 null ガードを書かない） -->

## 実装詳細

<!-- プロジェクト固有の実装詳細をここに記述してください -->
<!-- 例: VContainer設定、シーン構成、シーン遷移フローなど -->
<!-- 例: ランタイム検証・回帰テストは LiminalPalette のシナリオで管理 -->
<!-- 例: 設定システムは my-unity-settings (SettingsSystem) を使用 -->

## 依存パッケージ

- VContainer (hadashiA/VContainer) - 依存性注入
- R3 (Cysharp/R3) - リアクティブプログラミング
- UniTask (Cysharp/UniTask) - 非同期処理
- LitMotion (AnnulusGames/LitMotion) - アニメーション
- Unity Template (void2610/my-unity-template) - プロジェクトテンプレート
