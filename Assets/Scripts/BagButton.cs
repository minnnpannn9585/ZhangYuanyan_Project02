using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The clickable level-1 bag on the level-select screen.
/// While level 1 has not been cleared the green bag art is hidden and the
/// grey "BagButton1-1" overlay is shown. After level 1 is cleared the overlay
/// is hidden again so the original green bag (BagButton1) is revealed.
/// </summary>
public class BagButton : MonoBehaviour
{
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private Image lockedImage;

    private Button button;
    private Image buttonImage;
    private GameManager gameManager;

    public int LevelNumber
    {
        get { return levelNumber; }
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        if (lockedImage == null)
        {
            lockedImage = FindLockedImage();
        }
    }

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnBagClicked);
            button.onClick.AddListener(OnBagClicked);
            button.interactable = true;
        }

        RefreshView(LevelProgress.IsCompleted(levelNumber));
    }

    public void Bind(GameManager manager)
    {
        gameManager = manager;
    }

    /// <summary>
    /// Shows the grey placeholder until the level is completed, then reveals
    /// the original green bag art.
    /// </summary>
    public void RefreshView(bool isCompleted)
    {
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            color.a = isCompleted ? 1f : 0f;
            buttonImage.color = color;
        }

        if (lockedImage != null)
        {
            lockedImage.gameObject.SetActive(!isCompleted);
        }

        if (button != null)
        {
            button.interactable = true;
        }
    }

    private void OnBagClicked()
    {
        if (gameManager != null)
        {
            gameManager.EnterLevel(levelNumber);
            return;
        }

        string sceneName = LevelProgress.GetLevelSceneName(levelNumber);
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private Image FindLockedImage()
    {
        // The grey placeholder lives next to the bag in the level-select canvas,
        // but also accept a child so the hierarchy can be reorganized safely.
        Transform parent = transform.parent;
        if (parent != null)
        {
            Transform sibling = parent.Find("BagButton1-1");
            if (sibling != null)
            {
                return sibling.GetComponent<Image>();
            }
        }

        Transform child = transform.Find("BagButton1-1");
        if (child != null)
        {
            return child.GetComponent<Image>();
        }

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.name == "BagButton1-1")
            {
                return sceneTransform.GetComponent<Image>();
            }
        }

        return null;
    }
}
