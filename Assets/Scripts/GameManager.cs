using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the SampleScene level-select screen.
/// Only level 1 exists for now; the bag stays grey until the level is cleared.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Level Select")]
    [SerializeField] private GameObject levelSelectCanvas;
    [SerializeField] private BagButton bagButton;
    [SerializeField] private TransitionImage transitionImage;

    [Header("Scenes")]
    [SerializeField] private string level1SceneName = LevelProgress.Level1SceneName;

    private bool isLeavingLevelSelect;

    private void Awake()
    {
        if (levelSelectCanvas == null)
        {
            levelSelectCanvas = FindSceneObject("LevelSelectCanvas");
        }

        if (bagButton == null)
        {
            bagButton = FindFirstObjectByType<BagButton>(FindObjectsInactive.Include);
        }

        if (transitionImage == null)
        {
            transitionImage = FindFirstObjectByType<TransitionImage>(FindObjectsInactive.Include);
        }

        if (bagButton != null)
        {
            bagButton.Bind(this);
        }

        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(true);
        }
    }

    private void Start()
    {
        RefreshLevelSelect();
    }

    public void RefreshLevelSelect()
    {
        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(true);
        }

        if (bagButton != null)
        {
            bagButton.RefreshView(LevelProgress.IsCompleted(bagButton.LevelNumber));
        }
    }

    public void EnterLevel(int levelNumber)
    {
        if (isLeavingLevelSelect)
        {
            return;
        }

        string sceneName = GetSceneName(levelNumber);
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' for level {levelNumber} is not available in Build Settings.");
            return;
        }

        isLeavingLevelSelect = true;

        if (transitionImage != null)
        {
            transitionImage.PlayTransition(() => SceneManager.LoadScene(sceneName));
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneName(int levelNumber)
    {
        if (levelNumber == 1)
        {
            return string.IsNullOrWhiteSpace(level1SceneName)
                ? LevelProgress.Level1SceneName
                : level1SceneName;
        }

        return LevelProgress.GetLevelSceneName(levelNumber);
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
