using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Soft outer glow shown while the mouse hovers over a bag.
/// The glow uses a pre-blurred silhouette texture, tinted to the bag's colour,
/// so it reads as a soft mist instead of a hard duplicated outline.
/// </summary>
public class BagHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform sourceRect;
    private Image sourceImage;

    private Sprite glowSprite;
    private Color glowColor = Color.white;
    private Vector2 glowSizeMultiplier = Vector2.one;

    private GameObject glowRoot;
    private RectTransform glowRect;
    private CanvasGroup glowGroup;
    private float targetAlpha;
    private bool isConfigured;

    private void Awake()
    {
        sourceRect = transform as RectTransform;
        sourceImage = GetComponent<Image>();
    }

    public void Initialize(Sprite sprite, Color color, Vector2 sizeMultiplier)
    {
        glowSprite = sprite;
        glowColor = color;
        glowSizeMultiplier = sizeMultiplier;
        CreateGlow();
    }

    private void OnEnable()
    {
        if (glowRoot != null)
        {
            glowRoot.SetActive(true);
        }
    }

    private void OnDisable()
    {
        targetAlpha = 0f;

        if (glowGroup != null)
        {
            glowGroup.alpha = 0f;
        }

        if (glowRoot != null)
        {
            glowRoot.SetActive(false);
        }
    }

    private void CreateGlow()
    {
        if (isConfigured ||
            glowSprite == null ||
            sourceRect == null ||
            sourceImage == null)
        {
            return;
        }

        Transform parent = sourceRect.parent;
        if (parent == null)
        {
            return;
        }

        isConfigured = true;

        glowRoot = new GameObject("Glow", typeof(RectTransform), typeof(CanvasGroup));
        glowRoot.transform.SetParent(parent, false);
        glowRoot.transform.SetSiblingIndex(sourceRect.GetSiblingIndex());

        glowRect = glowRoot.GetComponent<RectTransform>();
        CopyRect(sourceRect, glowRect);

        glowGroup = glowRoot.GetComponent<CanvasGroup>();
        glowGroup.alpha = 0f;
        glowGroup.interactable = false;
        glowGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject(
            "GlowImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(glowRect, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(
            sourceRect.sizeDelta.x * glowSizeMultiplier.x,
            sourceRect.sizeDelta.y * glowSizeMultiplier.y);

        Image glowImage = imageObject.GetComponent<Image>();
        glowImage.sprite = glowSprite;
        glowImage.type = Image.Type.Simple;
        glowImage.preserveAspect = false;
        glowImage.raycastTarget = false;
        glowImage.color = glowColor;
    }

    private void LateUpdate()
    {
        if (glowRoot == null || glowRect == null || sourceRect == null || glowGroup == null)
        {
            return;
        }

        CopyRect(sourceRect, glowRect);

        // Only the currently visible/clickable bag version should glow.
        if (sourceImage != null && !sourceImage.raycastTarget)
        {
            targetAlpha = 0f;
        }

        glowGroup.alpha = Mathf.MoveTowards(
            glowGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime * 6f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = 0f;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }
}
