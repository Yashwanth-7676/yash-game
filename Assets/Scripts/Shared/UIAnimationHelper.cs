using System.Collections;
using TMPro;
using UnityEngine;

namespace YashGame.Shared
{
    /// <summary>
    /// Provides static utility methods for common UI animations.
    /// All methods return IEnumerator and should be used with StartCoroutine.
    /// </summary>
    public static class UIAnimationHelper
    {
        #region Easing Curves

        /// <summary>
        /// Creates a smooth ease-in-out AnimationCurve.
        /// </summary>
        private static AnimationCurve SmoothEase => AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>
        /// Creates an overshoot ease-out curve (great for scale-in effects).
        /// Overshoots to ~1.1 then settles at 1.0.
        /// </summary>
        private static AnimationCurve OvershootEase
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, 0f, 0f, 2f));
                curve.AddKey(new Keyframe(0.7f, 1.1f, 0f, 0f));
                curve.AddKey(new Keyframe(1f, 1f, -0.5f, 0f));
                return curve;
            }
        }

        /// <summary>
        /// Creates an ease-out curve (fast start, slow finish).
        /// </summary>
        private static AnimationCurve EaseOut
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, 0f, 2f, 2f));
                curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
                return curve;
            }
        }

        #endregion

        #region Fade Animations

        /// <summary>
        /// Fades a CanvasGroup from its current alpha to fully opaque.
        /// </summary>
        /// <param name="canvasGroup">The CanvasGroup to fade.</param>
        /// <param name="duration">Duration of the fade in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
        {
            if (canvasGroup == null) yield break;

            AnimationCurve curve = SmoothEase;
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Fades a CanvasGroup from its current alpha to fully transparent.
        /// </summary>
        /// <param name="canvasGroup">The CanvasGroup to fade.</param>
        /// <param name="duration">Duration of the fade in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator FadeOut(CanvasGroup canvasGroup, float duration)
        {
            if (canvasGroup == null) yield break;

            AnimationCurve curve = SmoothEase;
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Scale Animations

        /// <summary>
        /// Scales a transform from zero to one with an overshoot ease effect.
        /// </summary>
        /// <param name="target">The Transform to animate.</param>
        /// <param name="duration">Duration of the animation in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator ScaleIn(Transform target, float duration)
        {
            if (target == null) yield break;

            AnimationCurve curve = OvershootEase;
            float elapsed = 0f;

            target.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                target.localScale = Vector3.one * t;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        #endregion

        #region Slide Animations

        /// <summary>
        /// Slides a RectTransform from an offset position to its current anchored position.
        /// </summary>
        /// <param name="target">The RectTransform to animate.</param>
        /// <param name="fromOffset">Starting position offset relative to the current anchored position.</param>
        /// <param name="duration">Duration of the slide in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator SlideIn(RectTransform target, Vector2 fromOffset, float duration)
        {
            if (target == null) yield break;

            AnimationCurve curve = EaseOut;
            Vector2 endPos = target.anchoredPosition;
            Vector2 startPos = endPos + fromOffset;
            float elapsed = 0f;

            target.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                target.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            target.anchoredPosition = endPos;
        }

        #endregion

        #region Continuous Animations

        /// <summary>
        /// Continuously pulses a transform's scale (runs indefinitely until stopped).
        /// </summary>
        /// <param name="target">The Transform to pulse.</param>
        /// <param name="scale">Maximum scale multiplier (e.g., 1.1 for a subtle pulse).</param>
        /// <param name="duration">Duration of one full pulse cycle in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator PulseAnimation(Transform target, float scale, float duration)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale * scale;
            float halfDuration = duration * 0.5f;

            while (true)
            {
                // Scale up
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                    target.localScale = Vector3.Lerp(originalScale, targetScale, t);
                    yield return null;
                }

                // Scale down
                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                    target.localScale = Vector3.Lerp(targetScale, originalScale, t);
                    yield return null;
                }
            }
        }

        #endregion

        #region Text Animations

        /// <summary>
        /// Types out text character by character in a typewriter effect.
        /// </summary>
        /// <param name="textComponent">The TMP_Text component to animate.</param>
        /// <param name="text">The full text to type out.</param>
        /// <param name="charDelay">Delay between each character in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator TypewriterText(TMP_Text textComponent, string text, float charDelay)
        {
            if (textComponent == null || string.IsNullOrEmpty(text)) yield break;

            textComponent.text = "";
            var builder = new System.Text.StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                builder.Append(text[i]);
                textComponent.text = builder.ToString();
                yield return new WaitForSecondsRealtime(charDelay);
            }

            textComponent.text = text;
        }

        #endregion

        #region Button Animations

        /// <summary>
        /// Quick button press animation — scales down briefly then back up.
        /// </summary>
        /// <param name="target">The button Transform to animate.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator ButtonPressAnimation(Transform target)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 pressedScale = originalScale * 0.85f;
            float pressDuration = 0.06f;
            float releaseDuration = 0.1f;

            // Scale down
            float elapsed = 0f;
            while (elapsed < pressDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / pressDuration);
                target.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }

            // Scale back up with slight overshoot
            AnimationCurve curve = OvershootEase;
            elapsed = 0f;
            while (elapsed < releaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / releaseDuration));
                target.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }

            target.localScale = originalScale;
        }

        #endregion
    }
}
