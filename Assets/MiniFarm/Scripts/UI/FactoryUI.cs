using UnityEngine;
using UnityEngine.UI;
using Zenject;
public class FactoryUI : MonoBehaviour
{
    // sağda ve solda -,+ production butonları olacak bu butonlar factorye hammadde ekleyip çıkarmak için kullanılacak
    // production butonlarının inaktif özellikleri olacak.
    // sıra boşsa -1 butonu inaktif
    // sıra doluysa veya hammadde bulunmuyorsa +1 butonu inaktif
    // factorye tıklandığında factoryui açılacak
    private BaseFactory currentFactory;
    private ResourceManager resourceManager;
    [Inject]
    public void Construct(ResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }
    public void SetFactory(BaseFactory factory)
    {
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
