using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransitionImage : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;

    private Image transitionImage;
    private Coroutine transitionCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        transitionImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        SetAlpha(0f);
    }

    public void PlayTransition(Action onMiddleAction)
    {
        gameObject.SetActive(true);

        if (rectTransform != null)
        {
            rectTransform.SetAsLastSibling();
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(PlayTransitionCoroutine(onMiddleAction));
    }

    private IEnumerator PlayTransitionCoroutine(Action onMiddleAction)
    {
        yield return Fade(0f, 1f);

        if (onMiddleAction != null)
        {
            onMiddleAction.Invoke();
        }

        yield return Fade(1f, 0f);
        transitionCoroutine = null;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        SetAlpha(startAlpha);

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = fadeDuration <= 0f ? 1f : timer / fadeDuration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (transitionImage == null)
        {
            return;
        }

        Color color = transitionImage.color;
        color.a = alpha;
        transitionImage.color = color;
    }
}
