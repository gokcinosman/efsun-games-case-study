using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UniRx;
using System.Linq;
using Zenject;
[System.Serializable]
public class ResourceUIMapping
{
    public string resourceKey;
    public TextMeshProUGUI textComponent;
    public RectTransform targetTransform; // Animasyon hedefi
    public void UpdateText(int value)
    {
        if (textComponent != null)
        {
            string formattedValue = string.Format("{0:N0}", value); // case study'deki virgül formatı
            textComponent.text = formattedValue;
        }
    }
}
public class CurrencyUI : MonoBehaviour
{
    [Inject] private ResourceManager resourceManager;
    [Inject] private ResourceAnimation resourceAnimation;
    [SerializeField] private List<ResourceUIMapping> resourceMappings = new List<ResourceUIMapping>();
    private Dictionary<string, ResourceUIMapping> resourceLookup = new Dictionary<string, ResourceUIMapping>();
    private void Start()
    {
        InitializeResourceMapping();
        UpdateResourceDisplay(resourceManager.GetAllResources());
        resourceManager.OnResourceChanged
            .Subscribe(UpdateResourceDisplay)
            .AddTo(this);
    }
    private void InitializeResourceMapping()
    {
        resourceLookup.Clear();
        foreach (var mapping in resourceMappings)
        {
            if (!string.IsNullOrEmpty(mapping.resourceKey) && mapping.textComponent != null)
            {
                resourceLookup[mapping.resourceKey] = mapping;
            }
        }
    }
    private void UpdateResourceDisplay(Dictionary<string, int> resources)
    {
        foreach (var resource in resources)
        {
            if (resourceLookup.TryGetValue(resource.Key, out ResourceUIMapping mapping))
            {
                mapping.UpdateText(resource.Value);
            }
        }
    }
    public RectTransform GetResourceUITarget(string resourceKey)
    {
        if (resourceLookup.TryGetValue(resourceKey, out ResourceUIMapping mapping))
        {
            return mapping.targetTransform;
        }
        return null;
    }
    public void PlayResourceCollectAnimation(Vector3 worldPosition, string resourceKey, int amount, Sprite resourceSprite, System.Action onCompleteCallback = null)
    {
        RectTransform target = GetResourceUITarget(resourceKey);
        if (target != null)
        {
            resourceAnimation.PlayResourceCollectAnimationToTarget(worldPosition, resourceSprite, amount, target, onCompleteCallback);
        }
        else if (onCompleteCallback != null)
        {
            onCompleteCallback.Invoke();
        }
    }
}