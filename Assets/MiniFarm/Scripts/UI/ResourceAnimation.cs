using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ResourceAnimation : MonoBehaviour
{
    [SerializeField] private GameObject resourceCollectAnimPrefab;
    [SerializeField] private Canvas worldCanvas;
    [Header("Animasyon Ayarları")]
    [SerializeField] private float animationDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float curveHeight = 50f;
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
    }
    public void PlayResourceCollectAnimationToTarget(Vector3 position, Sprite resourceSprite, int amount, RectTransform target, System.Action onCompleteCallback = null)
    {
        if (resourceCollectAnimPrefab == null || target == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(position + Vector3.up * 0.5f);
        GameObject animObj = Instantiate(resourceCollectAnimPrefab, screenPos, Quaternion.identity, worldCanvas.transform);
        SetupAnimObject(animObj, resourceSprite, amount);
        AnimateToTarget(animObj, screenPos, target, onCompleteCallback);
    }
    private void SetupAnimObject(GameObject animObj, Sprite resourceSprite, int amount)
    {
        Image resourceImage = animObj.transform.GetChild(0).GetComponent<Image>();
        if (resourceImage != null && resourceSprite != null)
        {
            resourceImage.sprite = resourceSprite;
            resourceImage.preserveAspect = true;
        }
        TMP_Text tmpText = animObj.transform.GetChild(1).GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = "+" + amount.ToString();
        }
    }
    private void AnimateToTarget(GameObject animObj, Vector3 start, RectTransform target, System.Action onCompleteCallback = null)
    {
        Vector3 targetPos = target.position;
        Vector3[] path = new Vector3[3];
        path[0] = start;
        path[2] = targetPos;
        float midX = (start.x + targetPos.x) * 0.5f + Random.Range(-50f, 50f);
        float midY = Mathf.Min(start.y, targetPos.y) - curveHeight;
        path[1] = new Vector3(midX, midY, 0);
        Sequence seq = DOTween.Sequence();
        seq.SetAutoKill(true).SetLink(animObj);
        seq.Append(animObj.transform.DOPath(path, animationDuration, PathType.CatmullRom).SetEase(Ease.OutQuad));
        seq.AppendCallback(() =>
        {
            target.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 5, 0.5f);
        });
        CanvasGroup canvasGroup = animObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0, fadeOutDuration));
        }
        else
        {
            Graphic[] graphics = animObj.GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    seq.Join(graphic.DOFade(0, fadeOutDuration));
                }
            }
        }
        seq.OnComplete(() =>
        {
            if (animObj != null) Destroy(animObj);
            onCompleteCallback?.Invoke();
        });
    }
}
