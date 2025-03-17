using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
public class ProductionUI : MonoBehaviour
{
    // sağda ve solda -,+ production butonları olacak bu butonlar factorye hammadde ekleyip çıkarmak için kullanılacak
    // production butonlarının inaktif özellikleri olacak.
    // sıra boşsa -1 butonu inaktif
    // sıra doluysa veya hammadde bulunmuyorsa +1 butonu inaktif
    // factorye tıklandığında factoryui açılacak
    private BaseFactory factory;
    [Inject] private ResourceManager resourceManager;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TextMeshProUGUI addButtonText;
    [SerializeField] private TextMeshProUGUI removeButtonText;
    [SerializeField] private TextMeshProUGUI requiredMaterialText;
    [SerializeField] private Image requiredMaterialIcon;
    [SerializeField] private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    private UIPositioner positioner;
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
        if (factory.config != null &&
            factory.config.recipe != null &&
            factory.config.recipe.requirements != null &&
            factory.config.recipe.requirements.Count > 0)
        {
            removeButton.onClick.AddListener(() =>
            {
                factory.RemoveProductionOrder();
                UpdateButtonStates();
            });
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
                UpdateButtonStates();
            });
            if (factory.config.removeAmount > 0)
                removeButtonText.text = "-" + factory.config.removeAmount.ToString();
            if (factory.config.addAmount > 0)
                addButtonText.text = "+" + factory.config.addAmount.ToString();
            requiredMaterialIcon.sprite = factory.config.recipe.requirements[0].resourceIcon;
            requiredMaterialText.text = "x" + factory.config.recipe.requirements[0].amount.ToString();
            UpdateButtonStates();
            gameObject.SetActive(false);
            if (positioner != null)
                positioner.SetTarget(targetTransform);
        }
        else
        {
            Hide();
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        if (positioner != null && targetTransform != null)
        {
            positioner.SetTarget(targetTransform);
        }
        UpdateButtonStates();
    }
    private void UpdateButtonStates()
    {
        if (factory == null || factory.config == null || factory.config.recipe == null) return;
        removeButton.interactable = factory.ProductionQueue > 0;
        bool canAdd = true;
        int potentialProduction = factory.ProductionQueue + factory.CurrentStock + factory.config.recipe.outputAmount;
        if (potentialProduction > factory.config.capacity)
        {
            canAdd = false;
        }
        if (canAdd && factory.config.recipe.requirements.Count > 0)
        {
            foreach (var requirement in factory.config.recipe.requirements)
            {
                if (!resourceManager.HasEnough(requirement.resourceName, requirement.amount))
                {
                    canAdd = false;
                    break;
                }
            }
        }
        addButton.interactable = canAdd;
    }
}
