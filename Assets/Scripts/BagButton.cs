using UnityEngine;
using UnityEngine.UI;

public class BagButton : MonoBehaviour
{
    [SerializeField] private Color unlockedColor = new Color(0.74f, 0.55f, 0.27f, 1f);
    [SerializeField] private Color completedColor = new Color(0.42f, 0.67f, 0.36f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.95f);

    private Button button;
    private Image buttonImage;
    private GameManager gameManager;
    private Text titleText;
    private Text stateText;
    private Font runtimeFont;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureLabels();
    }

    private void Start()
    {
        Bind(FindFirstObjectByType<GameManager>());

        if (button != null)
        {
            button.onClick.RemoveListener(OnBagClicked);
            button.onClick.AddListener(OnBagClicked);
        }
    }

    public void Bind(GameManager manager)
    {
        gameManager = manager;
    }

    public void RefreshView(string bagName, bool isUnlocked, bool isCompleted, int displayIndex, int totalCount)
    {
        EnsureLabels();

        if (titleText != null)
        {
            titleText.text = $"{displayIndex + 1}/{totalCount}  {bagName}";
        }

        if (stateText != null)
        {
            stateText.text = isCompleted ? "Completed" : isUnlocked ? "Unlocked" : "Locked";
        }

        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        if (buttonImage != null)
        {
            buttonImage.color = isCompleted ? completedColor : isUnlocked ? unlockedColor : lockedColor;
        }
    }

    private void OnBagClicked()
    {
        if (gameManager != null)
        {
            gameManager.TryEnterFocusedBag();
        }
    }

    private void EnsureLabels()
    {
        if (titleText == null)
        {
            titleText = CreateLabel("BagTitle", new Vector2(0f, 84f), 34);
        }

        if (stateText == null)
        {
            stateText = CreateLabel("BagState", new Vector2(0f, -88f), 24);
        }
    }

    private Text CreateLabel(string objectName, Vector2 anchoredPosition, int fontSize)
    {
        Transform existingChild = transform.Find(objectName);
        GameObject labelObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject(objectName, typeof(RectTransform));

        labelObject.transform.SetParent(transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = new Vector2(520f, 60f);

        Text label = labelObject.GetComponent<Text>();
        if (label == null)
        {
            label = labelObject.AddComponent<Text>();
        }

        label.font = runtimeFont;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.16f, 0.11f, 0.05f, 1f);
        label.raycastTarget = false;
        return label;
    }
}
