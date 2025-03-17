using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
public class MouseClickManager : MonoBehaviour
{
    private Camera mainCamera;
    private ProductionUI currentOpenUI;
    private BaseFactory lastClickedFactory;
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
                    // Hay Factory veya requirementi olmayan factory'ler için özel durum
                    if (factory.config != null && !factory.config.requiresInput)
                    {
                        int collected = factory.CollectItems();
                        Debug.Log($"{collected} adet ürün toplandı");
                        if (currentOpenUI != null)
                        {
                            currentOpenUI.Hide();
                            currentOpenUI = null;
                            lastClickedFactory = null;
                        }
                        return;
                    }
                    if (factory == lastClickedFactory && currentOpenUI != null && currentOpenUI == factory.ProductionUI)
                    {
                        int collected = factory.CollectItems();
                        Debug.Log($"{collected} adet ürün toplandı");
                    }
                    else // İlk kez tıklandıysa veya farklı bir factory'e tıklandıysa
                    {
                        if (factory.ProductionUI != null && factory.config != null && factory.config.requiresInput)
                        {
                            if (currentOpenUI != null && currentOpenUI != factory.ProductionUI)
                            {
                                currentOpenUI.Hide();
                            }
                            factory.ProductionUI.Show();
                            currentOpenUI = factory.ProductionUI;
                            lastClickedFactory = factory;
                        }
                    }
                }
                else // Factory olmayan bir yere tıklandıysa
                {
                    if (currentOpenUI != null)
                    {
                        currentOpenUI.Hide();
                        currentOpenUI = null;
                        lastClickedFactory = null;
                    }
                }
            }
            else // Hiçbir şeye tıklanmadıysa
            {
                if (currentOpenUI != null)
                {
                    currentOpenUI.Hide();
                    currentOpenUI = null;
                    lastClickedFactory = null;
                }
            }
        }
    }
}
