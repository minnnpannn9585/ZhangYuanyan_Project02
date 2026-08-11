using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagButton : MonoBehaviour
{
    [SerializeField] private GameObject levelSelectCanvas;
    [SerializeField] private TransitionImage transitionImage;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CloseLevelSelectCanvas);
        }
    }

    public void CloseLevelSelectCanvas()
    {
        if (transitionImage != null)
        {
            if (button != null)
            {
                button.interactable = false;
            }

            transitionImage.PlayTransition(DisableCanvas);
            return;
        }

        DisableCanvas();
    }

    private void DisableCanvas()
    {
        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(false);
        }

        if (button != null)
        {
            button.interactable = true;
        }
    }
}
