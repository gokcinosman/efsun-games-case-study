using UnityEngine;
using Zenject;
public class UIManager : MonoBehaviour
{
    [Inject] private FactoryUI factoryUI;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray))
            {
                factoryUI.Hide();
            }
        }
    }
}
