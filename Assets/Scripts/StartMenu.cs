using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    public const string MenuSceneName = "StartMenu";
    internal const string CutsceneSceneName = "Cutscene";
    internal const string LevelSelectSceneName = "SampleScene";
    private const string DefaultStartSceneName = CutsceneSceneName;
    internal const string SavedSceneKey = "MainMenu.LastScene";
    internal const string MusicVolumeKey = "MainMenu.MusicVolume";
    internal const string SfxVolumeKey = "MainMenu.SfxVolume";

    private const float MenuButtonLeft = 112f;
    private const float MenuButtonWidth = 500f;
    private const float MenuButtonHeight = 104f;
    private const float MenuButtonTopY = 140f;
    private const float MenuButtonSpacing = 140f;

    [SerializeField] private string newGameSceneName = DefaultStartSceneName;

    private Button startButton;
    private Button settingButton;
    private Button loadButton;
    private GameObject settingsPanel;
    private Font builtInFont;

    private void Awake()
    {
        builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CacheButtons();
        DisableBackgroundRaycasts();
        ConfigureMenuButtons();
        CreateSettingsPanel();
        ApplyVolumeSettingsToScene();
    }

    private void Start()
    {
        BindButtons();
        RefreshLoadButton();
        CloseSettings();
    }

    private void CacheButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            switch (button.name)
            {
                case "StartButton":
                    startButton = button;
                    break;
                case "SettingBtn":
                    settingButton = button;
                    break;
                case "LoadBtn":
                    loadButton = button;
                    break;
            }
        }
    }

    /// <summary>
    /// The menu scene is assembled from several full-screen artwork images.
    /// They must not swallow clicks meant for the buttons on the left.
    /// </summary>
    private void DisableBackgroundRaycasts()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.GetComponent<Button>() != null)
            {
                continue;
            }

            image.raycastTarget = false;
        }
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartNewGame);
            startButton.onClick.AddListener(StartNewGame);
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(OpenSettings);
            settingButton.onClick.AddListener(OpenSettings);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(LoadSavedGame);
            loadButton.onClick.AddListener(LoadSavedGame);
        }
    }

    private void ConfigureMenuButtons()
    {
        ConfigureMenuButton(startButton, "Start", 0);
        ConfigureMenuButton(loadButton, "Load", 1);
        ConfigureMenuButton(settingButton, "Settings", 2);
    }

    private void ConfigureMenuButton(Button button, string label, int verticalIndex)
    {
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(
                MenuButtonLeft,
                MenuButtonTopY - verticalIndex * MenuButtonSpacing);
            buttonRect.sizeDelta = new Vector2(MenuButtonWidth, MenuButtonHeight);
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.71f, 0.52f, 0.23f, 0.96f);
            buttonImage.type = Image.Type.Sliced;
            buttonImage.raycastTarget = true;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
        colors.pressedColor = new Color(0.85f, 0.8f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        button.colors = colors;

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText == null)
        {
            labelText = CreateLabel("Text", label, buttonRect, Vector2.zero, 34, FontStyle.Bold);
        }

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        labelText.font = builtInFont;
        labelText.fontSize = 34;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.18f, 0.12f, 0.05f, 1f);
        labelText.text = label;
        labelText.raycastTarget = false;

        // The menu artwork contains full-screen photos; keep the buttons on top
        // so they render above the artwork and receive clicks.
        button.transform.SetAsLastSibling();
    }

    private void StartNewGame()
    {
        LevelProgress.ResetBag1Intro();

        string sceneName = string.IsNullOrWhiteSpace(newGameSceneName)
            ? DefaultStartSceneName
            : newGameSceneName;
        LoadScene(sceneName);
    }

    private void LoadSavedGame()
    {
        string savedSceneName = GetSavedSceneName();
        if (string.IsNullOrEmpty(savedSceneName))
        {
            RefreshLoadButton();
            return;
        }

        LoadScene(savedSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not available in Build Settings.");

            if (sceneName == GetSavedSceneName())
            {
                PlayerPrefs.DeleteKey(SavedSceneKey);
                PlayerPrefs.Save();
                RefreshLoadButton();
            }

            return;
        }

        SaveLastScene(sceneName);
        SceneManager.LoadScene(sceneName);
    }

    private void RefreshLoadButton()
    {
        if (loadButton == null)
        {
            return;
        }

        string savedSceneName = GetSavedSceneName();
        loadButton.interactable = !string.IsNullOrEmpty(savedSceneName) &&
                                  Application.CanStreamedLevelBeLoaded(savedSceneName);
    }

    private void OpenSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void CreateSettingsPanel()
    {
        if (settingsPanel != null)
        {
            return;
        }

        RectTransform parent = transform as RectTransform;
        if (parent == null)
        {
            return;
        }

        settingsPanel = CreateUiObject("SettingsPanel", parent);
        RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsRect.anchorMin = Vector2.zero;
        settingsRect.anchorMax = Vector2.one;
        settingsRect.offsetMin = Vector2.zero;
        settingsRect.offsetMax = Vector2.zero;
        Image panelBackdrop = settingsPanel.AddComponent<Image>();
        panelBackdrop.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject window = CreateUiObject("Window", settingsRect);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(720f, 420f);
        windowRect.anchoredPosition = Vector2.zero;
        Image windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.94f, 0.88f, 0.73f, 0.98f);

        CreateLabel("Title", "Settings", windowRect, new Vector2(0f, 150f), 38, FontStyle.Bold);
        CreateSliderRow(windowRect, "Music Volume", new Vector2(0f, 45f), GetMusicVolume(), OnMusicVolumeChanged);
        CreateSliderRow(windowRect, "SFX Volume", new Vector2(0f, -55f), GetSfxVolume(), OnSfxVolumeChanged);

        Button closeButton = CreateButton(windowRect, "CloseButton", "Close", new Vector2(0f, -145f), new Vector2(220f, 64f));
        closeButton.onClick.AddListener(CloseSettings);
    }

    private Slider CreateSliderRow(RectTransform parent, string label, Vector2 anchoredPosition, float initialValue, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject row = CreateUiObject(label.Replace(" ", string.Empty), parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(560f, 92f);
        rowRect.anchoredPosition = anchoredPosition;

        CreateLabel("Label", label, rowRect, new Vector2(0f, 26f), 26, FontStyle.Bold);

        GameObject sliderObject = CreateUiObject("Slider", rowRect);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(520f, 24f);
        sliderRect.anchoredPosition = new Vector2(0f, -18f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject background = CreateUiObject("Background", sliderRect);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.55f, 0.38f, 0.16f, 0.95f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderRect);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = CreateUiObject("Fill", fillAreaRect);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.97f, 0.78f, 0.35f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderRect);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, -4f);
        handleAreaRect.offsetMax = new Vector2(-10f, 4f);

        GameObject handle = CreateUiObject("Handle", handleAreaRect);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 28f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.35f, 0.2f, 0.08f, 1f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
    }

    private Button CreateButton(RectTransform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.71f, 0.52f, 0.23f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.8f, 0.6f, 0.28f, 1f);
        colors.pressedColor = new Color(0.55f, 0.38f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        button.colors = colors;

        CreateLabel("Text", label, buttonRect, Vector2.zero, 28, FontStyle.Bold);
        return button;
    }

    private Text CreateLabel(string objectName, string content, RectTransform parent, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle)
    {
        GameObject labelObject = CreateUiObject(objectName, parent);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(560f, 44f);
        labelRect.anchoredPosition = anchoredPosition;

        Text text = labelObject.AddComponent<Text>();
        text.font = builtInFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.18f, 0.12f, 0.05f, 1f);
        text.text = content;
        return text;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
        ApplyVolumeSettingsToScene();
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        PlayerPrefs.Save();
        ApplyVolumeSettingsToScene();
    }

    private static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    private static float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    private static string GetSavedSceneName()
    {
        if (!PlayerPrefs.HasKey(SavedSceneKey))
        {
            return string.Empty;
        }

        string savedSceneName = PlayerPrefs.GetString(SavedSceneKey, string.Empty);
        return IsResumableScene(savedSceneName) ? savedSceneName : string.Empty;
    }

    internal static bool IsResumableScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               sceneName != MenuSceneName &&
               sceneName != CutsceneSceneName;
    }

    private static void SaveLastScene(string sceneName)
    {
        if (!IsResumableScene(sceneName))
        {
            return;
        }

        PlayerPrefs.SetString(SavedSceneKey, sceneName);
        PlayerPrefs.Save();
    }

    internal static void ApplyVolumeSettingsToScene()
    {
        AudioListener.volume = 1f;

        AudioSource[] audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource == null)
            {
                continue;
            }

            AudioSourceVolumeCache volumeCache = audioSource.GetComponent<AudioSourceVolumeCache>();
            if (volumeCache == null)
            {
                volumeCache = audioSource.gameObject.AddComponent<AudioSourceVolumeCache>();
            }

            float channelVolume = audioSource.loop ? GetMusicVolume() : GetSfxVolume();
            audioSource.volume = volumeCache.BaseVolume * channelVolume;
        }
    }
}

public sealed class AudioSourceVolumeCache : MonoBehaviour
{
    public float BaseVolume { get; private set; } = 1f;

    private void Awake()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            BaseVolume = audioSource.volume;
        }
    }

    private void OnEnable()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && BaseVolume <= 0f)
        {
            BaseVolume = audioSource.volume;
        }
    }
}

public static class SceneResumeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterCallbacks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (StartMenu.IsResumableScene(scene.name))
        {
            PlayerPrefs.SetString(StartMenu.SavedSceneKey, scene.name);
            PlayerPrefs.Save();
        }

        StartMenu.ApplyVolumeSettingsToScene();
    }
}
