# CLAUDE.md

Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンス。

## プロジェクト概要

<!-- プロジェクトの概要をここに記述してください -->
<!-- 例: ○○ は Unity 6000.x.xxf1 で開発されている○○プロジェクト。 -->

## 開発ワークフロー

1. コードを変更する
2. `mcp__uLoopMCP__compile` (ForceRecompile=true) でコンパイル
3. `mcp__uLoopMCP__get-logs` (LogType=Error) でコンパイルエラーがないことを確認
4. ロジック変更があれば `mcp__uLoopMCP__run-tests` (EditMode / 必要なら PlayMode) を回す
5. フォーマット自動修正を実行:
   - `dotnet format whitespace FormatCheck.csproj`
   - `dotnet format style FormatCheck.csproj --severity warn`
   - `dotnet format analyzers FormatCheck.csproj --severity warn`
6. **フォーマッタ / アナライザ / コンパイル / テストの出力は、末尾サマリ (`tail` や "Formatted N of M" 行)
   だけで判断せず必ず全文を確認する。** `warning` / `error` / `IDE` / `VUA` / `Unable to fix` 等の行が
   1 件でも残っていないか確認し、残っていれば対処してから次に進む。出力が長い場合は
   `grep -iE "warning|error|IDE[0-9]|VUA[0-9]|Unable to fix"` で取りこぼしを防ぐ
7. 結果をユーザーに報告する

## レスポンスガイドライン

- 常に簡潔なコメントを心がける。以前の実装からの変更点、削除した部分への言及などは書かない。後に有益な情報だけをコメントとして残す。
- メソッド、主要フィールド、分岐の意図が一読で分からない場合は、その場で説明コメントを追加する。
- フォーマッタやアナライザーが warning を出した場合は、まず `.editorconfig` や既存規約を自分で確認し、命名規則やスタイル違反を自力で直す。自動修正不能という出力をそのまま理由にして放置しない。
- **ツール出力は全文を確認する**。フォーマッタ / アナライザ / コンパイル / テストの結果を `tail` の末尾数行や
  "Formatted N of M files" のサマリだけ見て「OK」と判断しない。`warning` / `error` / `IDE10xx` / `VUAxxxx` /
  `Unable to fix` の行が残っていないかを全文 (必要なら grep) で確認してから完了報告する。
- 新規シンボル (特に定数・命名) を書くときは、その場で既存規約と照合する。定数は `ALL_UPPER` (例: `CURRENT_SCHEMA_VERSION`)。「手癖で PascalCase の定数を書く」等、既知の規約違反を新規コードに持ち込まない。
- 実装を進める際は、関連する設計や運用ルールのドキュメントも積極的に整理する。新しい仕組みを入れたら、必要に応じて `Docs/` やこのファイルへ判断基準と配置方針を残す。

## Unity 自動操作ツール（uloop / LiminalPalette）

役割分担を明確にすること：

- **uloop (`mcp__uLoopMCP__*`)**: 単発の操作・開発ワークフロー内のエディタ操作。コンパイル、テスト実行、シーン構造の確認、SerializeField ワイヤリング、スクリーンショット、メニュー操作など、Editor を外から叩く用途。
    **ランタイム（Play モード中）のゲーム動作確認には使わない**（`simulate-*` / `record-input` / `replay-input` は禁止）。
- **LiminalPalette (`liminal-*`)**: ランタイムでの動作確認とテストを**宣言的に記述・管理**する手段。
    単発の確認は `[LiminalCommand]` を実行する、再現性が要る検証や回帰テストは `[LiminalScenario]` としてコード化して実行する。「変更後にゲームを起動して目視で確認してください」は禁止。
  - **検証はできる限りシナリオ化する**。動作確認のために書いた手順は、そのまま `[LiminalScenario]` に落として残す。バグ修正・新機能・調整 — どんな変更でも検証手順をシナリオに足していくことで、**回帰テスト・統合テストのコーパスが副作用的に勝手に育っていく**。「検証作業」と「テスト資産の追加」を別タスクにしない（同じ作業の自然な副産物にする）。

**LiminalPalette はオーナー (void2610) 自身が開発しているライブラリ**である点に注意。
API が不足する・挙動が不便・バグっぽい等の問題に当たった場合は、クライアント側で workaround を組む前に **ライブラリ側の修正や API 追加を提案 / 実施する選択肢を必ず併せて検討する**こと。

## 設計原則

### DI設計原則

- VContainerを使用する
- シーンとLifetimeScopeを1:1で対応させる (例: TitleLifetimeScope, MainLifetimeScope)
- **ViewクラスのみMonoBehaviour継承**: Model、PresenterはピュアC#クラス
- **コンストラクタ注入**: Presenterはコンストラクタで依存関係を受け取る
- **Presenterでの取得**: コンストラクタで `FindFirstObjectByType<T>()` を使用してViewを取得

### nullチェック

- **SerializeField の null チェックは書かない**: カスタムアナライザ `VUA1001` が `[SerializeField]` 付きフィールドに対する防御的 null ガード (`x != null`, `x?.Member`, `x ?? y`, `is null` 等) を警告する。
    方針は「設定ミスは即座にクラッシュさせる」。SerializeField は Inspector で必ずアサインされる前提で書くこと。
- `FindFirstObjectByType` で View が見つからない場合、手動で null チェックを行わない。View がないままアクセスすれば null 参照例外で自動的にエラーが発生し、問題の原因が明確になる。

## 実装詳細

<!-- プロジェクト固有の実装詳細をここに記述してください -->
<!-- 例: VContainer設定、シーン構成、シーン遷移フローなど -->

## 依存パッケージ

- Utils (void2610/my-unity-utils) - 再利用可能なUnity Utilスクリプト群
- SettingsSystem (void2610/my-unity-settings) - 再利用可能なUnityのゲーム内設定システム
- LiminalPalette (void2610/liminal-palette) - Unity用コマンドパレットシステム。コマンドの定義と実行基盤を提供する
- uloop (hatayama/uLoopMCP) - AI-agentがUnityを操作可能にする
- unity-analyzers (void2610/unity-analyzers) - VUA カスタムアナライザ（命名規則・SerializeField null ガード検出など）
- VContainer (hadashiA/VContainer) - 依存性注入
- R3 (Cysharp/R3) - リアクティブプログラミング
- UniTask (Cysharp/UniTask) - 非同期処理
- LitMotion (AnnulusGames/LitMotion) - アニメーション
- Unity Template (void2610/my-unity-template) - プロジェクトテンプレート
