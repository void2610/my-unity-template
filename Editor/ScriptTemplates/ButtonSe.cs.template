using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ボタンに効果音を自動追加するコンポーネント
    /// ホバー音とクリック音を設定して、統一された音響フィードバックを提供
    /// </summary>
    public class ButtonSe : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler, ISubmitHandler
    {
        [Header("効果音設定")]
        [SerializeField] private AudioClip hoverSe;
        [SerializeField] private float hoverVolume = 1.0f;
        [SerializeField] private AudioClip clickSe;
        [SerializeField] private float clickVolume = 1.0f;
        
        [Header("ピッチランダム化")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private float minPitch = 0.9f;
        [SerializeField] private float maxPitch = 1.1f;
        
        [Header("クールダウン")]
        [SerializeField] private float hoverCooldown = 0.1f; // ホバー音のクールダウン時間
        
        private Button _button;
        private AudioSource _audioSource;
        private float _lastHoverTime;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            
            // AudioSourceを取得または作成
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
            }
        }
        
        /// <summary>
        /// ホバー効果音を再生
        /// </summary>
        private void PlayHoverSound()
        {
            // ボタンが無効またはクールダウン中の場合は再生しない
            if ((_button != null && !_button.interactable) || 
                Time.time - _lastHoverTime < hoverCooldown ||
                hoverSe == null) 
                return;
            
            _lastHoverTime = Time.time;
            PlaySound(hoverSe, hoverVolume);
        }
        
        /// <summary>
        /// クリック効果音を再生
        /// </summary>
        private void PlayClickSound()
        {
            if ((_button != null && !_button.interactable) || clickSe == null) 
                return;
            
            PlaySound(clickSe, clickVolume);
        }
        
        /// <summary>
        /// 効果音を再生する汎用メソッド
        /// </summary>
        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null) return;
            
            _audioSource.clip = clip;
            _audioSource.volume = volume;
            
            // ピッチのランダム化
            if (randomizePitch)
            {
                _audioSource.pitch = Random.Range(minPitch, maxPitch);
            }
            else
            {
                _audioSource.pitch = 1f;
            }
            
            _audioSource.Play();
        }
        
        // EventSystemインターフェースの実装
        
        /// <summary>
        /// マウスホバー時
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// キーボード/コントローラー選択時
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// キーボード/コントローラー決定時
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// マウスクリック時
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// 効果音を手動で再生
        /// </summary>
        public void PlayHoverSoundManual()
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// クリック音を手動で再生
        /// </summary>
        public void PlayClickSoundManual()
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// 効果音の設定を更新
        /// </summary>
        public void UpdateAudioSettings(AudioClip newHoverSe = null, AudioClip newClickSe = null, 
                                       float newHoverVolume = -1f, float newClickVolume = -1f)
        {
            if (newHoverSe != null) hoverSe = newHoverSe;
            if (newClickSe != null) clickSe = newClickSe;
            if (newHoverVolume >= 0f) hoverVolume = newHoverVolume;
            if (newClickVolume >= 0f) clickVolume = newClickVolume;
        }
        
        /// <summary>
        /// ピッチランダム化の設定を更新
        /// </summary>
        public void UpdatePitchSettings(bool enableRandomPitch, float minPitchValue = 0.9f, float maxPitchValue = 1.1f)
        {
            randomizePitch = enableRandomPitch;
            minPitch = minPitchValue;
            maxPitch = maxPitchValue;
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでのテスト再生
        /// </summary>
        [ContextMenu("Test Hover Sound")]
        private void TestHoverSound()
        {
            if (Application.isPlaying)
                PlayHoverSound();
        }
        
        [ContextMenu("Test Click Sound")]
        private void TestClickSound()
        {
            if (Application.isPlaying)
                PlayClickSound();
        }
#endif
    }
}