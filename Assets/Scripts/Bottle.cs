using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Bottle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Shake")]
    [SerializeField] private float rotateAngle = 8f;
    [SerializeField] private float rotateSpeed = 8f;
    [SerializeField] private int shakeCount = 2;

    private RectTransform rectTransform;
    private Quaternion initialRotation;
    private bool isAnimating;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialRotation = rectTransform.localRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAnimating || rectTransform == null)
        {
            return;
        }

        shakeCoroutine = StartCoroutine(PlayShake());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        isAnimating = false;
        rectTransform.localRotation = initialRotation;
    }

    private IEnumerator PlayShake()
    {
        isAnimating = true;

        float duration = Mathf.PI * 2f * shakeCount;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime * rotateSpeed;
            float angle = Mathf.Sin(timer) * rotateAngle;
            rectTransform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        rectTransform.localRotation = initialRotation;
        isAnimating = false;
        shakeCoroutine = null;
    }
}
