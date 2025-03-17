using System;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
public class MouseClickManager : MonoBehaviour
{
    private Camera mainCamera;
    private ProductionUI currentOpenUI;
    private BaseFactory lastClickedFactory;
    private CompositeDisposable disposables = new CompositeDisposable();
    private void Start()
    {
        mainCamera = Camera.main;
        this.UpdateAsObservable()
            .Where(_ => Input.GetMouseButtonDown(0))
            .Where(_ => !EventSystem.current.IsPointerOverGameObject())
            .Subscribe(_ => HandleMouseClick().Forget())
            .AddTo(disposables);
    }
    private async UniTaskVoid HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            await ProcessRaycastHit(hit);
        }
        else
        {
            // Hiçbir şeye tıklanmadıysa
            CloseCurrentUI();
        }
    }
    private async UniTask ProcessRaycastHit(RaycastHit hit)
    {
        BaseFactory factory = hit.collider.GetComponent<BaseFactory>() ??
                             hit.collider.GetComponentInParent<BaseFactory>();
        if (factory != null)
        {
            await ProcessFactoryClick(factory);
        }
        else
        {
            // Factory olmayan bir yere tıklandıysa
            CloseCurrentUI();
        }
    }
    private UniTask ProcessFactoryClick(BaseFactory factory)
    {
        // Hay Factory veya requirementi olmayan factory'ler için özel durum
        if (factory.config != null && !factory.config.requiresInput)
        {
            factory.CollectItems();
            CloseCurrentUI();
            return UniTask.CompletedTask;
        }
        if (factory == lastClickedFactory && currentOpenUI != null && currentOpenUI == factory.ProductionUI)
        {
            factory.CollectItems();
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
        return UniTask.CompletedTask;
    }
    private void CloseCurrentUI()
    {
        if (currentOpenUI != null)
        {
            currentOpenUI.Hide();
            currentOpenUI = null;
            lastClickedFactory = null;
        }
    }
    private void OnDestroy()
    {
        disposables.Dispose();
    }
}
