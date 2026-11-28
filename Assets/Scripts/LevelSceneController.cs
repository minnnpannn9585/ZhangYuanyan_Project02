using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Minimal GameScene controller for the current vertical slice.
/// It exposes a temporary return button. The real level-complete condition is
/// still undefined, so that button deliberately does NOT mark level 1 cleared;
/// it only returns to the level-select screen. When the real condition exists,
/// call <see cref="CompleteLevelAndReturn"/> from it.
/// Escape also returns without marking the level cleared.
/// </summary>
public class LevelSceneController : MonoBehaviour
{
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelSelectSceneName = LevelProgress.LevelSelectSceneName;
    [SerializeField] private string albumSceneName = "AlbumScene";
    [SerializeField] private string completeButtonText = "Complete Level";

    private Button completeButton;
    private bool isLeaving;

    private RectTransform folderPanel;
    private Vector2 folderTargetPosition;
    private Vector2 folderHiddenPosition;
    private GameObject folderClickCatcher;
    private bool isFolderShown;
    private bool isFolderSliding;

    private void Start()
    {
        ConfigureAlbumButton();
        ConfigureFolderPanel();
        CreateCompleteButton();
    }

    private void Update()
    {
        if (!isLeaving && Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToLevelSelect(false);
        }
    }

    public void CompleteLevelAndReturn()
    {
        ReturnToLevelSelect(true);
    }

    /// <summary>
    /// Temporary button path: return to the level select without granting a
    /// clear, because level 1's real success condition is not implemented yet.
    /// </summary>
    public void ReturnToLevelSelectWithoutCompleting()
    {
        ReturnToLevelSelect(false);
    }

    public void OpenAlbum()
    {
        if (isLeaving)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(albumSceneName) ||
            !Application.CanStreamedLevelBeLoaded(albumSceneName))
        {
            Debug.LogError(
                $"Scene '{albumSceneName}' is not available in Build Settings. " +
                "Add AlbumScene to the build settings list.");
            return;
        }

        isLeaving = true;
        SceneFade.LoadScene(albumSceneName, 0.6f, 1.2f);
    }

    private void ConfigureAlbumButton()
    {
        Transform album = transform.Find("Image Album");
        if (album == null)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform sceneTransform in transforms)
            {
                if (sceneTransform.name == "Image Album")
                {
                    album = sceneTransform;
                    break;
                }
            }
        }

        if (album == null)
        {
            Debug.LogWarning("GameScene: could not find the 'Image Album' UI object.");
            return;
        }

        Image albumImage = album.GetComponent<Image>();
        if (albumImage == null)
        {
            Debug.LogWarning("GameScene: 'Image Album' has no Image component.");
            return;
        }

        albumImage.raycastTarget = true;

        Button albumButton = album.GetComponent<Button>();
        if (albumButton == null)
        {
            albumButton = album.gameObject.AddComponent<Button>();
        }

        albumButton.targetGraphic = albumImage;
        albumButton.onClick.RemoveAllListeners();
        albumButton.onClick.AddListener(OpenAlbum);

        // The full-screen folder artwork is drawn above the album and would
        // otherwise absorb every click. Keep its visuals, let clicks through.
        DisableRaycastBlocker("Image file folder1");
    }

    private void DisableRaycastBlocker(string objectName)
    {
        Transform blocker = transform.Find(objectName);
        if (blocker == null)
        {
            return;
        }

        Image image = blocker.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private void ConfigureFolderPanel()
    {
        Transform folderTrigger = transform.Find("Image File Foder");
        Transform folder = transform.Find("Image file folder1");

        if (folderTrigger == null || folder == null)
        {
            Debug.LogWarning("GameScene: folder trigger or folder panel was not found.");
            return;
        }

        Image triggerImage = folderTrigger.GetComponent<Image>();
        if (triggerImage != null)
        {
            triggerImage.raycastTarget = true;
        }

        Button triggerButton = folderTrigger.GetComponent<Button>();
        if (triggerButton == null)
        {
            triggerButton = folderTrigger.gameObject.AddComponent<Button>();
        }

        triggerButton.targetGraphic = triggerImage;
        triggerButton.onClick.RemoveAllListeners();
        triggerButton.onClick.AddListener(ShowFolder);

        folderPanel = folder as RectTransform;
        if (folderPanel == null)
        {
            return;
        }

        Image folderImage = folder.GetComponent<Image>();
        if (folderImage != null)
        {
            folderImage.raycastTarget = false;
        }

        // Remember the position the folder was placed at in the scene,
        // then hide it off-screen to the right until the player clicks.
        folderTargetPosition = folderPanel.anchoredPosition;
        float offscreenOffset = Mathf.Max(2000f, Screen.width + 400f);
        folderHiddenPosition = new Vector2(
            folderTargetPosition.x + offscreenOffset,
            folderTargetPosition.y);
        folderPanel.anchoredPosition = folderHiddenPosition;
        folderPanel.gameObject.SetActive(true);

        CreateFolderClickCatcher();
    }

    private void CreateFolderClickCatcher()
    {
        GameObject catcherObject = new GameObject(
            "FolderClickCatcher",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        catcherObject.layer = 5;
        catcherObject.transform.SetParent(transform, false);
        catcherObject.transform.SetSiblingIndex(1);

        RectTransform catcherRect = catcherObject.GetComponent<RectTransform>();
        catcherRect.anchorMin = Vector2.zero;
        catcherRect.anchorMax = Vector2.one;
        catcherRect.offsetMin = Vector2.zero;
        catcherRect.offsetMax = Vector2.zero;

        Image catcherImage = catcherObject.GetComponent<Image>();
        catcherImage.color = new Color(0f, 0f, 0f, 0f);
        catcherImage.raycastTarget = true;

        Button catcherButton = catcherObject.GetComponent<Button>();
        catcherButton.targetGraphic = catcherImage;
        catcherButton.transition = Selectable.Transition.None;
        catcherButton.onClick.RemoveAllListeners();
        catcherButton.onClick.AddListener(HideFolder);

        catcherObject.SetActive(false);
        folderClickCatcher = catcherObject;
    }

    public void ShowFolder()
    {
        if (folderPanel == null || isFolderShown || isFolderSliding)
        {
            return;
        }

        isFolderSliding = true;
        StartCoroutine(SlideFolderRoutine());
    }

    private IEnumerator SlideFolderRoutine()
    {
        Vector2 start = folderPanel.anchoredPosition;
        Vector2 end = folderTargetPosition;
        float duration = 0.6f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            folderPanel.anchoredPosition = Vector2.LerpUnclamped(start, end, EaseOutBack(progress));
            yield return null;
        }

        folderPanel.anchoredPosition = end;
        isFolderSliding = false;
        isFolderShown = true;

        if (folderClickCatcher != null)
        {
            folderClickCatcher.SetActive(true);
        }
    }

    public void HideFolder()
    {
        if (folderPanel == null || !isFolderShown || isFolderSliding)
        {
            return;
        }

        isFolderShown = false;
        isFolderSliding = true;

        if (folderClickCatcher != null)
        {
            folderClickCatcher.SetActive(false);
        }

        StartCoroutine(SlideFolderOutRoutine());
    }

    private IEnumerator SlideFolderOutRoutine()
    {
        Vector2 start = folderPanel.anchoredPosition;
        Vector2 end = folderHiddenPosition;
        float duration = 0.45f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            folderPanel.anchoredPosition = Vector2.LerpUnclamped(start, end, EaseInBack(progress));
            yield return null;
        }

        folderPanel.anchoredPosition = end;
        isFolderSliding = false;
    }

    private static float EaseOutBack(float t)
    {
        // Small overshoot so the folder settles with a subtle pop.
        const float c1 = 0.8f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private static float EaseInBack(float t)
    {
        const float c1 = 0.8f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    private void CreateCompleteButton()
    {
        if (completeButton != null)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
        {
            return;
        }

        Transform existing = canvasRect.Find("CompleteLevelButton");
        GameObject buttonObject;
        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject = new GameObject(
                "CompleteLevelButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasRect, false);
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-36f, -36f);
        buttonRect.sizeDelta = new Vector2(292f, 82f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonObject.AddComponent<Image>();
        }

        buttonImage.color = new Color(0.66f, 0.46f, 0.22f, 0.97f);

        completeButton = buttonObject.GetComponent<Button>();
        if (completeButton == null)
        {
            completeButton = buttonObject.AddComponent<Button>();
        }

        ColorBlock colors = completeButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 1f);
        colors.pressedColor = new Color(0.8f, 0.72f, 0.6f, 1f);
        colors.selectedColor = colors.highlightedColor;
        completeButton.colors = colors;
        completeButton.targetGraphic = buttonImage;
        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(ReturnToLevelSelectWithoutCompleting);

        Transform labelTransform = buttonRect.Find("Text");
        GameObject labelObject;
        if (labelTransform != null)
        {
            labelObject = labelTransform.gameObject;
        }
        else
        {
            labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonRect, false);
        }

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        if (label == null)
        {
            label = labelObject.AddComponent<Text>();
        }

        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.99f, 0.95f, 0.86f, 1f);
        label.text = completeButtonText;
        label.raycastTarget = false;

        buttonObject.transform.SetAsLastSibling();
    }

    private void ReturnToLevelSelect(bool markCompleted)
    {
        if (isLeaving)
        {
            return;
        }

        isLeaving = true;

        if (markCompleted)
        {
            LevelProgress.MarkCompleted(levelNumber);
        }

        if (string.IsNullOrWhiteSpace(levelSelectSceneName) ||
            !Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError(
                $"Scene '{levelSelectSceneName}' is not available in Build Settings. " +
                "Add SampleScene to the build settings list.");
            isLeaving = false;
            return;
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }
}
