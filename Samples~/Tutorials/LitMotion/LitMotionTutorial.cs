using UnityEngine;
using Cysharp.Threading.Tasks;

// LitMotionを使うには以下の名前空間をusingする
using LitMotion;
using LitMotion.Extensions;

namespace void2610.UnityTemplate.Tutorials
{
    public class LitMotionTutorial : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer object1;
        [SerializeField] private Transform object2;
        [SerializeField] private Transform object3;
        [SerializeField] private Transform object4;
        
        private async UniTask Start()
        {
            // ログにTweenの値を表示する
            LMotion.Create(0f, 1f, 1f) // 0fから1fまで1f(秒)かけて移動するTweenを作る
                .WithEase(Ease.OutSine) // イージング関数を指定する。OutSineは正弦波のように減速する
                .BindToUnityLogger() // それをUnityのLogに紐づけ
                .AddTo(this.gameObject) // このコンポーネントがアタッチされたGameObjectがDestroyされたらTweenが停止するようにする
                .ToUniTask().Forget(); // UniTaskに変換する。awaitしない場合はForget()して警告を消す。
            
            
            // 全てのTweenはawaitで待機することができる
            // イージング関数はここを参考に -> https://easings.net/ja
            
            // オブジェクトの透明度を変化させてみる
            await LMotion.Create(new Color(1, 1, 1, 1), new Color(0, 0, 0, 0), 1f)
                .WithEase(Ease.Linear) // 線形に変化
                .BindToColor(object1) // object1の色に紐づけ
                .AddTo(this.gameObject);
            
            // オブジェクトの座標を変化させてみる
            await LMotion.Create(new Vector3(0, 0, 0), new Vector3(0, 1, 0), 1f)
                .WithEase(Ease.OutBounce) // 最後にバウンドするように
                .BindToPosition(object2) // object2の位置に紐づけ
                .AddTo(this.gameObject);
            
            // オブジェクトの角度を変化させてみる
            await LMotion.Create(new Vector3(0, 0, 0), new Vector3(0, 0, 360), 1f)
                .WithEase(Ease.OutBack) // 最後に行きすぎて戻ってくるような
                .BindToEulerAngles(object3) // object3の回転に紐づけ
                .AddTo(this.gameObject);
            
            // オブジェクトの大きさを変化させてみる
            // WithLoop()を使うとループさせられる
            await LMotion.Create(new Vector3(1, 1, 1), new Vector3(2, 2, 2), 1f)
                .WithLoops(-1, LoopType.Yoyo) // ループを設定する。第一引数はループ回数。-1で無限ループに。第二引数はループの種類
                .WithEase(Ease.InOutCirc) // 急激に加速、減速
                .BindToLocalScale(object4) // object3の大きさに紐づけ
                .AddTo(this.gameObject);
        }

        private void Update()
        {
        }
    }
}