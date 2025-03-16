using UnityEngine;
using Zenject;
public class MouseClickManager : MonoBehaviour
{
    [Inject] private FactoryUI factoryUI;
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                BaseFactory factory = hit.collider.GetComponent<BaseFactory>();
                if (factory != null)
                {
                    factoryUI.SetFactory(factory);
                }
            }
        }
    }
}
