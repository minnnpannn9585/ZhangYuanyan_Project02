using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Minimal GameScene controller for the current vertical slice.
/// It exposes a temporary "Complete Level" button so clearing level 1 can be
/// tested end-to-end. The real level-complete condition can call
/// <see cref="CompleteLevelAndReturn"/> when it is implemented.
/// Escape returns to the level-select screen without marking the level cleared.
/// </summary>
public class LevelSceneController : MonoBehaviour
{
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelSelectSceneName = LevelProgress.LevelSelectSceneName;
    [SerializeField] private string completeButtonText = "Complete Level";

    private Button completeButton;
    private bool isLeaving;

    private void Start()
    {
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
        completeButton.onClick.RemoveListener(CompleteLevelAndReturn);
        completeButton.onClick.AddListener(CompleteLevelAndReturn);

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
