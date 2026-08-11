using UnityEngine;

public class ObjectInBagHandle : MonoBehaviour
{
    private ObjectInBag objectInBag;

    private void Awake()
    {
        objectInBag = GetComponentInParent<ObjectInBag>();
    }

    private void OnMouseDown()
    {
        if (objectInBag != null)
        {
            objectInBag.BeginDrag();
        }
    }

    private void OnMouseUp()
    {
        if (objectInBag != null)
        {
            objectInBag.EndDrag();
        }
    }
}
