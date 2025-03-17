using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using System;
using Cysharp.Threading.Tasks;
public class ResourceUI : MonoBehaviour
{   // anlık hammadde miktarı
    // süre sliderı
    // hammadde spriteı
    // ondalıklı bir üretim sırası göstergeci (hay factory dahil değil çünkü sınırsız üretim)
    // eğer üretim varsa ui çalışsın. üretim yoksa fakat hammadde varsa hammadde miktarı gözüken bir ui olacak
    [Header("UI Bileşenleri")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceAmountText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private Slider productionProgressSlider;
    [SerializeField] private GameObject resourcePanel;
    [Header("Ayarlar")]
    [SerializeField] private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    private BaseFactory factory;
    private UIPositioner positioner;
    private float productionTimer = 0f;
    private CompositeDisposable disposables = new CompositeDisposable();
    private bool isInitialized = false;
    private void Awake()
    {
        positioner = GetComponent<UIPositioner>();
        if (positioner == null)
        {
            Debug.LogError("UIPositioner ekle.");
        }
        positioner.SetOffset(offset);
    }
    public void Initialize(BaseFactory factory)
    {
        this.factory = factory;
        targetTransform = factory.transform;
        positioner.SetTarget(targetTransform);
        disposables.Clear();
        factory.OnStockChanged
            .Subscribe(amount =>
            {
                UpdateResourceAmount(amount);
                UpdateQueueText(factory.ProductionQueue);
            })
            .AddTo(disposables);
        factory.ObserveEveryValueChanged(f => f.IsProducing())
            .Subscribe(isProducing =>
            {
                UpdateUIState();
                if (isProducing)
                {
                    productionTimer = 0f;
                }
            })
            .AddTo(disposables);
        factory.ObserveEveryValueChanged(f => f.ProductionQueue)
            .Subscribe(queue => UpdateQueueText(queue))
            .AddTo(disposables);
        UpdateUIState();
        UpdateQueueText(factory.ProductionQueue);
        if (factory.config != null && factory.config.recipe.outputResourceIcon != null)
        {
            resourceIcon.sprite = factory.config.recipe.outputResourceIcon;
        }
        transform.localScale = -1 * transform.localScale;
        TrackProductionProgressAsync().Forget();
        if (factory.IsProducing())
        {
            UpdateProductionProgress(0f);
        }
        isInitialized = true;
    }
    private void UpdateResourceAmount(int amount)
    {
        resourceAmountText.text = amount.ToString();
        UpdateUIState();
    }
    private void UpdateProductionProgress(float progress)
    {
        productionProgressSlider.value = progress;
        if (timerText != null && factory != null && factory.config != null)
        {
            float remainingTime = factory.config.recipe.productionTime * (1 - progress);
            timerText.text = remainingTime.ToString("F0") + " sn";
            timerText.color = Color.white;
        }
        UpdateUIState();
    }
    private void UpdateQueueText(int queue)
    {
        if (queueText != null && factory != null && factory.config != null)
        {
            int total = queue + factory.CurrentStock;
            queueText.text = $"{total}/{factory.config.capacity}";
            if (total >= factory.config.capacity)
            {
                queueText.color = Color.red;
            }
            else if (queue > 0)
            {
                queueText.color = Color.yellow;
            }
            else
            {
                queueText.color = Color.white;
            }
        }
    }
    private void UpdateUIState()
    {
        if (factory == null || factory.config == null) return;
        bool hasProduction = factory.IsProducing();
        bool hasResources = factory.CurrentStock > 0;
        bool hasQueue = factory.ProductionQueue > 0;
        resourcePanel.SetActive(hasProduction || hasResources || hasQueue);
        bool isFull = factory.CurrentStock >= factory.config.capacity;
        if (isFull && timerText != null)
        {
            timerText.text = "FULL";
            timerText.color = Color.red;
        }
    }
    private async UniTaskVoid TrackProductionProgressAsync()
    {
        await UniTask.DelayFrame(0);
        while (this != null && isInitialized && gameObject.activeInHierarchy)
        {
            if (factory != null && factory.IsProducing() && factory.config != null && factory.config.recipe != null)
            {
                productionTimer += Time.deltaTime;
                float progress = Mathf.Repeat(productionTimer, factory.config.recipe.productionTime) / factory.config.recipe.productionTime;
                UpdateProductionProgress(progress);
            }
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    private void OnDestroy()
    {
        disposables.Dispose();
        isInitialized = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = Time.timeScale * 10;
        }
    }
}
