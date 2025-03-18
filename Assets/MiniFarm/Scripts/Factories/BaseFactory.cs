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
    [SerializeField] protected int capacity;
    [SerializeField] protected Recipe recipe;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private ProductionUI productionUI;
    public FactoryConfig config;
    public ProductionUI ProductionUI => productionUI;
    public int CurrentStock => stockReactiveProperty.Value;
    public int ProductionQueue => productionQueue;
    public IObservable<int> OnStockChanged => stockReactiveProperty;
    public IReadOnlyReactiveProperty<int> StockReactiveProperty => stockReactiveProperty;
    private readonly ReactiveProperty<int> stockReactiveProperty = new ReactiveProperty<int>(0);
    private readonly ReactiveProperty<bool> isProducingReactiveProperty = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> productionQueueReactiveProperty = new ReactiveProperty<int>(0);
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private bool isProducing;
    private int productionQueue;
    [Inject] protected ResourceManager resourceManager;
    private bool stockInitialized = false;
    protected virtual void Start()
    {
        InitializeFactory();
        InitializeUI();
        // Eğer stok değeri başka bir yerden zaten ayarlanmışsa, otomatik üretimi başlatma
        if (!stockInitialized)
        {
            TryStartAutoProduction();
        }
    }
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
    private void TryStartAutoProduction()
    {
        if (recipe != null && config != null && !config.requiresInput && CurrentStock < capacity)
        {
            StartProduction();
        }
    }
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
        stockReactiveProperty.SetValueAndForceNotify(CurrentStock);
        if (!IsProducing)
        {
            StartProduction();
        }
    }
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
    public void RemoveProductionOrder()
    {
        if (productionQueue >= recipe.outputAmount && config != null && recipe != null)
        {
            productionQueue -= recipe.outputAmount;
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
        try
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            while (IsProducing &&
                   CurrentStock < capacity &&
                   (productionQueue > 0 || (config != null && !config.requiresInput)))
            {
                await UniTask.Delay(TimeSpan.FromSeconds(recipe.productionTime), cancellationToken: cancellationToken);
                ProduceItem();
            }
        }
        catch (OperationCanceledException)
        {
            // İşlem iptal edildiğinde yapılacak birşey yok
        }
    }
    public void StartProduction()
    {
        if (IsProducing) return;
        StartProductionAsync().Forget();
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
    public int CollectItems()
    {
        int collected = CurrentStock;
        stockReactiveProperty.Value = 0;
        if (collected > 0 && config != null && recipe != null)
        {
            string outputResourceName = recipe.outputResourceName;
            resourceManager.Add(outputResourceName, collected);
        }
        if (config != null && !config.requiresInput && CurrentStock < capacity && !IsProducing)
        {
            StartProduction();
        }
        return collected;
    }
    public void Dispose()
    {
        _disposables.Dispose();
        stockReactiveProperty.Dispose();
        isProducingReactiveProperty.Dispose();
        productionQueueReactiveProperty.Dispose();
    }
    protected virtual void OnDestroy()
    {
        IsProducing = false;
        Dispose();
    }
    public void SetStockDirectly(int value)
    {
        if (value >= 0 && value <= capacity)
        {
            stockInitialized = true;
            stockReactiveProperty.Value = value;
            Debug.Log($"{gameObject.name} fabrikasının stok değeri {value} olarak ayarlandı");
        }
    }
    public IReadOnlyReactiveProperty<bool> IsProducingReactiveProperty => isProducingReactiveProperty;
    public IReadOnlyReactiveProperty<int> ProductionQueueReactiveProperty => productionQueueReactiveProperty;
}
