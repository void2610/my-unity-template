using UnityEngine;

// UniTaskを使うには以下の名前空間をusingする
using Cysharp.Threading.Tasks;

namespace void2610.UnityTemplate.Tutorials
{
    public class UniTaskTutorial : MonoBehaviour
    {
        // asyncキーワードをつけて非同期関数を定義する
        // 返り値をUniTaskにすることで、非同期処理を行うことができる
        private async UniTask WaitAndPrint(string message)
        {
            // 1000ミリ秒待ってから、メッセージを表示する
            // 非同期関数の中では、awaitキーワードを使って他の非同期関数処理の終了を待てる
            await UniTask.Delay(1000);
            Debug.Log(message);
        }
        
        // UniTask<T>を使うと返り値を返せる
        private async UniTask<int> WaitAndReturn(int value)
        {
            // 1000ミリ秒待ってから、値を返す
            await UniTask.Delay(1000);
            return value;
        }
        
        // UniTaskVoidを使うと、外部でawaitできない非同期関数を作れる
        private async UniTaskVoid WaitAndPrintVoid(string message)
        {
            // 1000ミリ秒待ってから、メッセージを表示する
            await UniTask.Delay(1000);
            Debug.Log(message);
        }
        
        // MonoBehaviourのイベント関数も非同期関数にすることができる
        private async UniTask Start()
        {
            // UniTaskを使って非同期処理を行う
            await WaitAndPrint("Hello, UniTask!");
            // これ以降の処理は、WaitAndPrintが終わってから実行される
            
            Debug.Log("AAAAAAAAAA");
            
            // 非同期処理はawaitを使わずに呼び出すこともできる
            // その場合、警告が出るので、Forget()をつけて警告を消す
            WaitAndPrint("Goodbye, UniTask!").Forget();
            
            // 非同期関数から帰ってきた値を使うこともできる
            Debug.Log(await WaitAndReturn(100));

            // これはawaitできないのでエラーになる
            // await WaitAndPrintVoid("Hello, UniTask!");
        }
    }
}