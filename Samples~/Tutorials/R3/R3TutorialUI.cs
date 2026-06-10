using UnityEngine;

// R3を使うには以下の名前空間をusingする
using R3;
using TMPro;

namespace void2610.UnityTemplate.Tutorials
{
    public class R3TutorialUI : MonoBehaviour
    {
        [SerializeField] private R3Tutorial r3Tutorial;
        [SerializeField] private TMP_Text text;
        private void Start()
        {
            // 値が変更されたときに自動でUIが更新される！！！
            // これだけ覚えとけば多分大丈夫
            r3Tutorial.PlayerHp.Subscribe(newValue =>
            {
                text.text = $"Player HP: {newValue}";
            });
        }
    }
}