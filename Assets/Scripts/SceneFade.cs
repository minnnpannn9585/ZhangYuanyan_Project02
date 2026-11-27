using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen fade that survives a scene load, so a scene can fade out,
/// load, and then fade back in smoothly.
/// </summary>
public static class SceneFade
{
    public static void LoadScene(string sceneName, float fadeOutDuration = 0.8f, float fadeInDuration = 1.2f)
    {
        SceneFadeRunner runner = SceneFadeRunner.GetOrCreate();
        runner.FadeAndLoad(sceneName, fadeOutDuration, fadeInDuration);
    }
}

public class SceneFadeRunner : MonoBehaviour
{
    private static SceneFadeRunner instance;

    private Image overlay;
    private bool isBusy;

    public static SceneFadeRunner GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject fadeObject = new GameObject(
            "SceneFade",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(SceneFadeRunner));

        DontDestroyOnLoad(fadeObject);
        instance = fadeObject.GetComponent<SceneFadeRunner>();
        instance.BuildOverlay();
        return instance;
    }

    private void BuildOverlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject(
            "Black",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        overlay = imageObject.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = true;
    }

    public void FadeAndLoad(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        if (isBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not available in Build Settings.");
            return;
        }

        isBusy = true;
        StartCoroutine(FadeAndLoadRoutine(sceneName, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        overlay.raycastTarget = true;
        yield return Fade(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(sceneName);

        // Let the new scene render one frame while the screen is still black.
        yield return null;
        yield return Fade(1f, 0f, fadeInDuration);

        overlay.raycastTarget = false;
        isBusy = false;

        Destroy(gameObject);
        instance = null;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);
            SetAlpha(Mathf.Lerp(startAlpha, endAlpha, Mathf.SmoothStep(0f, 1f, progress)));
            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (overlay == null)
        {
            return;
        }

        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
