using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace void2610.UnityTemplate.Tutorials
{
    // VContainerのエントリーポイントクラス
    // IInitializableを実装してVContainerのライフサイクルに組み込む
    public class VContainerTutorial : IInitializable
    {
        // インターフェースを通じて依存性注入される
        // VContainerが自動的にPlayerServiceとScoreServiceのインスタンスを注入
        private readonly IPlayerService _playerService;
        private readonly IScoreService _scoreService;
        
        // コンストラクタ注入
        // VContainerがLifetimeScopeで登録されたサービスを自動的に注入する
        public VContainerTutorial(IPlayerService playerService, IScoreService scoreService)
        {
            _playerService = playerService;
            _scoreService = scoreService;
            
            Debug.Log("VContainerTutorial: 依存性注入が完了しました");
        }
        
        // IInitializableの実装
        // VContainerによって自動的に呼び出される初期化メソッド
        public void Initialize()
        {
            Debug.Log("VContainerTutorial: Initialize()が呼び出されました");
            
            // 初期化処理を実行
            RunTutorial();
        }
        
        private void RunTutorial()
        {
            Debug.Log("=== VContainer 依存性注入チュートリアル ===");
            
            // プレイヤーサービスのテスト
            Debug.Log($"初期プレイヤー名: {_playerService.GetPlayerName()}");
            Debug.Log($"初期レベル: {_playerService.GetPlayerLevel()}");
            
            _playerService.SetPlayerName("TestPlayer");
            _playerService.LevelUp();
            _playerService.LevelUp();
            
            Debug.Log($"変更後プレイヤー名: {_playerService.GetPlayerName()}");
            Debug.Log($"変更後レベル: {_playerService.GetPlayerLevel()}");
            
            // スコアサービスのテスト
            Debug.Log($"初期スコア: {_scoreService.GetScore()}");
            
            _scoreService.AddScore(100);
            _scoreService.AddScore(250);
            
            Debug.Log($"スコア追加後: {_scoreService.GetScore()}");
            
            // 両サービスが連携して動作することを示す
            var bonusScore = _playerService.GetPlayerLevel() * 50;
            _scoreService.AddScore(bonusScore);
            
            Debug.Log($"レベルボーナス({bonusScore})追加後の最終スコア: {_scoreService.GetScore()}");
            
            Debug.Log("=== チュートリアル完了 ===");
            Debug.Log("VContainerにより、インターフェースを通じた依存性注入が正常に動作しました！");
        }
    }
}