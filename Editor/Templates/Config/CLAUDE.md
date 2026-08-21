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
5. コードを編集した場合は、必ず `./unity-coding-standards/scripts/run-format.sh` を実行する
   （analyzer の Release ビルド → `dotnet format analyzers / whitespace / style` をまとめて流す。個別の `dotnet format` を直接叩かない）
6. フォーマッタ / アナライザ / コンパイル / テストの出力を全文確認する（後述「ツール出力は全文を確認する」）
7. 結果をユーザーに報告する

## レスポンスガイドライン

- 常に簡潔なコメントを心がける。以前の実装からの変更点、削除した部分への言及などは書かない。後に有益な情報だけをコメントとして残す。
- メソッド、主要フィールド、分岐の意図が一読で分からない場合は、その場で説明コメントを追加する。
- フォーマッタやアナライザーが warning を出した場合は、まず `.editorconfig` や既存規約を自分で確認し、命名規則やスタイル違反を自力で直す。自動修正不能という出力をそのまま理由にして放置しない。
- **ツール出力は全文を確認する**。フォーマッタ / アナライザ / コンパイル / テストの結果を `tail` の末尾数行や
  "Formatted N of M files" のサマリだけ見て「OK」と判断しない。`warning` / `error` / `IDE10xx` / `VUAxxxx` /
  `Unable to fix` の行が 1 件でも残っていないかを全文（出力が長い場合は
  `grep -iE "warning|error|IDE[0-9]|VUA[0-9]|Unable to fix"`）で確認し、残っていれば対処してから完了報告する。
- 新規シンボル (特に定数・命名) を書くときは、その場で既存規約と照合する。定数は `ALL_UPPER` (例: `CURRENT_SCHEMA_VERSION`)。「手癖で PascalCase の定数を書く」等、既知の規約違反を新規コードに持ち込まない。
- 実装を進める際は、関連する設計や運用ルールのドキュメントも積極的に整理する。新しい仕組みを入れたら、必要に応じて `Docs/` やこのファイルへ判断基準と配置方針を残す。

## コーディング規約・フォーマット (unity-coding-standards)

フォーマットと静的解析は **unity-coding-standards** サブモジュール（`./unity-coding-standards/`）に集約されている。

- **フォーマット**: `./unity-coding-standards/scripts/run-format.sh` が唯一の入口。
  カスタムアナライザを Release ビルドしてから `dotnet format` の analyzers / whitespace / style を順に流すため、
  個別コマンドの直叩きや一部だけの実行をしない。CI は `--verify-no-changes` 付きで同じスクリプトを使う。
- **カスタムアナライザ (VUA)**: `unity-coding-standards/src/Void2610.Unity.Analyzers` がプロジェクト固有規約を強制する。
  代表例は下記「nullチェック」の `VUA1001`。警告が出たら規約に従って直す（抑制しない）。
- **ツール運用ドキュメント**: uloop / LiminalPalette の使い分けや注意点は
  [`docs/uloop.md`](./unity-coding-standards/docs/uloop.md) と
  [`docs/liminal-palette.md`](./unity-coding-standards/docs/liminal-palette.md) を参照。

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

- **SerializeField の null チェックは書かない**: アナライザ `VUA1001` が `[SerializeField]` 付きフィールドに対する防御的 null ガード (`x != null`, `x?.Member`, `x ?? y`, `is null` 等) を警告する。
    方針は「設定ミスは即座にクラッシュさせる」。SerializeField は Inspector で必ずアサインされる前提で書くこと。
- `FindFirstObjectByType` で View が見つからない場合、手動で null チェックを行わない。View がないままアクセスすれば null 参照例外で自動的にエラーが発生し、問題の原因が明確になる。

## 実装詳細

<!-- プロジェクト固有の実装詳細をここに記述してください -->
<!-- 例: VContainer設定、シーン構成、シーン遷移フローなど -->

## 依存パッケージ

本プロジェクトには以下のライブラリが導入されている。汎用処理を自前で書く前に、まずこれらで実現できないか検討すること。

### 自作ライブラリ (void2610)

- **Utils** (my-unity-utils) - 再利用 Unity スクリプト群を `Assets/Scripts/Utils` にシンボリックリンクで取り込む。UI / Animation / Audio / Core / Debug / System 系（`SingletonMonoBehaviour`, `SeManager` / `BgmManager`, `DataPersistence`, `ExtendedMethods`, `SerializableDictionary` など）。
- **SettingsSystem** (my-unity-settings) - ゲーム内設定システム（MVP）。設定項目の定義と UI への双方向バインドを提供する。
- **LiminalPalette** (liminal-palette) - ランタイムのコマンド / シナリオ実行基盤。`[LiminalCommand]` / `[LiminalScenario]` でランタイム検証を宣言的に書く（上記参照）。
- **unity-coding-standards** - VUA カスタムアナライザ + `run-format.sh` + ツール運用ドキュメント（上記「コーディング規約・フォーマット」参照）。

### DI / リアクティブ / 非同期 / アニメーション

- **VContainer** (hadashiA/VContainer) - 依存性注入。
- **R3** (Cysharp/R3) - リアクティブプログラミング。状態は `ReactiveProperty` で公開し、購読で UI / 演出を駆動する。
- **UniTask** (Cysharp/UniTask) - async/await ベースの非同期処理。コルーチンより優先する。
- **LitMotion** (AnnulusGames/LitMotion) - 高性能アニメーション（Tween）。DOTween は使わずこちらを使う。

### ログ / UI 演出 / テスト / その他

- **ZLogger** (Cysharp/ZLogger) - 構造化・高速ロギング。
- **UIEffect** (mob-sakai/UIEffect) - UI のシェーダ演出（ブラー / シャドウ / グラデーション等）。
- **UnmaskForUGUI** (mob-sakai/UnmaskForUGUI) - UI マスクのくり抜き（チュートリアルのハイライト等）。
- **NuGetForUnity** - NuGet パッケージ管理（R3 等のコア依存の導入に使う）。
- **CsprojModifier** (Cysharp) - `.csproj` 生成のカスタマイズ（アナライザ / フォーマット連携）。
- **test-helper** (nowsprinting/test-helper) - PlayMode / EditMode テスト補助ユーティリティ。
- **unityroom-client** (naichilab) - unityroom へのスコア送信。
- **uloop** (hatayama/uLoopMCP) - AI-agent が Unity Editor を操作する MCP サーバ（上記参照）。

### Unity 標準

- URP (2D) / TextMeshPro / Input System / 2D ツール各種 / Test Framework など。
