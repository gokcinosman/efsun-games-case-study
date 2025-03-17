using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UniRx;
public class ProductionUI : MonoBehaviour
{
    [Inject] private ResourceManager resourceManager;
    private BaseFactory factory;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TextMeshProUGUI addButtonText;
    [SerializeField] private TextMeshProUGUI removeButtonText;
    [SerializeField] private TextMeshProUGUI requiredMaterialText;
    [SerializeField] private Image requiredMaterialIcon;
    [SerializeField] private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    private UIPositioner positioner;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
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
        if (!IsValidFactory())
        {
            Hide();
            return;
        }
        SetupButtons();
        SetupButtonTexts();
        SetupRequiredMaterials();
        SubscribeToFactoryEvents();
        gameObject.SetActive(false);
        if (positioner != null)
            positioner.SetTarget(targetTransform);
    }
    private bool IsValidFactory()
    {
        return factory.config != null &&
               factory.config.recipe != null &&
               factory.config.recipe.requirements != null &&
               factory.config.recipe.requirements.Count > 0;
    }
    private void SetupButtons()
    {
        removeButton.onClick.AddListener(() => factory.RemoveProductionOrder());
        addButton.onClick.AddListener(() =>
        {
            var requirement = factory.config.recipe.requirements[0];
            bool hasEnoughResources = resourceManager.HasEnough(requirement.resourceName, requirement.amount);
            int potentialProduction = factory.ProductionQueue + factory.CurrentStock + factory.config.recipe.outputAmount;
            bool hasCapacity = potentialProduction <= factory.config.capacity;
            if (hasEnoughResources && hasCapacity)
            {
                factory.AddProductionOrder();
            }
        });
    }
    private void SetupButtonTexts()
    {
        if (factory.config.removeAmount > 0)
            removeButtonText.text = "-" + factory.config.removeAmount.ToString();
        if (factory.config.addAmount > 0)
            addButtonText.text = "+" + factory.config.addAmount.ToString();
    }
    private void SetupRequiredMaterials()
    {
        requiredMaterialIcon.sprite = factory.config.recipe.requirements[0].resourceIcon;
        requiredMaterialText.text = "x" + factory.config.recipe.requirements[0].amount.ToString();
    }
    private void SubscribeToFactoryEvents()
    {
        factory.StockReactiveProperty
            .Subscribe(_ => UpdateButtonStates())
            .AddTo(_disposables);
        factory.IsProducingReactiveProperty
            .Subscribe(_ => UpdateButtonStates())
            .AddTo(_disposables);
        factory.ProductionQueueReactiveProperty
            .Subscribe(_ => UpdateButtonStates())
            .AddTo(_disposables);
        UpdateButtonStates();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        ActivateCanvasGroup();
        if (positioner != null && targetTransform != null)
        {
            positioner.SetTarget(targetTransform);
        }
        UpdateButtonStates();
    }
    private void ActivateCanvasGroup()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
    private void UpdateButtonStates()
    {
        if (factory == null || factory.config == null || factory.config.recipe == null)
            return;
        removeButton.interactable = factory.ProductionQueue > 0;
        addButton.interactable = CanAddToProduction();
    }
    private bool CanAddToProduction()
    {
        // Kapasite kontrolü
        int potentialProduction = factory.ProductionQueue + factory.CurrentStock + factory.config.recipe.outputAmount;
        if (potentialProduction > factory.config.capacity)
        {
            return false;
        }
        // Gerekli kaynakların kontrolü
        if (factory.config.recipe.requirements.Count > 0)
        {
            foreach (var requirement in factory.config.recipe.requirements)
            {
                if (!resourceManager.HasEnough(requirement.resourceName, requirement.amount))
                {
                    return false;
                }
            }
        }
        return true;
    }
    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
