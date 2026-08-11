using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInBag : MonoBehaviour
{
    [Header("Drag")]
    [SerializeField] private float switchY = 2.6f;

    [Header("States")]
    [SerializeField] private GameObject firstChild;
    [SerializeField] private GameObject secondChild;

    private Camera mainCamera;
    private bool isDragging;
    private Vector3 dragOffset;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        UpdateChildState();
    }

    private void Update()
    {
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }

        if (isDragging)
        {
            Vector3 targetPosition = GetMouseWorldPosition() + dragOffset;
            targetPosition.z = transform.position.z;
            transform.position = targetPosition;
        }

        UpdateChildState();
    }

    public void BeginDrag()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPosition;
        isDragging = true;
    }

    public void EndDrag()
    {
        isDragging = false;
    }

    private void UpdateChildState()
    {
        bool useFirstChild = transform.position.y > switchY;

        if (firstChild != null)
        {
            firstChild.SetActive(useFirstChild);
        }

        if (secondChild != null)
        {
            secondChild.SetActive(!useFirstChild);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePosition);
    }
}
