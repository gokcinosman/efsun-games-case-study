using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using UnityEngine.Experimental.Rendering;
using System.Linq;
public class BaseFactory : MonoBehaviour, IDisposable
{
    #region Serileştirilen Değişkenler
    [SerializeField] protected int capacity;
    [SerializeField] protected Recipe recipe;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private ProductionUI productionUI;
    public FactoryConfig config;
    #endregion
    #region Özellikleri
    public Recipe Recipe => recipe;
    public ProductionUI ProductionUI => productionUI;
    public int CurrentStock => stockReactiveProperty.Value;
    public int ProductionQueue => productionQueue;
    public bool IsProducing
    {
        get => isProducing;
        private set
        {
            if (isProducing != value)
            {
                isProducing = value;
                isProducingReactiveProperty.Value = value;
            }
        }
    }
    #endregion
    #region Reactive Özellikler
    public IObservable<int> OnStockChanged => stockReactiveProperty;
    public IReadOnlyReactiveProperty<int> StockReactiveProperty => stockReactiveProperty;
    public IReadOnlyReactiveProperty<bool> IsProducingReactiveProperty => isProducingReactiveProperty;
    public IReadOnlyReactiveProperty<int> ProductionQueueReactiveProperty => productionQueueReactiveProperty;
    #endregion
    #region Özel Değişkenler
    private readonly ReactiveProperty<int> stockReactiveProperty = new ReactiveProperty<int>(0);
    private readonly ReactiveProperty<bool> isProducingReactiveProperty = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> productionQueueReactiveProperty = new ReactiveProperty<int>(0);
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private bool isProducing;
    private int productionQueue;
    private float remainingProductionTime;
    private float currentProductionTime;
    [Inject] protected ResourceManager resourceManager;
    [Inject] protected ResourceAnimation resourceAnimation;
    [Inject] protected CurrencyUI currencyUI;
    #endregion
    #region Unity Yaşam Döngüsü
    protected virtual void Start()
    {
        InitializeFactory();
        InitializeUI();
    }
    protected virtual void OnDestroy()
    {
        IsProducing = false;
        Dispose();
    }
    #endregion
    #region Başlatma Metodları
    private void InitializeFactory()
    {
        if (config != null)
        {
            capacity = config.capacity;
            recipe = config.recipe;
        }
    }
    private void InitializeUI()
    {
        if (resourceUI != null && productionUI != null)
        {
            resourceUI.Initialize(this);
            productionUI.Initialize(this);
        }
    }
    #endregion
    #region Üretim Yönetimi
    public void AddProductionOrder()
    {
        if (CurrentStock >= capacity)
            return;
        if (!config.requiresInput)
        {
            StartProduction();
            return;
        }
        int potentialProduction = productionQueue + CurrentStock + recipe.outputAmount;
        if (potentialProduction > capacity)
            return;
        if (!HasEnoughResources())
            return;
        ConsumeRequiredResources();
        productionQueue += recipe.outputAmount;
        productionQueueReactiveProperty.Value = productionQueue;
        stockReactiveProperty.SetValueAndForceNotify(CurrentStock);
        if (!IsProducing)
        {
            StartProduction();
        }
    }
    public void RemoveProductionOrder()
    {
        if (productionQueue < recipe.outputAmount || config == null || recipe == null)
            return;
        productionQueue -= recipe.outputAmount;
        productionQueueReactiveProperty.Value = productionQueue;
        foreach (var requirement in recipe.requirements)
        {
            resourceManager.Add(requirement.resourceName, requirement.amount);
        }
        stockReactiveProperty.SetValueAndForceNotify(CurrentStock);
        if (productionQueue <= 0)
        {
            IsProducing = false;
        }
    }
    public void StartProduction()
    {
        if (IsProducing)
            return;
        if (CurrentStock >= capacity ||
            (productionQueue <= 0 && (config != null && config.requiresInput)))
        {
            IsProducing = false;
            return;
        }
        StartProductionAsync().Forget();
    }
    public async UniTask StartProductionAsync()
    {
        if (CurrentStock >= capacity ||
            (productionQueue <= 0 && (config == null || config.requiresInput)))
        {
            IsProducing = false;
            return;
        }
        IsProducing = true;
        // Başlangıçta kalan süreyi hemen ayarla
        remainingProductionTime = remainingProductionTime > 0 ? remainingProductionTime : recipe.productionTime;
        try
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            while (CanContinueProduction())
            {
                float timeToWait = remainingProductionTime;
                currentProductionTime = timeToWait;
                await ProcessProductionTime(timeToWait, cancellationToken);
                if (IsProducing)
                {
                    ProduceItem();
                    remainingProductionTime = 0;
                    if (CanContinueProduction())
                    {
                        remainingProductionTime = recipe.productionTime;
                    }
                    else
                    {
                        IsProducing = false;
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            remainingProductionTime = currentProductionTime;
        }
        finally
        {
            if (CurrentStock >= capacity ||
                (productionQueue <= 0 && (config != null && config.requiresInput)))
            {
                IsProducing = false;
            }
        }
    }
    private async UniTask ProcessProductionTime(float timeToWait, System.Threading.CancellationToken cancellationToken)
    {
        float elapsedTime = 0;
        while (elapsedTime < timeToWait && IsProducing)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: cancellationToken);
            elapsedTime += 0.1f;
            currentProductionTime = timeToWait - elapsedTime;
            remainingProductionTime = currentProductionTime;
        }
    }
    private bool CanContinueProduction()
    {
        return IsProducing &&
               CurrentStock < capacity &&
               (productionQueue > 0 || (config != null && !config.requiresInput));
    }
    private void ProduceItem()
    {
        if (CurrentStock >= capacity)
        {
            IsProducing = false;
            return;
        }
        if (productionQueue > 0)
        {
            productionQueue -= recipe.outputAmount;
            productionQueueReactiveProperty.Value = productionQueue;
            stockReactiveProperty.Value += recipe.outputAmount;
        }
        else if (config != null && !config.requiresInput)
        {
            stockReactiveProperty.Value += recipe.outputAmount;
        }
        else
        {
            IsProducing = false;
            return;
        }
        if (CurrentStock >= capacity || (productionQueue <= 0 && (config == null || config.requiresInput)))
        {
            IsProducing = false;
        }
    }
    #endregion
    #region Kaynak Yönetimi
    private bool HasEnoughResources()
    {
        return recipe.requirements.All(requirement =>
            resourceManager.HasEnough(requirement.resourceName, requirement.amount));
    }
    private void ConsumeRequiredResources()
    {
        foreach (var requirement in recipe.requirements)
        {
            resourceManager.Consume(requirement.resourceName, requirement.amount);
        }
    }
    public int CollectItems()
    {
        int collected = CurrentStock;
        stockReactiveProperty.Value = 0;
        if (collected > 0 && config != null && recipe != null)
        {
            string outputResourceName = recipe.outputResourceName;
            currencyUI.PlayResourceCollectAnimation(
                transform.position,
                outputResourceName,
                collected,
                recipe.outputResourceIcon,
                () => resourceManager.Add(outputResourceName, collected)
            );
        }
        if (config != null && !config.requiresInput && CurrentStock < capacity && !IsProducing)
        {
            StartProduction();
        }
        return collected;
    }
    #endregion
    #region Değer Ayarlama Metodları
    public void SetStockDirectly(int value)
    {
        if (value >= 0 && value <= capacity)
        {
            stockReactiveProperty.Value = value;
        }
    }
    public void SetProductionQueue(int value)
    {
        if (value >= 0)
        {
            productionQueue = value;
            productionQueueReactiveProperty.Value = value;
        }
    }
    public void SetRemainingProductionTime(float value)
    {
        if (value >= 0)
        {
            if (CurrentStock >= capacity)
            {
                remainingProductionTime = 0;
                IsProducing = false;
            }
            else
            {
                remainingProductionTime = value;
            }
        }
    }
    public float GetRemainingProductionTime()
    {
        if (!IsProducing || CurrentStock >= capacity)
            return 0f;
        return remainingProductionTime;
    }
    #endregion
    #region IDisposable Uygulaması
    public void Dispose()
    {
        _disposables.Dispose();
        stockReactiveProperty.Dispose();
        isProducingReactiveProperty.Dispose();
        productionQueueReactiveProperty.Dispose();
    }
    #endregion
}