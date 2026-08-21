namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// Full Setup の 1 ステップ (タイトル + 実行処理)
    /// </summary>
    internal sealed class SetupStep
    {
        internal string Title { get; }
        internal System.Action<TemplateConfigData> Run { get; }

        internal SetupStep(string title, System.Action<TemplateConfigData> run)
        {
            Title = title;
            Run = run;
        }
    }
}
