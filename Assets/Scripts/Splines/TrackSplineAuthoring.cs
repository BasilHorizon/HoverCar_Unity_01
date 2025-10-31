using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class TrackSplineAuthoring : MonoBehaviour
{
    [SerializeField]
    TrackSplineAsset splineAsset;

    [SerializeField]
    bool alignTransformToWorldUp = true;

    public TrackSplineAsset SplineAsset
    {
        get => splineAsset;
        set
        {
            splineAsset = value;
            ApplyAsset();
        }
    }

    void Reset()
    {
        EnsureContainer();
        ApplyAsset();
    }

    void OnEnable()
    {
        EnsureContainer();
        ApplyAsset();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureContainer();
        ApplyAsset();
    }
#endif

    void EnsureContainer()
    {
        if (!TryGetComponent<SplineContainer>(out _))
        {
            gameObject.AddComponent<SplineContainer>();
        }
    }

    void ApplyAsset()
    {
        if (!TryGetComponent(out SplineContainer container))
        {
            return;
        }

        var splines = container.Splines;
        if (splines.Count == 0)
        {
            splines.Add(new Spline());
        }

        var spline = container.Spline;

        if (splineAsset == null)
        {
            spline.Clear();
            return;
        }

        splineAsset.ApplyTo(spline);

        if (alignTransformToWorldUp)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
