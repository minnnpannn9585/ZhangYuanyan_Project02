using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Serializable]
    private class BagDefinition
    {
        public string bagId = "Bag1";
        public string displayName = "Bag 1";
        public GameObject[] propRoots = Array.Empty<GameObject>();
    }

    [Header("Flow")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private bool closeLevelSelectAfterChoice = true;
    [SerializeField] private GameObject introCutscene;
    [SerializeField] private GameObject levelSelectCanvas;
    [SerializeField] private TransitionImage transitionImage;

    [Header("Level Select")]
    [SerializeField] private BagButton bagButton;
    [SerializeField] private Button previousBagButton;
    [SerializeField] private Button nextBagButton;
    [SerializeField] private Text selectionInfoText;

    [Header("Bags")]
    [SerializeField] private List<BagDefinition> bags = new List<BagDefinition>();

    private bool introFinished;
    private int focusedBagIndex;
    private int completedBagCount;
    private int[] bagOrder = Array.Empty<int>();
    private Font runtimeFont;

    private const string BagOrderKey = "GameFlow.BagOrder";
    private const string CompletedBagCountKey = "GameFlow.CompletedBagCount";
    private const string FocusedBagIndexKey = "GameFlow.FocusedBagIndex";

    private void Awake()
    {
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AutoAssignReferences();
        RegisterDefaultBagsIfNeeded();
        LoadOrCreateBagOrder();
        LoadProgress();
        EnsureNavigationControls();
        BindControls();
        SetActiveBag(string.Empty);
        RefreshBagSelectionUi();

        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(false);
        }

        if (introCutscene != null)
        {
            introCutscene.SetActive(playIntroOnStart);
        }
    }

    private void Start()
    {
        if (!playIntroOnStart || introCutscene == null)
        {
            CompleteIntroCutscene();
        }
    }

    public void CompleteIntroCutscene()
    {
        if (introFinished)
        {
            return;
        }

        introFinished = true;

        if (introCutscene != null)
        {
            introCutscene.SetActive(false);
        }

        OpenLevelSelectCanvas();
    }

    public void SkipIntroCutscene()
    {
        CompleteIntroCutscene();
    }

    public void OpenLevelSelectCanvas()
    {
        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(true);
        }

        RefreshBagSelectionUi();
    }

    public void CloseLevelSelectCanvas()
    {
        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(false);
        }
    }

    public void ShowPreviousBag()
    {
        if (bagOrder.Length == 0 || focusedBagIndex <= 0)
        {
            return;
        }

        focusedBagIndex--;
        SaveProgress();
        RefreshBagSelectionUi();
    }

    public void ShowNextBag()
    {
        if (bagOrder.Length == 0 || focusedBagIndex >= bagOrder.Length - 1)
        {
            return;
        }

        focusedBagIndex++;
        SaveProgress();
        RefreshBagSelectionUi();
    }

    public void TryEnterFocusedBag()
    {
        BagDefinition focusedBag = GetFocusedBag();
        if (focusedBag == null)
        {
            Debug.LogWarning("No bag is currently configured.");
            return;
        }

        if (!IsBagUnlocked(focusedBagIndex))
        {
            Debug.LogWarning($"Bag '{focusedBag.displayName}' is still locked.");
            RefreshBagSelectionUi();
            return;
        }

        SetActiveBag(focusedBag.bagId);

        if (closeLevelSelectAfterChoice)
        {
            HideLevelSelectWithTransition();
        }
    }

    public void CompleteCurrentBag()
    {
        if (!IsBagUnlocked(focusedBagIndex))
        {
            return;
        }

        if (focusedBagIndex == completedBagCount && completedBagCount < bagOrder.Length)
        {
            completedBagCount++;
            SaveProgress();
            RefreshBagSelectionUi();
        }
    }

    private void HideLevelSelectWithTransition()
    {
        if (transitionImage != null)
        {
            transitionImage.PlayTransition(CloseLevelSelectCanvas);
            return;
        }

        CloseLevelSelectCanvas();
    }

    private void AutoAssignReferences()
    {
        if (levelSelectCanvas == null)
        {
            levelSelectCanvas = FindSceneObject("LevelSelectCanvas");
        }

        if (introCutscene == null)
        {
            introCutscene = FindSceneObject("IntroCutscene");
        }

        if (transitionImage == null)
        {
            transitionImage = FindFirstObjectByType<TransitionImage>(FindObjectsInactive.Include);
        }

        if (bagButton == null)
        {
            bagButton = FindFirstObjectByType<BagButton>(FindObjectsInactive.Include);
        }
    }

    private void RegisterDefaultBagsIfNeeded()
    {
        if (bags.Count > 0)
        {
            return;
        }

        GameObject defaultPropsRoot = FindSceneObject("Key");

        for (int index = 0; index < 5; index++)
        {
            BagDefinition bag = new BagDefinition();
            bag.bagId = $"Bag{index + 1}";
            bag.displayName = $"Bag {index + 1}";
            bag.propRoots = index == 0 && defaultPropsRoot != null
                ? new[] { defaultPropsRoot }
                : Array.Empty<GameObject>();
            bags.Add(bag);
        }
    }

    private void LoadOrCreateBagOrder()
    {
        int bagCount = bags.Count;
        if (bagCount == 0)
        {
            bagOrder = Array.Empty<int>();
            return;
        }

        string storedOrder = PlayerPrefs.GetString(BagOrderKey, string.Empty);
        if (TryParseStoredOrder(storedOrder, bagCount, out int[] parsedOrder))
        {
            bagOrder = parsedOrder;
            return;
        }

        bagOrder = CreateBagOrder(bagCount);
        PlayerPrefs.SetString(BagOrderKey, string.Join(",", bagOrder));
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        completedBagCount = Mathf.Clamp(PlayerPrefs.GetInt(CompletedBagCountKey, 0), 0, bagOrder.Length);
        focusedBagIndex = Mathf.Clamp(PlayerPrefs.GetInt(FocusedBagIndexKey, 0), 0, Mathf.Max(0, bagOrder.Length - 1));
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(CompletedBagCountKey, completedBagCount);
        PlayerPrefs.SetInt(FocusedBagIndexKey, focusedBagIndex);
        PlayerPrefs.Save();
    }

    private void EnsureNavigationControls()
    {
        if (levelSelectCanvas == null)
        {
            return;
        }

        RectTransform levelSelectRect = levelSelectCanvas.transform as RectTransform;
        if (levelSelectRect == null)
        {
            return;
        }

        if (selectionInfoText == null)
        {
            selectionInfoText = CreateCanvasLabel(levelSelectRect, "BagSelectionInfo", new Vector2(0f, 265f), new Vector2(820f, 72f), 30);
        }

        if (previousBagButton == null)
        {
            previousBagButton = CreateNavigationButton(levelSelectRect, "PreviousBagButton", "<", new Vector2(-420f, -90f));
        }

        if (nextBagButton == null)
        {
            nextBagButton = CreateNavigationButton(levelSelectRect, "NextBagButton", ">", new Vector2(420f, -90f));
        }
    }

    private void BindControls()
    {
        if (bagButton != null)
        {
            bagButton.Bind(this);
        }

        if (previousBagButton != null)
        {
            previousBagButton.onClick.RemoveListener(ShowPreviousBag);
            previousBagButton.onClick.AddListener(ShowPreviousBag);
        }

        if (nextBagButton != null)
        {
            nextBagButton.onClick.RemoveListener(ShowNextBag);
            nextBagButton.onClick.AddListener(ShowNextBag);
        }
    }

    private void RefreshBagSelectionUi()
    {
        BagDefinition focusedBag = GetFocusedBag();
        if (focusedBag == null)
        {
            return;
        }

        bool isUnlocked = IsBagUnlocked(focusedBagIndex);
        bool isCompleted = focusedBagIndex < completedBagCount;

        if (bagButton != null)
        {
            bagButton.RefreshView(focusedBag.displayName, isUnlocked, isCompleted, focusedBagIndex, bagOrder.Length);
        }

        if (selectionInfoText != null)
        {
            selectionInfoText.text = isUnlocked
                ? $"Choose a bag. Clear {focusedBag.displayName} to unlock the next one."
                : $"This bag is locked. Complete the previous bag first.";
        }

        if (previousBagButton != null)
        {
            previousBagButton.interactable = focusedBagIndex > 0;
        }

        if (nextBagButton != null)
        {
            nextBagButton.interactable = focusedBagIndex < bagOrder.Length - 1;
        }
    }

    private void SetActiveBag(string bagId)
    {
        foreach (BagDefinition bag in bags)
        {
            if (bag == null || bag.propRoots == null)
            {
                continue;
            }

            bool isSelectedBag = bag.bagId == bagId;
            foreach (GameObject propRoot in bag.propRoots)
            {
                if (propRoot != null)
                {
                    propRoot.SetActive(isSelectedBag);
                }
            }
        }
    }

    private BagDefinition GetFocusedBag()
    {
        if (bagOrder.Length == 0 || focusedBagIndex < 0 || focusedBagIndex >= bagOrder.Length)
        {
            return null;
        }

        int bagListIndex = bagOrder[focusedBagIndex];
        if (bagListIndex < 0 || bagListIndex >= bags.Count)
        {
            return null;
        }

        return bags[bagListIndex];
    }

    private bool IsBagUnlocked(int displayIndex)
    {
        return displayIndex <= completedBagCount;
    }

    private static bool TryParseStoredOrder(string storedOrder, int bagCount, out int[] parsedOrder)
    {
        parsedOrder = Array.Empty<int>();
        if (string.IsNullOrEmpty(storedOrder))
        {
            return false;
        }

        string[] tokens = storedOrder.Split(',');
        if (tokens.Length != bagCount)
        {
            return false;
        }

        int[] order = new int[bagCount];
        bool[] seen = new bool[bagCount];

        for (int index = 0; index < tokens.Length; index++)
        {
            if (!int.TryParse(tokens[index], out int parsedIndex) ||
                parsedIndex < 0 ||
                parsedIndex >= bagCount ||
                seen[parsedIndex])
            {
                return false;
            }

            order[index] = parsedIndex;
            seen[parsedIndex] = true;
        }

        if (bagCount > 1 && order[bagCount - 1] != bagCount - 1)
        {
            return false;
        }

        parsedOrder = order;
        return true;
    }

    private static int[] CreateBagOrder(int bagCount)
    {
        int[] order = new int[bagCount];
        for (int index = 0; index < bagCount; index++)
        {
            order[index] = index;
        }

        int shuffleLimit = Mathf.Max(0, bagCount - 1);
        for (int index = 0; index < shuffleLimit; index++)
        {
            int swapIndex = UnityEngine.Random.Range(index, shuffleLimit);
            int temp = order[index];
            order[index] = order[swapIndex];
            order[swapIndex] = temp;
        }

        if (bagCount > 0)
        {
            order[bagCount - 1] = bagCount - 1;
        }

        return order;
    }

    private Button CreateNavigationButton(RectTransform parent, string objectName, string label, Vector2 anchoredPosition)
    {
        Transform existingTransform = parent.Find(objectName);
        GameObject buttonObject = existingTransform != null
            ? existingTransform.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));

        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(120f, 120f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.73f, 0.54f, 0.24f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.84f, 1f);
        colors.pressedColor = new Color(0.84f, 0.8f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text buttonText = CreateCanvasLabel(buttonRect, "Text", Vector2.zero, new Vector2(120f, 120f), 44);
        buttonText.text = label;
        return button;
    }

    private Text CreateCanvasLabel(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        Transform existingTransform = parent.Find(objectName);
        GameObject labelObject = existingTransform != null
            ? existingTransform.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(Text));

        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        Text label = labelObject.GetComponent<Text>();
        label.font = runtimeFont;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.16f, 0.11f, 0.05f, 1f);
        label.raycastTarget = false;
        return label;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.name == objectName)
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }
}
