using UnityEngine;
public class UIPositioner : MonoBehaviour
{
    private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}