using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;
public class UIPositioner : MonoBehaviour
{
    private Vector3 offset = new Vector3(0, -2f, 0);
    private ReactiveProperty<Transform> targetTransform = new ReactiveProperty<Transform>();
    private CompositeDisposable disposables = new CompositeDisposable();
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        this.LateUpdateAsObservable()
            .Subscribe(_ => UpdatePositionAndRotation())
            .AddTo(disposables);
        targetTransform
            .Where(target => target != null)
            .Subscribe(_ => UpdatePositionAndRotation())
            .AddTo(disposables);
    }
    public void SetTarget(Transform target)
    {
        targetTransform.Value = target;
    }
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        UpdatePositionAndRotation();
    }
    private void UpdatePositionAndRotation()
    {
        if (targetTransform.Value != null)
        {
            transform.position = targetTransform.Value.position + offset;
            transform.rotation = mainCamera.transform.rotation;
        }
    }
    private void OnDestroy()
    {
        disposables.Dispose();
    }
}