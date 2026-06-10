using UnityEngine;
using TMPro;

// LitMotionを使うには以下の名前空間をusingする
using LitMotion;
using LitMotion.Extensions;

namespace void2610.UnityTemplate.Tutorials
{
    public class LitMotionTutorialUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        private void Start()
        {
            // TextMeshProの情報を強制更新
            text.ForceMeshUpdate();
            
            // 各文字の初期状態を設定してからアニメーション開始
            for (var i = 0; i < text.textInfo.characterCount; i++)
            {
                // スペースや改行は除外
                if (!text.textInfo.characterInfo[i].isVisible) continue;
                
                // 色のアニメーション（白から黒、透明から不透明へ）
                LMotion.Create(new Color(1f, 1f, 1f, 0f), new Color(0f, 0f, 0f, 1f), 1f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutQuad)
                    .BindToTMPCharColor(text, i)
                    .AddTo(this.gameObject);
        
                // スケールのアニメーション（0から1へ）
                LMotion.Create(Vector3.zero, Vector3.one, 1f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutBack)
                    .BindToTMPCharScale(text, i)
                    .AddTo(this.gameObject);
                
                // 回転のアニメーション（45度から0度へ）
                LMotion.Create(45f, 0f, 0.5f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutQuad)
                    .BindToTMPCharEulerAnglesZ(text, i)
                    .AddTo(this.gameObject);
            }
        }
    }
}