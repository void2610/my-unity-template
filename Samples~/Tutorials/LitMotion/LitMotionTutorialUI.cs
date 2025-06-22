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
            for (var i = 0; i < text.textInfo.characterCount; i++)
            {
                LMotion.Create(Color.white, Color.black, 1f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutQuad)
                    .BindToTMPCharColor(text, i);
        
                LMotion.Create(Vector3.zero, Vector3.one, 1f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutQuad)
                    .BindToTMPCharScale(text, i);
                
                LMotion.Create(45f, 0f, 0.5f)
                    .WithDelay(i * 0.1f)
                    .WithEase(Ease.OutQuad)
                    .BindToTMPCharEulerAnglesZ(text, i);
            }
        }
    }
}