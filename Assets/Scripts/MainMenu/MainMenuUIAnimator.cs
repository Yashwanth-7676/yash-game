using System.Collections;
using UnityEngine;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Handles visual animations for the main menu scene: title effects, background movement,
    /// staggered button entrance, tagline typewriter, and continuous floating animations.
    /// </summary>
    public class MainMenuUIAnimator : MonoBehaviour
    {
        #region Title Settings

        [Header("Title Animation")]
        [SerializeField, Tooltip("The title TMP_Text component.")]
        private TMP_Text _titleText;

        [SerializeField, Tooltip("Scale breathing amplitude (e.g., 1.05 for subtle).")]
        private float _titleBreathScale = 1.05f;

        [SerializeField, Tooltip("Duration of one full breathing cycle in seconds.")]
        private float _titleBreathDuration = 2.0f;

        [SerializeField, Tooltip("Primary title color.")]
        private Color _titleColorA = Color.white;

        [SerializeField, Tooltip("Secondary title color for glow pulse.")]
        private Color _titleColorB = new Color(0.6f, 0.8f, 1f, 1f);

        [SerializeField, Tooltip("Speed of the title color pulse.")]
        private float _titleColorSpeed = 1.5f;

        #endregion

        #region Background Settings

        [Header("Background Animation")]
        [SerializeField, Tooltip("Background material with UV offset support.")]
        private Renderer _backgroundRenderer;

        [SerializeField, Tooltip("UV scroll speed for the background.")]
        private Vector2 _uvScrollSpeed = new Vector2(0.02f, 0.01f);

        [SerializeField, Tooltip("Primary background tint color.")]
        private Color _bgColorA = new Color(0.05f, 0.05f, 0.15f, 1f);

        [SerializeField, Tooltip("Secondary background tint color.")]
        private Color _bgColorB = new Color(0.1f, 0.05f, 0.2f, 1f);

        [SerializeField, Tooltip("Background color shift speed.")]
        private float _bgColorShiftSpeed = 0.5f;

        #endregion

        #region Button Animation Settings

        [Header("Button Animations")]
        [SerializeField, Tooltip("Array of button RectTransforms for staggered entrance.")]
        private RectTransform[] _buttons;

        [SerializeField, Tooltip("Direction offset for button slide-in (e.g., (-300, 0) from left).")]
        private Vector2 _buttonSlideOffset = new Vector2(-300f, 0f);

        [SerializeField, Tooltip("Duration of each button's slide-in animation.")]
        private float _buttonSlideDuration = 0.5f;

        [SerializeField, Tooltip("Delay between consecutive button animations.")]
        private float _buttonStaggerDelay = 0.12f;

        [SerializeField, Tooltip("Continuous floating animation amplitude in pixels.")]
        private float _floatAmplitude = 4f;

        [SerializeField, Tooltip("Continuous floating animation speed.")]
        private float _floatSpeed = 1.5f;

        #endregion

        #region Tagline Settings

        [Header("Tagline Animation")]
        [SerializeField, Tooltip("The tagline TMP_Text component for typewriter effect.")]
        private TMP_Text _taglineText;

        [SerializeField, Tooltip("The text to type out as the tagline.")]
        private string _taglineContent = "Choose your adventure...";

        [SerializeField, Tooltip("Delay between each character appearing.")]
        private float _typewriterCharDelay = 0.04f;

        [SerializeField, Tooltip("Delay before tagline typing starts.")]
        private float _taglineStartDelay = 1.0f;

        #endregion

        #region Private State

        private Material _bgMaterialInstance;
        private Vector2 _currentUVOffset;
        private Coroutine _titlePulseCoroutine;
        private Coroutine _titleBreathCoroutine;

        // Cache property IDs to avoid string lookups
        private static readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // Clean up instanced material to prevent memory leaks
            if (_bgMaterialInstance != null)
            {
                Destroy(_bgMaterialInstance);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call this to begin all entrance animations. Typically called by MainMenuController.Start().
        /// </summary>
        public void PlayEntranceAnimations()
        {
            // Title animations
            if (_titleText != null)
            {
                _titleBreathCoroutine = StartCoroutine(TitleBreathingAnimation());
                _titlePulseCoroutine = StartCoroutine(TitleColorPulse());
            }

            // Background animation
            if (_backgroundRenderer != null)
            {
                // Use an instanced material so we don't modify the shared one
                _bgMaterialInstance = new Material(_backgroundRenderer.sharedMaterial);
                _backgroundRenderer.material = _bgMaterialInstance;
                StartCoroutine(BackgroundAnimation());
            }

            // Button entrance
            if (_buttons != null && _buttons.Length > 0)
            {
                StartCoroutine(StaggeredButtonEntrance());
            }

            // Tagline typewriter
            if (_taglineText != null)
            {
                StartCoroutine(TaglineTypewriter());
            }
        }

        #endregion

        #region Title Animations

        /// <summary>
        /// Continuously scales the title text up and down with a breathing effect.
        /// </summary>
        private IEnumerator TitleBreathingAnimation()
        {
            if (_titleText == null) yield break;

            Transform titleTransform = _titleText.transform;
            // Use the pulse animation helper for continuous breathing
            yield return UIAnimationHelper.PulseAnimation(titleTransform, _titleBreathScale, _titleBreathDuration);
        }

        /// <summary>
        /// Continuously cycles the title text color between two colors for a glow pulse effect.
        /// </summary>
        private IEnumerator TitleColorPulse()
        {
            if (_titleText == null) yield break;

            float time = 0f;

            while (true)
            {
                time += Time.deltaTime * _titleColorSpeed;
                float t = (Mathf.Sin(time * Mathf.PI) + 1f) * 0.5f; // Normalized sine wave 0-1
                _titleText.color = Color.Lerp(_titleColorA, _titleColorB, t);
                yield return null;
            }
        }

        #endregion

        #region Background Animations

        /// <summary>
        /// Continuously scrolls the background UV and shifts background color.
        /// </summary>
        private IEnumerator BackgroundAnimation()
        {
            if (_bgMaterialInstance == null) yield break;

            float colorTime = 0f;

            while (true)
            {
                // UV scrolling
                _currentUVOffset += _uvScrollSpeed * Time.deltaTime;
                _bgMaterialInstance.SetTextureOffset(MainTexPropertyId, _currentUVOffset);

                // Color shifting
                colorTime += Time.deltaTime * _bgColorShiftSpeed;
                float t = (Mathf.Sin(colorTime * Mathf.PI) + 1f) * 0.5f;
                Color currentColor = Color.Lerp(_bgColorA, _bgColorB, t);

                // Try both URP and standard property names
                if (_bgMaterialInstance.HasProperty(BaseColorPropertyId))
                {
                    _bgMaterialInstance.SetColor(BaseColorPropertyId, currentColor);
                }
                else if (_bgMaterialInstance.HasProperty(ColorPropertyId))
                {
                    _bgMaterialInstance.SetColor(ColorPropertyId, currentColor);
                }

                yield return null;
            }
        }

        #endregion

        #region Button Animations

        /// <summary>
        /// Animates buttons into view one by one with slide-in and then starts continuous floating.
        /// </summary>
        private IEnumerator StaggeredButtonEntrance()
        {
            // Wait a moment for the scene to settle
            yield return new WaitForSecondsRealtime(0.3f);

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null)
                {
                    StartCoroutine(UIAnimationHelper.SlideIn(_buttons[i], _buttonSlideOffset, _buttonSlideDuration));
                    yield return new WaitForSecondsRealtime(_buttonStaggerDelay);
                }
            }

            // After all buttons have entered, start continuous floating
            yield return new WaitForSecondsRealtime(_buttonSlideDuration);

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null)
                {
                    StartCoroutine(ContinuousFloat(_buttons[i], i));
                }
            }
        }

        /// <summary>
        /// Applies a continuous subtle floating animation to a RectTransform.
        /// Each button gets a phase offset based on its index to create a wave effect.
        /// </summary>
        /// <param name="target">The RectTransform to animate.</param>
        /// <param name="index">Index used for phase offset.</param>
        private IEnumerator ContinuousFloat(RectTransform target, int index)
        {
            if (target == null) yield break;

            Vector2 basePosition = target.anchoredPosition;
            float phaseOffset = index * 0.5f; // Offset phase per button

            while (true)
            {
                float offset = Mathf.Sin((Time.time + phaseOffset) * _floatSpeed) * _floatAmplitude;
                target.anchoredPosition = basePosition + new Vector2(0f, offset);
                yield return null;
            }
        }

        #endregion

        #region Tagline Animation

        /// <summary>
        /// Plays a typewriter effect on the tagline text after a delay.
        /// </summary>
        private IEnumerator TaglineTypewriter()
        {
            if (_taglineText == null) yield break;

            _taglineText.text = "";
            yield return new WaitForSecondsRealtime(_taglineStartDelay);
            yield return UIAnimationHelper.TypewriterText(_taglineText, _taglineContent, _typewriterCharDelay);
        }

        #endregion
    }
}
