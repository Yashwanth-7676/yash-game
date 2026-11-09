using System;
using System.Collections;
using UnityEngine;

namespace YashGame.Shared
{
    /// <summary>
    /// Audio management singleton that handles music and sound effects playback.
    /// Supports volume control, muting, and crossfading between music tracks.
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        #region Fields

        [Header("Audio Sources")]
        [SerializeField, Tooltip("AudioSource dedicated to music playback.")]
        private AudioSource _musicSource;

        [SerializeField, Tooltip("AudioSource dedicated to sound effects playback.")]
        private AudioSource _sfxSource;

        [Header("Crossfade Settings")]
        [SerializeField, Tooltip("Duration of the crossfade transition in seconds.")]
        private float _crossfadeDuration = 1.5f;

        // PlayerPrefs keys
        private const string PREF_MUSIC_VOLUME = "YashGame_MusicVolume";
        private const string PREF_SFX_VOLUME = "YashGame_SFXVolume";
        private const string PREF_MUSIC_MUTED = "YashGame_MusicMuted";
        private const string PREF_SFX_MUTED = "YashGame_SFXMuted";

        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isMusicMuted;
        private bool _isSFXMuted;

        private Coroutine _crossfadeCoroutine;

        #endregion

        #region Properties

        /// <summary>Current music volume (0-1).</summary>
        public float MusicVolume => _musicVolume;

        /// <summary>Current SFX volume (0-1).</summary>
        public float SFXVolume => _sfxVolume;

        /// <summary>Whether music is currently muted.</summary>
        public bool IsMusicMuted => _isMusicMuted;

        /// <summary>Whether SFX is currently muted.</summary>
        public bool IsSFXMuted => _isSFXMuted;

        #endregion

        #region Unity Lifecycle

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            EnsureAudioSources();
            LoadVolumeSettings();
        }

        #endregion

        #region Public Methods – Music

        /// <summary>
        /// Plays the specified music clip. Replaces any currently playing music.
        /// </summary>
        /// <param name="clip">The music AudioClip to play.</param>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlayMusic called with null clip.");
                return;
            }

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            _musicSource.clip = clip;
            _musicSource.loop = true;
            _musicSource.volume = _isMusicMuted ? 0f : _musicVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public void StopMusic()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        /// <summary>
        /// Crossfades from the currently playing music track to a new one.
        /// </summary>
        /// <param name="newClip">The new music clip to crossfade to.</param>
        public void CrossfadeMusic(AudioClip newClip)
        {
            if (newClip == null)
            {
                Debug.LogWarning("[AudioManager] CrossfadeMusic called with null clip.");
                return;
            }

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip));
        }

        #endregion

        #region Public Methods – SFX

        /// <summary>
        /// Plays a one-shot sound effect.
        /// </summary>
        /// <param name="clip">The SFX AudioClip to play.</param>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySFX called with null clip.");
                return;
            }

            if (_isSFXMuted) return;

            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        [Header("Default UI Audio")]
        [SerializeField, Tooltip("Optional default UI click sound.")]
        private AudioClip _defaultClickClip;

        /// <summary>
        /// Plays a default UI button click sound.
        /// </summary>
        public void PlayButtonClick()
        {
            if (_defaultClickClip != null)
            {
                PlaySFX(_defaultClickClip);
            }
        }

        #endregion

        #region Public Methods – Volume

        /// <summary>
        /// Sets the music volume and saves to PlayerPrefs.
        /// </summary>
        /// <param name="volume">Volume level from 0 to 1.</param>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);

            if (!_isMusicMuted)
            {
                _musicSource.volume = _musicVolume;
            }

            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, _musicVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets the SFX volume and saves to PlayerPrefs.
        /// </summary>
        /// <param name="volume">Volume level from 0 to 1.</param>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);

            if (!_isSFXMuted)
            {
                _sfxSource.volume = _sfxVolume;
            }

            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _sfxVolume);
            PlayerPrefs.Save();
        }

        #endregion

        #region Public Methods – Muting

        /// <summary>
        /// Toggles the music mute state.
        /// </summary>
        /// <returns>True if music is now muted.</returns>
        public bool ToggleMusicMute()
        {
            _isMusicMuted = !_isMusicMuted;
            _musicSource.volume = _isMusicMuted ? 0f : _musicVolume;

            PlayerPrefs.SetInt(PREF_MUSIC_MUTED, _isMusicMuted ? 1 : 0);
            PlayerPrefs.Save();

            return _isMusicMuted;
        }

        /// <summary>
        /// Toggles the SFX mute state.
        /// </summary>
        /// <returns>True if SFX is now muted.</returns>
        public bool ToggleSFXMute()
        {
            _isSFXMuted = !_isSFXMuted;
            _sfxSource.volume = _isSFXMuted ? 0f : _sfxVolume;

            PlayerPrefs.SetInt(PREF_SFX_MUTED, _isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();

            return _isSFXMuted;
        }

        /// <summary>
        /// Sets the music mute state explicitly.
        /// </summary>
        /// <param name="muted">Whether music should be muted.</param>
        public void SetMusicMuted(bool muted)
        {
            _isMusicMuted = muted;
            _musicSource.volume = _isMusicMuted ? 0f : _musicVolume;

            PlayerPrefs.SetInt(PREF_MUSIC_MUTED, _isMusicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets the SFX mute state explicitly.
        /// </summary>
        /// <param name="muted">Whether SFX should be muted.</param>
        public void SetSFXMuted(bool muted)
        {
            _isSFXMuted = muted;
            _sfxSource.volume = _isSFXMuted ? 0f : _sfxVolume;

            PlayerPrefs.SetInt(PREF_SFX_MUTED, _isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures both AudioSources exist, creating them if missing.
        /// </summary>
        private void EnsureAudioSources()
        {
            if (_musicSource == null)
            {
                var musicGo = new GameObject("MusicSource");
                musicGo.transform.SetParent(transform);
                _musicSource = musicGo.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
            }

            if (_sfxSource == null)
            {
                var sfxGo = new GameObject("SFXSource");
                sfxGo.transform.SetParent(transform);
                _sfxSource = sfxGo.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.loop = false;
            }
        }

        /// <summary>
        /// Loads volume and mute settings from PlayerPrefs.
        /// </summary>
        private void LoadVolumeSettings()
        {
            _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
            _isMusicMuted = PlayerPrefs.GetInt(PREF_MUSIC_MUTED, 0) == 1;
            _isSFXMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;

            _musicSource.volume = _isMusicMuted ? 0f : _musicVolume;
            _sfxSource.volume = _isSFXMuted ? 0f : _sfxVolume;
        }

        /// <summary>
        /// Crossfade coroutine that smoothly transitions between two music tracks.
        /// </summary>
        /// <param name="newClip">The new clip to crossfade to.</param>
        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;
            float halfDuration = _crossfadeDuration * 0.5f;

            // Phase 1: Fade out current track
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            // Switch to new track
            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.loop = true;
            _musicSource.Play();

            // Phase 2: Fade in new track
            float targetVolume = _isMusicMuted ? 0f : _musicVolume;
            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            _musicSource.volume = targetVolume;
            _crossfadeCoroutine = null;
        }

        #endregion
    }
}
