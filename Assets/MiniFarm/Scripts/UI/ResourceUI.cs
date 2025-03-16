using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
public class ResourceUI : MonoBehaviour
{   // anlık hammadde miktarı
    // süre sliderı
    // hammadde spriteı
    // ondalıklı bir üretim sırası göstergeci (hay factory dahil değil çünkü sınırsız üretim)
    // eğer üretim varsa ui çalışsın. üretim yoksa fakat hammadde varsa hammadde miktarı gözüken bir ui olacak
    [Header("UI Bileşenleri")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceAmountText;
    [SerializeField] private Slider productionProgressSlider;
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private GameObject resourcePanel;
    [Header("Ayarlar")]
    [SerializeField] private Vector3 offset = new Vector3(0, -2f, 0);
    private Transform targetTransform;
    private BaseFactory factory;
    private float productionTimer = 0f;
    public void Initialize(BaseFactory factory)
    {
        this.factory = factory;
        targetTransform = factory.transform;
        factory.OnStockChanged.Subscribe(amount =>
        {
            UpdateResourceAmount(amount);
        }).AddTo(this);
        UpdateUIState();
        if (factory.config != null && factory.config.recipe.outputResourceIcon != null)
        {
            resourceIcon.sprite = factory.config.recipe.outputResourceIcon;
        }
        transform.localScale = -1 * transform.localScale;
    }
    private void UpdateResourceAmount(int amount)
    {
        resourceAmountText.text = amount.ToString();
        UpdateUIState();
    }
    private void UpdateProductionProgress(float progress)
    {
        productionProgressSlider.value = progress;
        UpdateUIState();
    }
    private void UpdateUIState()
    {
        bool hasProduction = factory.IsProducing();
        bool hasResources = factory.CurrentStock > 0;
        productionPanel.SetActive(hasProduction);
        // resourcePanel.SetActive(!hasProduction && hasResources);
    }
    private void Update()
    {
        if (factory.IsProducing() && factory.config != null && factory.config.recipe != null)
        {
            productionTimer += Time.deltaTime;
            float progress = Mathf.Repeat(productionTimer, factory.config.recipe.productionTime) / factory.config.recipe.productionTime;
            UpdateProductionProgress(progress);
        }
    }
    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
