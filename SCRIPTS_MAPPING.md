# ScriptTemplates to my-unity-utils Mapping

このファイルは、ScriptTemplatesフォルダからmy-unity-utilsリポジトリへの移行マッピングです。

## カテゴリ別ファイル配置

### UI/ (11ファイル)
- ButtonSe.cs
- ButtonSelectionGlow.cs
- ButtonTween.cs
- CanvasGroupSwitcher.cs
- FadeImageView.cs
- MultiImageButton.cs
- MyButton.cs
- SceneSwitchLeftButton.cs
- TextAutoSizer.cs
- TMPInputFieldCaretFixer.cs
- UILineRenderer.cs

### Animation/ (2ファイル)
- FloatMove.cs
- SpriteSheetAnimator.cs

### Core/ (4ファイル)
- ExtendedMethods.cs
- SerializableDictionary.cs
- SingletonMonoBehaviour.cs
- Utils.cs

### Audio/ (2ファイル)
- BgmManager.cs
- SeManager.cs

### Debug/ (3ファイル)
- CurrentSelectedGameObjectChecker.cs
- DebugLogDisplay.cs
- GameViewCapture.cs

### System/ (12ファイル)
- CameraAspectRatioHandler.cs
- CameraShake.cs
- CanvasAspectRatioFitter.cs
- CreditService.cs
- DataPersistence.cs
- DistanceUtility.cs
- InputActionExtensions.cs
- IrisShot.cs
- LicenseService.cs
- RandomManager.cs
- RenderTextureAspectManager.cs
- TweetService.cs
- VersionText.cs

## my-unity-utilsリポジトリ構造

```
my-unity-utils/
├─ UI/
│   ├─ ButtonSe.cs
│   ├─ ButtonSelectionGlow.cs
│   ├─ ButtonTween.cs
│   ├─ CanvasGroupSwitcher.cs
│   ├─ FadeImageView.cs
│   ├─ MultiImageButton.cs
│   ├─ MyButton.cs
│   ├─ SceneSwitchLeftButton.cs
│   ├─ TextAutoSizer.cs
│   ├─ TMPInputFieldCaretFixer.cs
│   └─ UILineRenderer.cs
├─ Animation/
│   ├─ FloatMove.cs
│   └─ SpriteSheetAnimator.cs
├─ Core/
│   ├─ ExtendedMethods.cs
│   ├─ SerializableDictionary.cs
│   ├─ SingletonMonoBehaviour.cs
│   └─ Utils.cs
├─ Audio/
│   ├─ BgmManager.cs
│   └─ SeManager.cs
├─ Debug/
│   ├─ CurrentSelectedGameObjectChecker.cs
│   ├─ DebugLogDisplay.cs
│   └─ GameViewCapture.cs
├─ System/
│   ├─ CameraAspectRatioHandler.cs
│   ├─ CameraShake.cs
│   ├─ CanvasAspectRatioFitter.cs
│   ├─ CreditService.cs
│   ├─ DataPersistence.cs
│   ├─ DistanceUtility.cs
│   ├─ InputActionExtensions.cs
│   ├─ IrisShot.cs
│   ├─ LicenseService.cs
│   ├─ RandomManager.cs
│   ├─ RenderTextureAspectManager.cs
│   ├─ TweetService.cs
│   └─ VersionText.cs
├─ README.md
└─ LICENSE
```

## 移行手順

1. GitHubでmy-unity-utilsリポジトリを作成
2. ローカルにクローン
3. 上記のフォルダ構造を作成
4. ScriptTemplatesから各.templateファイルを読み取り、.cs拡張子で保存
5. コミット＆プッシュ
