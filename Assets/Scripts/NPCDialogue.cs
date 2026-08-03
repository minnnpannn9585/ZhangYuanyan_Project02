using UnityEngine;
using UnityEngine.UI;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private string[] dialogueLines;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject interactHint;

    [Header("Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool isPlayerInRange;
    private bool isDialogueOpen;
    private int currentLineIndex;

    private void Start()
    {
        CloseDialogue();

        if (interactHint != null)
        {
            interactHint.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange && !isDialogueOpen && Input.GetKeyDown(interactKey))
        {
            OpenDialogue();
        }

        if (isDialogueOpen && Input.GetMouseButtonDown(0))
        {
            ShowNextLine();
        }
    }

    private void OpenDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        isDialogueOpen = true;
        currentLineIndex = 0;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (interactHint != null)
        {
            interactHint.SetActive(false);
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Length)
        {
            CloseDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void CloseDialogue()
    {
        isDialogueOpen = false;
        currentLineIndex = 0;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (interactHint != null)
        {
            interactHint.SetActive(isPlayerInRange);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        isPlayerInRange = true;

        if (interactHint != null && !isDialogueOpen)
        {
            interactHint.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        isPlayerInRange = false;

        if (interactHint != null)
        {
            interactHint.SetActive(false);
        }

        if (isDialogueOpen)
        {
            CloseDialogue();
        }
    }
}
