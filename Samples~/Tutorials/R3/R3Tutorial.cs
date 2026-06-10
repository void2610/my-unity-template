using UnityEngine;

// R3を使うには以下の名前空間をusingする
using R3;

namespace void2610.UnityTemplate.Tutorials
{
    public class R3Tutorial : MonoBehaviour
    {
        // ReactiveProperty<T>で、R3のObservableの機能を持った変数を作成できる
        private readonly ReactiveProperty<int> _playerHp = new(0);
        
        // 外部に公開する場合は、ReadOnlyReactiveProperty<T>にキャストすると、外部から値を変更できないが、Subscribeと値の取得はできる
        public ReadOnlyReactiveProperty<int> PlayerHp =>_playerHp;  

        
        private void OnValueChanged(int newValue)
        {
            Debug.Log($"player hp changed A: {newValue}");
        }
        
        private void Start()
        {
            // ReactivePropertyの値が変更されると、Subscribeで登録したメソッドが呼ばれる!!!!
            _playerHp.Subscribe(OnValueChanged);
            
            // こんな感じで、アロー演算子を使った匿名関数も使える
            _playerHp.Subscribe(newValue => Debug.Log($"player hp changed B: {newValue}"));
            
            
            // ForceNotifyを使うと、値の変更がなくても、強制的に通知を行える
            _playerHp.ForceNotify();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Valueプロパティを使って値を取得・設定する
                _playerHp.Value++;
            }
        }
    }
}