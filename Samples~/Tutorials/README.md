# Unity Template チュートリアル

このフォルダには、テンプレートに含まれる主要ライブラリのチュートリアルが含まれています。

## チュートリアルシーンのセットアップ方法

各チュートリアルを実行するには、以下の手順でシーンをセットアップしてください：

### 1. R3 チュートリアル
1. 新しいシーンを作成し、`Assets/Tutorials/R3/R3Tutorial.unity`として保存
2. 空のGameObjectを作成し、`R3Tutorial.cs`をアタッチ
3. UIテスト用：
   - Canvas と TextMeshPro Text を作成
   - 別のGameObjectに`R3TutorialUI.cs`をアタッチ
   - Inspectorで`R3Tutorial`と`Text`の参照を設定

### 2. LitMotion チュートリアル
1. 新しいシーンを作成し、`Assets/Tutorials/LitMotion/LitMotionTutorial.unity`として保存
2. 空のGameObjectを作成し、`LitMotionTutorial.cs`をアタッチ
3. テスト用オブジェクトの準備：
   - SpriteRenderer付きのGameObject × 1
   - 通常のGameObject × 3
   - Inspectorで`object1`～`object4`に割り当て

### 3. UniTask チュートリアル
1. 新しいシーンを作成し、`Assets/Tutorials/UniTask/UniTaskTutorial.unity`として保存
2. 空のGameObjectを作成し、`UniTaskTutorial.cs`をアタッチ
3. Consoleウィンドウでログを確認

## 実行方法
- 各シーンを開いてPlayボタンを押す
- R3: スペースキーでHP増加
- LitMotion: 自動でアニメーション実行
- UniTask: コンソールで非同期処理の動作確認

## 注意事項
- 各チュートリアルを実行する前に、`Tools > Unity Template > Install Dependencies`でライブラリをインストールしてください
- 名前空間は`void2610.UnityTemplate.Tutorials`で統一されているため、実際のプロジェクトコードと干渉しません