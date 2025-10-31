using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[CreateAssetMenu(fileName = "TrackLoopSpline", menuName = "Track/Spline Asset", order = 0)]
public class TrackSplineAsset : ScriptableObject
{
    [SerializeField]
    bool closed = true;

    [SerializeField]
    SerializableKnot[] knots = Array.Empty<SerializableKnot>();

    public bool Closed => closed;

    public SerializableKnot[] Knots => knots;

    public void ApplyTo(Spline spline)
    {
        if (spline == null)
        {
            throw new ArgumentNullException(nameof(spline));
        }

        spline.Clear();

        if (knots == null || knots.Length == 0)
        {
            return;
        }

        foreach (var knot in knots)
        {
            spline.Add(knot.ToBezierKnot());
        }

        spline.Closed = closed;
    }

    [Serializable]
    public struct SerializableKnot
    {
        public Vector3 position;
        public Vector3 tangentIn;
        public Vector3 tangentOut;
        public Quaternion rotation;

        public BezierKnot ToBezierKnot()
        {
            return new BezierKnot(
                new float3(position.x, position.y, position.z),
                new float3(tangentIn.x, tangentIn.y, tangentIn.z),
                new float3(tangentOut.x, tangentOut.y, tangentOut.z),
                new quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
        }
    }
}
