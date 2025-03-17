using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
public class MouseClickManager : MonoBehaviour
{
    private Camera mainCamera;
    private ProductionUI currentOpenUI;
    private void Start()
    {
        mainCamera = Camera.main;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                BaseFactory factory = hit.collider.GetComponent<BaseFactory>();
                if (factory == null)
                {
                    factory = hit.collider.GetComponentInParent<BaseFactory>();
                }
                if (factory != null)
                {
                    if (factory.ProductionUI != null)
                    {
                        if (factory.config != null)
                        {
                            if (factory.config.requiresInput)
                            {
                                if (currentOpenUI != null && currentOpenUI != factory.ProductionUI)
                                {
                                    currentOpenUI.Hide();
                                }
                                factory.ProductionUI.Show();
                                currentOpenUI = factory.ProductionUI;
                            }
                        }
                    }
                }
                else
                {
                    if (currentOpenUI != null)
                    {
                        currentOpenUI.Hide();
                        currentOpenUI = null;
                    }
                }
            }
            else
            {
                if (currentOpenUI != null)
                {
                    currentOpenUI.Hide();
                    currentOpenUI = null;
                }
            }
        }
    }
}
