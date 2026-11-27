using UnityEngine;

/// <summary>
/// Small, centralized store for level completion state.
/// Progress is written to PlayerPrefs so it survives scene loads and restarts.
/// </summary>
public static class LevelProgress
{
    public const string LevelSelectSceneName = "SampleScene";
    public const string Level1SceneName = "GameScene";

    private const string CompletedKeyPrefix = "Progress.Level";
    private const string CompletedKeySuffix = ".Completed";
    private const int HighestSupportedLevel = 50;

    public static bool IsCompleted(int levelNumber)
    {
        if (levelNumber <= 0)
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetCompletedKey(levelNumber), 0) == 1;
    }

    public static bool IsLevel1Completed
    {
        get { return IsCompleted(1); }
    }

    public static void MarkCompleted(int levelNumber)
    {
        if (levelNumber <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(GetCompletedKey(levelNumber), 1);
        PlayerPrefs.Save();
    }

    public static void MarkLevel1Completed()
    {
        MarkCompleted(1);
    }

    public static void ResetLevel(int levelNumber)
    {
        if (levelNumber <= 0)
        {
            return;
        }

        PlayerPrefs.DeleteKey(GetCompletedKey(levelNumber));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        for (int levelNumber = 1; levelNumber <= HighestSupportedLevel; levelNumber++)
        {
            PlayerPrefs.DeleteKey(GetCompletedKey(levelNumber));
        }

        PlayerPrefs.Save();
    }

    public static string GetLevelSceneName(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                return Level1SceneName;
            default:
                return string.Empty;
        }
    }

    private static string GetCompletedKey(int levelNumber)
    {
        return CompletedKeyPrefix + levelNumber + CompletedKeySuffix;
    }
}
