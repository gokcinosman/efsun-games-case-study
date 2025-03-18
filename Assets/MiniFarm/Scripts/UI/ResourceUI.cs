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
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceAmountText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private Slider productionProgressSlider;
    [SerializeField] private GameObject resourcePanel;
    [SerializeField] private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    private BaseFactory factory;
    private UIPositioner positioner;
    private float productionTimer = 0f;
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private bool isInitialized = false;
    private void Awake()
    {
        InitializePositioner();
    }
    private void InitializePositioner()
    {
        positioner = GetComponent<UIPositioner>();
        if (positioner == null)
        {
            Debug.LogError("UIPositioner ekle.");
            return;
        }
        positioner.SetOffset(offset);
    }
    public void Initialize(BaseFactory factory)
    {
        this.factory = factory;
        targetTransform = factory.transform;
        positioner.SetTarget(targetTransform);
        disposables.Clear();
        SubscribeToFactoryEvents();
        SetupUIElements();
        transform.localScale = -1 * transform.localScale;
        TrackProductionProgressAsync().Forget();
        if (factory.IsProducing)
        {
            UpdateProductionProgress(0f);
        }
        isInitialized = true;
    }
    private void SubscribeToFactoryEvents()
    {
        // Stok değişikliği
        factory.OnStockChanged
            .Subscribe(amount =>
            {
                UpdateResourceAmount(amount);
                UpdateQueueText(factory.ProductionQueue);
            })
            .AddTo(disposables);
        // Üretim durumu değişikliği
        factory.ObserveEveryValueChanged(f => f.IsProducing)
            .Subscribe(isProducing =>
            {
                UpdateUIState();
                if (isProducing)
                {
                    productionTimer = 0f;
                }
            })
            .AddTo(disposables);
        // Üretim sırası değişikliği
        factory.ObserveEveryValueChanged(f => f.ProductionQueue)
            .Subscribe(queue => UpdateQueueText(queue))
            .AddTo(disposables);
    }
    private void SetupUIElements()
    {
        UpdateUIState();
        UpdateQueueText(factory.ProductionQueue);
        if (factory.config != null && factory.config.recipe.outputResourceIcon != null)
        {
            resourceIcon.sprite = factory.config.recipe.outputResourceIcon;
        }
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
            UpdateTimerText(progress);
        }
        UpdateUIState();
    }
    private void UpdateTimerText(float progress)
    {
        if (factory != null)
        {
            float remainingTime = factory.GetRemainingProductionTime();
            timerText.text = remainingTime.ToString("F0") + " sn";
            timerText.color = Color.white;
        }
    }
    private void UpdateQueueText(int queue)
    {
        if (queueText == null || factory == null || factory.config == null)
            return;
        int total = queue + factory.CurrentStock;
        queueText.text = $"{total}/{factory.config.capacity}";
        UpdateQueueTextColor(total, queue);
    }
    private void UpdateQueueTextColor(int total, int queue)
    {
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
    private void UpdateUIState()
    {
        if (factory == null || factory.config == null)
            return;
        bool hasProduction = factory.IsProducing;
        bool hasResources = factory.CurrentStock > 0;
        bool hasQueue = factory.ProductionQueue > 0;
        resourcePanel.SetActive(hasProduction || hasResources || hasQueue);
        bool isFull = factory.CurrentStock >= factory.config.capacity;
        if (isFull)
        {
            // Stok dolu olduğunda slider'ı sıfırla
            productionProgressSlider.value = 1f;
            if (timerText != null)
            {
                timerText.text = "FULL";
                timerText.color = Color.red;
            }
        }
    }
    private async UniTaskVoid TrackProductionProgressAsync()
    {
        await UniTask.DelayFrame(0);
        while (this != null && isInitialized && gameObject.activeInHierarchy)
        {
            bool isFull = factory.CurrentStock >= factory.config.capacity;
            if (IsFactoryProducing() && !isFull)
            {
                float remainingTime = factory.GetRemainingProductionTime();
                float totalTime = factory.config.recipe.productionTime;
                if (totalTime <= 0f) totalTime = 1f;
                float progress = 1f - (remainingTime / totalTime);
                progress = Mathf.Clamp01(progress);
                UpdateProductionProgress(progress);
            }
            else
            {
                UpdateProductionProgress(0f);
            }
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    private bool IsFactoryProducing()
    {
        return factory != null &&
               factory.IsProducing &&
               factory.CurrentStock < factory.config.capacity &&
               factory.config != null &&
               factory.config.recipe != null;
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
