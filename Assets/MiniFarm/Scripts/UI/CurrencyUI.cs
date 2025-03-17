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
    public void UpdateText(int value)
    {
        if (textComponent != null)
        {
            string formattedValue = string.Format("{0:N0}", value); // casedeki virgül formatı
            textComponent.text = formattedValue;
        }
    }
}
public class CurrencyUI : MonoBehaviour
{
    [Inject] private ResourceManager resourceManager;
    [SerializeField] private List<ResourceUIMapping> resourceMappings = new List<ResourceUIMapping>();
    private Dictionary<string, ResourceUIMapping> resourceLookup = new Dictionary<string, ResourceUIMapping>();
    private void Start()
    {
        InitializeResourceMapping();
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
}