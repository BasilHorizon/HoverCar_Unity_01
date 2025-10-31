using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.StationAssembly
{
    [ExecuteAlways]
    public class SplineSegment : MonoBehaviour
    {
        [SerializeField]
        private List<Vector3> controlPoints = new();

        public IReadOnlyList<Vector3> ControlPoints => controlPoints;

        public void SetControlPoints(IEnumerable<Vector3> points)
        {
            controlPoints.Clear();
            controlPoints.AddRange(points);
        }

        private void OnDrawGizmos()
        {
            if (controlPoints.Count < 2)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 previous = transform.TransformPoint(controlPoints[0]);
            for (int i = 1; i < controlPoints.Count; i++)
            {
                Vector3 current = transform.TransformPoint(controlPoints[i]);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
