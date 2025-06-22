using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace void2610.UnityTemplate.Tutorials
{
    // VContainerのLifetimeScopeは、依存性注入の範囲を定義する
    // このクラスでサービスやコンポーネントの登録を行う
    public class VContainerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Pure C#クラスをシングルトンとして登録
            // 1つのインスタンスがアプリケーション全体で共有される
            builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
            builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
            
            // エントリーポイントとして登録
            // VContainerTutorialがゲーム開始時に自動的に依存性注入され、Initialize()が呼ばれる
            builder.RegisterEntryPoint<VContainerTutorial>();
            
            Debug.Log("VContainer: 依存性の登録が完了しました");
        }
    }
    
    // プレイヤー情報を管理するサービスのインターフェース
    public interface IPlayerService
    {
        string GetPlayerName();
        void SetPlayerName(string name);
        int GetPlayerLevel();
        void LevelUp();
    }
    
    // プレイヤーサービスの実装
    public class PlayerService : IPlayerService
    {
        private string _playerName = "DefaultPlayer";
        private int _playerLevel = 1;
        
        public string GetPlayerName() => _playerName;
        public void SetPlayerName(string name) => _playerName = name;
        public int GetPlayerLevel() => _playerLevel;
        public void LevelUp() => _playerLevel++;
        
        public PlayerService()
        {
            Debug.Log("PlayerService: インスタンスが作成されました");
        }
    }
    
    // スコア管理サービスのインターフェース
    public interface IScoreService
    {
        int GetScore();
        void AddScore(int points);
        void ResetScore();
    }
    
    // スコアサービスの実装
    public class ScoreService : IScoreService
    {
        private int _score = 0;
        
        public int GetScore() => _score;
        public void AddScore(int points) => _score += points;
        public void ResetScore() => _score = 0;
        
        public ScoreService()
        {
            Debug.Log("ScoreService: インスタンスが作成されました");
        }
    }
}
