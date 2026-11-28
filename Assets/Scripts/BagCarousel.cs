using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level-select bag carousel. It auto-discovers the bag objects in the scene
/// (BagButton1 / BagButton1-1, BagBotton2 / BagBotton2-1, ...), animates the
/// left/right arrows, keeps one shadow under every bag, and locks level N
/// until level N-1 has been cleared.
/// </summary>
public class BagCarousel : MonoBehaviour
{
    private const float MoveDuration = 0.55f;
    private const float Bag1CenterScale = 1f;
    private const float Bag1SideScale = 0.5f;
    private const float FullCanvasCenterScale = 1.6f;
    private const float FullCanvasSideScale = 1f;
    private const int MaxBagCount = 5;
    private const float CenterEnterOffset = 700f;

    // The whole carousel is lifted a little and the side bags are kept close
    // enough to stay readable, but far enough not to look crowded.
    private const float BagVerticalOffset = 68f;
    private const float SideVerticalOffset = 30f;
    private const float SideDistanceFactor = 1f;

    // Level 1's real clear condition is not implemented yet. Until it is,
    // every bag after Bag 1 must stay on its grey "-1" visual and stay locked,
    // even if an old test run left a completion key behind.
    private const bool NextBagUnlockEnabled = false;

    // Bag 2+ sprites are full 16:9 images and the bag artwork sits slightly
    // below the image centre. Lift the RectTransform by this much (per unit
    // scale) so the visible bag itself sits on the slot instead of sinking.
    private const float FullCanvasBagVisualOffsetY = 70f;

    private class BagEntry
    {
        public int Level;
        public GameObject Colored;
        public GameObject Locked;
        public RectTransform ColoredRect;
        public RectTransform LockedRect;
        public Image ColoredImage;
        public Image LockedImage;
        public GameObject Shadow;
        public RectTransform ShadowRect;
        public float CenterScale;
        public float SideScale;
        public float VisualOffsetY;
        public bool IsUnlocked;
        public bool IsColored;
    }

    private readonly List<BagEntry> entries = new List<BagEntry>();

    private GameManager gameManager;
    private Transform sourceShadow;
    private Vector2 centerPosition;
    private Vector2 leftPosition;
    private Vector2 rightPosition;
    private Vector2 shadowCenterOffset;
    private int centerIndex;
    private bool isBuilt;
    private bool isAnimating;
    private bool isBag1IntroPlaying;

    private void Awake()
    {
        Build();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        if (!isBuilt)
        {
            Build();
        }

        Refresh();
    }

    private void Build()
    {
        if (isBuilt)
        {
            return;
        }

        isBuilt = true;

        sourceShadow = FindTransform("Shadow");
        if (sourceShadow != null)
        {
            Image shadowImage = sourceShadow.GetComponent<Image>();
            if (shadowImage != null)
            {
                shadowImage.raycastTarget = false;
            }
        }

        SetupArrow("LeftArrow", -1);
        SetupArrow("RightArrow", 1);

        for (int level = 1; level <= MaxBagCount; level++)
        {
            GameObject colored =
                FindAny("BagButton" + level) ??
                FindAny("BagBotton" + level) ??
                FindAny("BackButton" + level);

            GameObject locked =
                FindAny("BagButton" + level + "-1") ??
                FindAny("BagBotton" + level + "-1") ??
                FindAny("BackButton" + level + "-1");

            if (colored == null && locked == null)
            {
                continue;
            }

            if (colored == null)
            {
                colored = locked;
            }

            BagEntry entry = CreateEntry(level, colored, locked);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning("BagCarousel: no BagButton objects found in the level-select scene.");
            return;
        }

        Vector2 bag1Original = entries[0].ColoredRect.anchoredPosition;
        centerPosition = bag1Original + new Vector2(0f, BagVerticalOffset);

        Vector2 sideOffset = entries.Count > 1
            ? entries[1].ColoredRect.anchoredPosition - bag1Original
            : new Vector2(544f, 102f);

        rightPosition = centerPosition + new Vector2(
            sideOffset.x * SideDistanceFactor,
            SideVerticalOffset);
        leftPosition = new Vector2(
            centerPosition.x - (rightPosition.x - centerPosition.x),
            rightPosition.y);

        if (sourceShadow != null &&
            sourceShadow.TryGetComponent(out RectTransform shadowRect))
        {
            shadowCenterOffset = shadowRect.anchoredPosition - bag1Original;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            BagEntry entry = entries[i];
            entry.CenterScale = entry.Level == 1 ? Bag1CenterScale : FullCanvasCenterScale;
            entry.SideScale = entry.Level == 1 ? Bag1SideScale : FullCanvasSideScale;
            entry.VisualOffsetY = entry.Level == 1 ? 0f : FullCanvasBagVisualOffsetY;
            CreateShadow(entry, i == 0);
        }

        centerIndex = 0;
        Refresh();
    }

    private BagEntry CreateEntry(int level, GameObject colored, GameObject locked)
    {
        if (colored == null)
        {
            return null;
        }

        BagEntry entry = new BagEntry
        {
            Level = level,
            Colored = colored,
            Locked = locked
        };

        entry.ColoredRect = colored.GetComponent<RectTransform>();
        entry.ColoredImage = colored.GetComponent<Image>();

        if (entry.ColoredRect == null)
        {
            return null;
        }

        if (locked != null)
        {
            entry.LockedRect = locked.GetComponent<RectTransform>();
            entry.LockedImage = locked.GetComponent<Image>();
        }

        // The old single-bag script is replaced by the carousel.
        BagButton legacyBagButton = colored.GetComponent<BagButton>();
        if (legacyBagButton != null)
        {
            legacyBagButton.enabled = false;
        }

        if (entry.ColoredImage != null)
        {
            entry.ColoredImage.raycastTarget = true;
            SetupAlphaHitTest(entry.ColoredImage);
        }

        Button coloredButton = colored.GetComponent<Button>();
        if (coloredButton == null)
        {
            coloredButton = colored.AddComponent<Button>();
        }

        coloredButton.targetGraphic = entry.ColoredImage;
        coloredButton.onClick.RemoveAllListeners();
        coloredButton.onClick.AddListener(() => OnBagClicked(entry));

        if (locked != null && entry.LockedImage != null)
        {
            entry.LockedImage.raycastTarget = true;
            SetupAlphaHitTest(entry.LockedImage);

            Button lockedButton = locked.GetComponent<Button>();
            if (lockedButton == null)
            {
                lockedButton = locked.AddComponent<Button>();
            }

            lockedButton.targetGraphic = entry.LockedImage;
            lockedButton.onClick.RemoveAllListeners();
            lockedButton.onClick.AddListener(() => OnBagClicked(entry));
        }

        AddGlow(entry.Colored, level, false);
        AddGlow(locked, level, true);

        return entry;
    }

    private static void AddGlow(GameObject target, int level, bool locked)
    {
        if (target == null)
        {
            return;
        }

        string resourceName;
        Color color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        Vector2 sizeMultiplier = Vector2.one;

        if (level == 1)
        {
            if (!locked)
            {
                resourceName = "BagGlow/Bag1Glow";
                sizeMultiplier = new Vector2(760f / 560f, 891f / 691f);
            }
            else
            {
                resourceName = "BagGlow/Bag1LockedGlow";
            }
        }
        else if (level == 2)
        {
            resourceName = "BagGlow/Bag2Glow";
        }
        else
        {
            // Glow assets for later bags can be added when those bags are made.
            return;
        }

        Sprite glowSprite = Resources.Load<Sprite>(resourceName);
        if (glowSprite == null)
        {
            Debug.LogWarning(
                "BagCarousel: missing glow sprite '" + resourceName +
                "' under Assets/Resources.");
            return;
        }

        BagHoverGlow glow = target.GetComponent<BagHoverGlow>();
        if (glow == null)
        {
            glow = target.AddComponent<BagHoverGlow>();
        }

        glow.Initialize(glowSprite, color, sizeMultiplier);
    }

    private void SetupAlphaHitTest(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return;
        }

        Texture2D texture = image.sprite.texture;
        if (texture != null && texture.isReadable)
        {
            image.alphaHitTestMinimumThreshold = 0.15f;
            return;
        }

        Debug.LogWarning(
            "BagCarousel: sprite '" + image.sprite.name +
            "' is not Read/Write enabled. Enable Read/Write on the texture " +
            "so the transparent parts of the full-screen image do not block clicks.",
            image);
    }

    private void SetupArrow(string objectName, int direction)
    {
        GameObject arrow = FindAny(objectName);
        if (arrow == null)
        {
            return;
        }

        Image image = arrow.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.raycastTarget = true;
        SetupAlphaHitTest(image);

        Button button = arrow.GetComponent<Button>();
        if (button == null)
        {
            button = arrow.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        if (direction < 0)
        {
            button.onClick.AddListener(MoveLeft);
        }
        else
        {
            button.onClick.AddListener(MoveRight);
        }
    }

    private void CreateShadow(BagEntry entry, bool useSourceShadow)
    {
        if (sourceShadow == null)
        {
            return;
        }

        entry.Shadow = useSourceShadow
            ? sourceShadow.gameObject
            : Instantiate(sourceShadow.gameObject, transform);

        if (!useSourceShadow)
        {
            entry.Shadow.name = "Shadow_Level" + entry.Level;
        }

        entry.ShadowRect = entry.Shadow.GetComponent<RectTransform>();
        Image shadowImage = entry.Shadow.GetComponent<Image>();
        if (shadowImage != null)
        {
            shadowImage.raycastTarget = false;
        }
    }

    public void Refresh()
    {
        if (!isBuilt)
        {
            return;
        }

        foreach (BagEntry entry in entries)
        {
            bool previousCleared =
                NextBagUnlockEnabled && LevelProgress.IsCompleted(entry.Level - 1);
            bool unlocked = entry.Level == 1 || previousCleared;

            // Bag 1 gets a one-time grey -> green intro. Every later bag flips
            // to its coloured version only when the previous level's real clear
            // condition has been implemented and satisfied.
            bool colored = entry.Level == 1
                ? LevelProgress.IsBag1IntroPlayed
                : previousCleared;

            entry.IsUnlocked = unlocked;
            entry.IsColored = colored;
            ApplyRaycastState(entry);

            if (entry.ColoredImage != null)
            {
                Color color = entry.ColoredImage.color;
                color.a = colored ? 1f : 0f;
                entry.ColoredImage.color = color;
            }

            if (entry.Locked != null)
            {
                entry.Locked.SetActive(!colored);
            }
        }

        ApplyLayout(centerIndex, true);
    }

    public void BeginIntroIfNeeded()
    {
        if (!isBuilt || isBag1IntroPlaying || LevelProgress.IsBag1IntroPlayed)
        {
            return;
        }

        if (entries.Count == 0)
        {
            return;
        }

        BagEntry entry = entries[0];
        if (entry.Colored == null || entry.ColoredImage == null || entry.Locked == null)
        {
            LevelProgress.MarkBag1IntroPlayed();
            return;
        }

        // Clear the stale test completion so Bag2 stays grey while Bag1 plays
        // its intro. Once the intro has finished, normal progress resumes.
        LevelProgress.ResetLevel(1);
        Refresh();

        entry.IsUnlocked = true;
        entry.IsColored = false;
        entry.Colored.SetActive(true);
        entry.Locked.SetActive(true);
        SetAlpha(entry.ColoredImage, 0f);
        if (entry.LockedImage != null)
        {
            SetAlpha(entry.LockedImage, 1f);
        }

        isBag1IntroPlaying = true;
        StartCoroutine(PlayBag1Intro(entry));
    }

    private IEnumerator PlayBag1Intro(BagEntry entry)
    {
        // The cutscene -> SampleScene fade-in is about 1.2s. Wait for it so the
        // grey -> green intro is actually visible to the player.
        yield return new WaitForSecondsRealtime(1.2f);

        const float duration = 0.7f;
        float timer = 0f;
        float baseScale = entry.CenterScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            SetAlpha(entry.ColoredImage, eased);
            if (entry.LockedImage != null)
            {
                SetAlpha(entry.LockedImage, 1f - eased);
            }

            float pop = 1f + Mathf.Sin(progress * Mathf.PI) * 0.06f;
            if (entry.ColoredRect != null)
            {
                entry.ColoredRect.localScale = Vector3.one * (baseScale * pop);
            }
            if (entry.LockedRect != null)
            {
                entry.LockedRect.localScale = Vector3.one * (baseScale * pop);
            }

            yield return null;
        }

        SetAlpha(entry.ColoredImage, 1f);
        if (entry.LockedImage != null)
        {
            SetAlpha(entry.LockedImage, 0f);
        }
        if (entry.ColoredRect != null)
        {
            entry.ColoredRect.localScale = Vector3.one * baseScale;
        }
        if (entry.LockedRect != null)
        {
            entry.LockedRect.localScale = Vector3.one * baseScale;
        }
        if (entry.Locked != null)
        {
            entry.Locked.SetActive(false);
        }

        entry.IsColored = true;
        LevelProgress.MarkBag1IntroPlayed();
        isBag1IntroPlaying = false;
        ApplyLayout(centerIndex, true);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public void MoveLeft()
    {
        Move(-1);
    }

    public void MoveRight()
    {
        Move(1);
    }

    private void Move(int direction)
    {
        if (isAnimating || isBag1IntroPlaying || entries.Count == 0)
        {
            return;
        }

        int next = Mathf.Clamp(centerIndex + direction, 0, entries.Count - 1);
        if (next == centerIndex)
        {
            return;
        }

        StartCoroutine(AnimateTo(next));
    }

    private void OnBagClicked(BagEntry entry)
    {
        if (isAnimating || isBag1IntroPlaying || entry == null)
        {
            return;
        }

        int index = entries.IndexOf(entry);
        if (index < 0)
        {
            return;
        }

        if (index != centerIndex)
        {
            StartCoroutine(AnimateTo(index));
            return;
        }

        if (!entry.IsUnlocked)
        {
            return;
        }

        string sceneName = LevelProgress.GetLevelSceneName(entry.Level);
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.Log("Level " + entry.Level + " is unlocked, but its gameplay scene is not built yet.");
            return;
        }

        if (gameManager != null)
        {
            gameManager.EnterLevel(entry.Level);
        }
    }

    private void ApplyLayout(int newCenterIndex, bool instant)
    {
        centerIndex = newCenterIndex;

        for (int i = 0; i < entries.Count; i++)
        {
            BagEntry entry = entries[i];
            int offset = i - centerIndex;
            bool visible = offset >= -1 && offset <= 1;

            if (visible)
            {
                entry.Colored.SetActive(true);
                if (entry.Locked != null)
                {
                    entry.Locked.SetActive(!entry.IsColored);
                }
                ApplyRaycastState(entry);

                SetEntryTransform(entry, GetSlotPosition(offset), GetSlotScale(entry, offset));
            }
            else if (instant)
            {
                entry.Colored.SetActive(false);
                if (entry.Locked != null)
                {
                    entry.Locked.SetActive(false);
                }
                if (entry.Shadow != null)
                {
                    entry.Shadow.SetActive(false);
                }
            }
        }
    }

    private static void ApplyRaycastState(BagEntry entry)
    {
        // Only the currently visible version should receive clicks/hover.
        // This also lets the matching BagHoverGlow know which one is active.
        if (entry.ColoredImage != null)
        {
            entry.ColoredImage.raycastTarget = entry.IsColored;
        }
        if (entry.LockedImage != null)
        {
            entry.LockedImage.raycastTarget = !entry.IsColored;
        }
    }

    private IEnumerator AnimateTo(int newCenterIndex)
    {
        isAnimating = true;

        int oldCenterIndex = centerIndex;
        int count = entries.Count;

        Vector2[] startPositions = new Vector2[count];
        Vector2[] targetPositions = new Vector2[count];
        float[] startScales = new float[count];
        float[] targetScales = new float[count];
        bool[] animate = new bool[count];

        for (int i = 0; i < count; i++)
        {
            BagEntry entry = entries[i];
            int oldOffset = i - oldCenterIndex;
            int newOffset = i - newCenterIndex;
            bool wasVisible = oldOffset >= -1 && oldOffset <= 1;
            bool willVisible = newOffset >= -1 && newOffset <= 1;

            if (!wasVisible && !willVisible)
            {
                animate[i] = false;
                continue;
            }

            animate[i] = true;

            entry.Colored.SetActive(true);
            if (entry.Locked != null)
            {
                entry.Locked.SetActive(!entry.IsColored);
            }
            ApplyRaycastState(entry);
            if (entry.Shadow != null)
            {
                entry.Shadow.SetActive(true);
            }

            Vector2 start = entry.ColoredRect.anchoredPosition;
            float startScale = entry.ColoredRect.localScale.x;

            if (!wasVisible && willVisible)
            {
                start = newOffset > 0
                    ? new Vector2(rightPosition.x + CenterEnterOffset, rightPosition.y)
                    : new Vector2(leftPosition.x - CenterEnterOffset, leftPosition.y);
                startScale = entry.SideScale;
            }
            else if (wasVisible)
            {
                // The rect position already includes the visual compensation.
                // Strip it so the animation interpolates slot positions, then
                // SetEntryTransform adds the compensation back every frame.
                start -= new Vector2(0f, entry.VisualOffsetY * startScale);
            }

            Vector2 target;
            float targetScale;

            if (willVisible)
            {
                target = GetSlotPosition(newOffset);
                targetScale = GetSlotScale(entry, newOffset);
            }
            else
            {
                target = newOffset < 0
                    ? new Vector2(leftPosition.x - CenterEnterOffset, leftPosition.y)
                    : new Vector2(rightPosition.x + CenterEnterOffset, rightPosition.y);
                targetScale = entry.SideScale * 0.8f;
            }

            startPositions[i] = start;
            targetPositions[i] = target;
            startScales[i] = startScale;
            targetScales[i] = targetScale;
        }

        float timer = 0f;
        while (timer < MoveDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / MoveDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < count; i++)
            {
                if (!animate[i])
                {
                    continue;
                }

                BagEntry entry = entries[i];
                Vector2 position = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], eased);
                float scale = Mathf.LerpUnclamped(startScales[i], targetScales[i], eased);
                SetEntryTransform(entry, position, scale);
            }

            yield return null;
        }

        centerIndex = newCenterIndex;

        for (int i = 0; i < count; i++)
        {
            BagEntry entry = entries[i];
            int offset = i - centerIndex;
            bool visible = offset >= -1 && offset <= 1;

            if (visible)
            {
                entry.Colored.SetActive(true);
                if (entry.Locked != null)
                {
                    entry.Locked.SetActive(!entry.IsColored);
                }
                ApplyRaycastState(entry);
                SetEntryTransform(entry, GetSlotPosition(offset), GetSlotScale(entry, offset));
            }
            else
            {
                entry.Colored.SetActive(false);
                if (entry.Locked != null)
                {
                    entry.Locked.SetActive(false);
                }
                if (entry.Shadow != null)
                {
                    entry.Shadow.SetActive(false);
                }
            }
        }

        isAnimating = false;
    }

    private Vector2 GetSlotPosition(int offset)
    {
        if (offset == 0)
        {
            return centerPosition;
        }

        return offset < 0 ? leftPosition : rightPosition;
    }

    private float GetSlotScale(BagEntry entry, int offset)
    {
        return offset == 0 ? entry.CenterScale : entry.SideScale;
    }

    private void SetEntryTransform(BagEntry entry, Vector2 position, float scale)
    {
        Vector2 bagPosition = position + new Vector2(0f, entry.VisualOffsetY * scale);

        if (entry.ColoredRect != null)
        {
            entry.ColoredRect.anchoredPosition = bagPosition;
            entry.ColoredRect.localScale = Vector3.one * scale;
        }

        if (entry.LockedRect != null)
        {
            entry.LockedRect.anchoredPosition = bagPosition;
            entry.LockedRect.localScale = Vector3.one * scale;
        }

        if (entry.ShadowRect != null)
        {
            // The shadow keeps the same size and vertical distance for every
            // bag, so it never drops out of the camera when a bag is enlarged.
            entry.ShadowRect.anchoredPosition = position + shadowCenterOffset;
            entry.ShadowRect.localScale = Vector3.one;
            if (entry.Shadow != null)
            {
                entry.Shadow.SetActive(true);
            }
        }
    }

    private Transform FindTransform(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform t in transforms)
        {
            if (t.name == objectName)
            {
                return t;
            }
        }

        return null;
    }

    private GameObject FindAny(string objectName)
    {
        Transform t = FindTransform(objectName);
        return t != null ? t.gameObject : null;
    }
}
