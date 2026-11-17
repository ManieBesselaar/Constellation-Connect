namespace Lifestories.UI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Fades the alpha of a CanvasGroup over a given duration, and toggles its interactivity to match.
    /// Each CanvasGroup is tracked separately, so fades on different panels never interrupt one another.
    /// </summary>
    public class CanvasGroupFader : MonoBehaviour
    {
        private readonly Dictionary<CanvasGroup, Coroutine> _fadeRoutines = new Dictionary<CanvasGroup, Coroutine>();

        /// <summary>
        /// Fades a CanvasGroup in to fully opaque and makes it interactive.
        /// </summary>
        /// <param name="canvasGroup">The group to fade. Ignored when null.</param>
        /// <param name="duration">Fade duration in unscaled seconds.</param>
        public void FadeIn(CanvasGroup canvasGroup, float duration)
        {
            startFade(canvasGroup, 1f, duration, true);
        }

        /// <summary>
        /// Fades a CanvasGroup out to fully transparent and stops it receiving input.
        /// </summary>
        /// <param name="canvasGroup">The group to fade. Ignored when null.</param>
        /// <param name="duration">Fade duration in unscaled seconds.</param>
        public void FadeOut(CanvasGroup canvasGroup, float duration)
        {
            startFade(canvasGroup, 0f, duration, false);
        }

        private void startFade(CanvasGroup canvasGroup, float targetAlpha, float duration, bool interactive)
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning("[CanvasGroupFader] Fade ignored: the supplied CanvasGroup is null.");
                return;
            }

            if (_fadeRoutines.TryGetValue(canvasGroup, out Coroutine runningRoutine) && runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            canvasGroup.interactable = interactive;
            canvasGroup.blocksRaycasts = interactive;
            _fadeRoutines[canvasGroup] = StartCoroutine(executeFadeRoutine(canvasGroup, targetAlpha, duration));
        }

        /* Lerps from the group's current alpha so an interrupted fade never pops. */
        private IEnumerator executeFadeRoutine(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;

            if (duration > 0f)
            {
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    if (canvasGroup == null)
                    {
                        yield break;
                    }

                    elapsedTime += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsedTime / duration));
                    yield return null;
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }

            _fadeRoutines.Remove(canvasGroup);
        }

        private void OnDisable()
        {
            foreach (Coroutine runningRoutine in _fadeRoutines.Values)
            {
                if (runningRoutine != null)
                {
                    StopCoroutine(runningRoutine);
                }
            }

            _fadeRoutines.Clear();
        }
    }
}
