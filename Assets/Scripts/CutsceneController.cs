using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneController : MonoBehaviour
{
    [Serializable]
    public class CutsceneStep
    {
        [TextArea(2, 6)]
        public string dialogue;

        [Tooltip("Fade these images in. Images already visible stay on screen.")]
        public Image[] imagesToFadeIn = Array.Empty<Image>();

        [Tooltip("Fade these images out. Leave empty to keep previous images.")]
        public Image[] imagesToFadeOut = Array.Empty<Image>();
    }

    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private string nextSceneName = LevelProgress.LevelSelectSceneName;
    [SerializeField] private CutsceneStep[] steps = Array.Empty<CutsceneStep>();

    private readonly Dictionary<Image, float> imageTargetAlphas = new Dictionary<Image, float>();
    private int currentStepIndex = -1;
    private bool isTransitioning;
    private bool hasFinished;
    private Text continueHint;

    private void Awake()
    {
        AutoAssignReferences();
        CacheImageAlphas();
        HideCutsceneVisuals();
        CreateContinueHint();
    }

    private void Start()
    {
        if (steps.Length == 0)
        {
            Debug.LogWarning("CutsceneController has no steps configured.");
            return;
        }

        StartCoroutine(PlayStep(0));
    }

    private void Update()
    {
        if (isTransitioning || steps.Length == 0)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) &&
            !Input.GetKeyDown(KeyCode.Space) &&
            !Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }

        if (currentStepIndex >= steps.Length - 1)
        {
            FinishCutscene();
            return;
        }

        StartCoroutine(PlayStep(currentStepIndex + 1));
    }

    private IEnumerator PlayStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Length)
        {
            yield break;
        }

        isTransitioning = true;
        currentStepIndex = stepIndex;
        UpdateContinueHint();
        CutsceneStep step = steps[stepIndex];

        if (dialogueText != null && dialogueText.color.a > 0.01f)
        {
            yield return FadeDialogue(dialogueText.color.a, 0f);
        }

        if (dialogueText != null)
        {
            dialogueText.text = step.dialogue ?? string.Empty;
        }

        List<IEnumerator> visualFades = new List<IEnumerator>();
        AppendImageFades(visualFades, step.imagesToFadeOut, 0f);
        AppendImageFades(visualFades, step.imagesToFadeIn, -1f);

        if (dialogueText != null && !string.IsNullOrWhiteSpace(step.dialogue))
        {
            visualFades.Add(FadeDialogue(0f, 1f));
        }

        yield return RunParallel(visualFades);
        isTransitioning = false;
    }

    private void FinishCutscene()
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;

        string targetScene = string.IsNullOrWhiteSpace(nextSceneName)
            ? LevelProgress.LevelSelectSceneName
            : nextSceneName;

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError($"Scene '{targetScene}' is not available in Build Settings.");
            return;
        }

        SceneFade.LoadScene(targetScene);
    }

    private void CreateContinueHint()
    {
        Transform existing = transform.Find("ContinueHint");
        GameObject hintObject = existing != null
            ? existing.gameObject
            : new GameObject("ContinueHint", typeof(RectTransform), typeof(Text));

        hintObject.transform.SetParent(transform, false);

        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-36f, -28f);
        hintRect.sizeDelta = new Vector2(420f, 44f);

        continueHint = hintObject.GetComponent<Text>();
        if (continueHint == null)
        {
            continueHint = hintObject.AddComponent<Text>();
        }

        continueHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        continueHint.fontSize = 24;
        continueHint.fontStyle = FontStyle.Italic;
        continueHint.alignment = TextAnchor.MiddleRight;
        continueHint.color = new Color(0.96f, 0.92f, 0.84f, 0.72f);
        continueHint.raycastTarget = false;
        continueHint.text = "Click to continue";

        hintObject.transform.SetAsLastSibling();
    }

    private void UpdateContinueHint()
    {
        if (continueHint == null)
        {
            return;
        }

        continueHint.text = currentStepIndex >= steps.Length - 1
            ? "Click to finish"
            : "Click to continue";
    }

    private void AutoAssignReferences()
    {
        if (dialogueText == null)
        {
            dialogueText = GetComponentInChildren<TMP_Text>(true);
        }

        if (steps.Length > 0)
        {
            return;
        }

        Image image01 = FindChildImage("image01");
        Image image02 = FindChildImage("image02");
        Image image03 = FindChildImage("image03");
        string firstLine = dialogueText != null ? dialogueText.text : string.Empty;

        steps = new[]
        {
            new CutsceneStep
            {
                dialogue = firstLine,
                imagesToFadeIn = CreateImageArray(image01)
            },
            new CutsceneStep
            {
                dialogue = firstLine,
                imagesToFadeIn = CreateImageArray(image02)
            },
            new CutsceneStep
            {
                dialogue = firstLine,
                imagesToFadeIn = CreateImageArray(image03),
                imagesToFadeOut = CreateImageArray(image01, image02)
            }
        };
    }

    private void CacheImageAlphas()
    {
        foreach (CutsceneStep step in steps)
        {
            CacheImages(step.imagesToFadeIn);
            CacheImages(step.imagesToFadeOut);
        }
    }

    private void CacheImages(Image[] images)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            if (image == null || imageTargetAlphas.ContainsKey(image))
            {
                continue;
            }

            float targetAlpha = image.color.a;
            imageTargetAlphas[image] = targetAlpha > 0.01f ? targetAlpha : 1f;
        }
    }

    private void HideCutsceneVisuals()
    {
        foreach (KeyValuePair<Image, float> pair in imageTargetAlphas)
        {
            SetImageAlpha(pair.Key, 0f);
            pair.Key.gameObject.SetActive(false);
        }

        SetDialogueAlpha(0f);
    }

    private void AppendImageFades(List<IEnumerator> fades, Image[] images, float targetAlpha)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            float endAlpha = targetAlpha < 0f ? GetTargetAlpha(image) : targetAlpha;
            float startAlpha = image.gameObject.activeSelf ? image.color.a : 0f;

            if (endAlpha > 0.01f)
            {
                image.gameObject.SetActive(true);
            }

            if (Mathf.Abs(startAlpha - endAlpha) < 0.01f)
            {
                SetImageAlpha(image, endAlpha);
                continue;
            }

            fades.Add(FadeImage(image, startAlpha, endAlpha));
        }
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha)
    {
        SetImageAlpha(image, startAlpha);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(timer / fadeDuration);
            SetImageAlpha(image, Mathf.Lerp(startAlpha, endAlpha, progress));
            yield return null;
        }

        SetImageAlpha(image, endAlpha);

        if (endAlpha <= 0.01f)
        {
            image.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeDialogue(float startAlpha, float endAlpha)
    {
        SetDialogueAlpha(startAlpha);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(timer / fadeDuration);
            SetDialogueAlpha(Mathf.Lerp(startAlpha, endAlpha, progress));
            yield return null;
        }

        SetDialogueAlpha(endAlpha);
    }

    private IEnumerator RunParallel(List<IEnumerator> routines)
    {
        if (routines.Count == 0)
        {
            yield break;
        }

        int remaining = 0;
        foreach (IEnumerator routine in routines)
        {
            remaining++;
            StartCoroutine(RunTracked(routine, () => remaining--));
        }

        while (remaining > 0)
        {
            yield return null;
        }
    }

    private static IEnumerator RunTracked(IEnumerator routine, Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }

    private float GetTargetAlpha(Image image)
    {
        if (image != null && imageTargetAlphas.TryGetValue(image, out float targetAlpha))
        {
            return targetAlpha;
        }

        return 1f;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetDialogueAlpha(float alpha)
    {
        if (dialogueText == null)
        {
            return;
        }

        Color color = dialogueText.color;
        color.a = alpha;
        dialogueText.color = color;
    }

    private Image FindChildImage(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return child.GetComponent<Image>();
            }
        }

        return null;
    }

    private static Image[] CreateImageArray(params Image[] images)
    {
        List<Image> validImages = new List<Image>();
        foreach (Image image in images)
        {
            if (image != null)
            {
                validImages.Add(image);
            }
        }

        return validImages.ToArray();
    }
}
