using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.Tracks
{
    /// <summary>
    /// Lightweight Catmull-Rom spline implementation used by the AI to navigate the track.
    /// </summary>
    [ExecuteAlways]
    public class TrackSpline : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Control points that define the spline in local space.")]
        private List<Vector3> _controlPoints = new()
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 30f),
            new Vector3(30f, 0f, 60f),
            new Vector3(60f, 0f, 30f),
            new Vector3(30f, 0f, 0f),
        };

        [SerializeField]
        [Tooltip("True if the spline loops back to the first control point.")]
        private bool _loop = true;

        [SerializeField]
        [Range(2, 64)]
        [Tooltip("Number of cached samples per segment.")]
        private int _samplesPerSegment = 16;

        private readonly List<Vector3> _cachedSamples = new();
        private readonly List<float> _sampleDistances = new();
        private float _totalLength;
        private bool _dirty = true;

        public float Length
        {
            get
            {
                EnsureCache();
                return _totalLength;
            }
        }

        public bool Loop => _loop;

        private void Reset()
        {
            EnsureDefaultPoints();
            _dirty = true;
        }

        private void Awake()
        {
            EnsureCache();
        }

        private void OnEnable()
        {
            EnsureCache();
        }

        private void OnValidate()
        {
            EnsureDefaultPoints();
            _dirty = true;
        }

        /// <summary>
        /// Returns a world position evaluated at the provided distance along the spline.
        /// </summary>
        public Vector3 EvaluatePosition(float distance)
        {
            EnsureCache();

            if (_cachedSamples.Count == 0)
            {
                return transform.position;
            }

            if (_totalLength <= Mathf.Epsilon)
            {
                return transform.TransformPoint(_cachedSamples[0]);
            }

            if (_loop)
            {
                distance = Mathf.Repeat(distance, _totalLength);
            }
            else
            {
                distance = Mathf.Clamp(distance, 0f, _totalLength);
            }

            int lower = 0;
            int upper = _sampleDistances.Count - 1;
            while (upper - lower > 1)
            {
                int mid = (lower + upper) >> 1;
                if (_sampleDistances[mid] < distance)
                {
                    lower = mid;
                }
                else
                {
                    upper = mid;
                }
            }

            float segmentLength = _sampleDistances[upper] - _sampleDistances[lower];
            float t = segmentLength > Mathf.Epsilon
                ? (distance - _sampleDistances[lower]) / segmentLength
                : 0f;

            Vector3 local = Vector3.Lerp(_cachedSamples[lower], _cachedSamples[upper], Mathf.Clamp01(t));
            return transform.TransformPoint(local);
        }

        /// <summary>
        /// Returns a world tangent vector evaluated at the provided distance.
        /// </summary>
        public Vector3 EvaluateTangent(float distance)
        {
            EnsureCache();

            if (_cachedSamples.Count < 2)
            {
                return transform.forward;
            }

            const float offset = 0.5f;
            Vector3 position = EvaluatePosition(distance);
            Vector3 next = EvaluatePosition(distance + offset);
            Vector3 tangent = next - position;
            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return transform.forward;
            }

            return tangent.normalized;
        }

        /// <summary>
        /// Advances a distance value taking the spline's looping behaviour into account.
        /// </summary>
        public float AdvanceDistance(float distance, float speed, float deltaTime)
        {
            EnsureCache();

            float newDistance = distance + Mathf.Max(0f, speed) * deltaTime;
            if (_loop && _totalLength > Mathf.Epsilon)
            {
                return Mathf.Repeat(newDistance, _totalLength);
            }

            return Mathf.Clamp(newDistance, 0f, _totalLength);
        }

        /// <summary>
        /// Returns the distance along the spline closest to the provided world position.
        /// </summary>
        public float FindClosestDistance(Vector3 worldPosition)
        {
            EnsureCache();

            if (_cachedSamples.Count == 0)
            {
                return 0f;
            }

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float bestDistance = float.MaxValue;
            int bestIndex = 0;

            for (int i = 0; i < _cachedSamples.Count; i++)
            {
                float sqr = (local - _cachedSamples[i]).sqrMagnitude;
                if (sqr < bestDistance)
                {
                    bestDistance = sqr;
                    bestIndex = i;
                }
            }

            return _sampleDistances[Mathf.Clamp(bestIndex, 0, _sampleDistances.Count - 1)];
        }

        private void EnsureCache()
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;
            RebuildCache();
        }

        private void RebuildCache()
        {
            _cachedSamples.Clear();
            _sampleDistances.Clear();

            if (_controlPoints.Count < 4)
            {
                _totalLength = 0f;
                if (_controlPoints.Count > 0)
                {
                    _cachedSamples.Add(_controlPoints[0]);
                    _sampleDistances.Add(0f);
                }
                return;
            }

            int segmentCount = _loop ? _controlPoints.Count : _controlPoints.Count - 3;
            Vector3 previous = GetPointOnSegment(0, 0f);

            _cachedSamples.Add(previous);
            _sampleDistances.Add(0f);

            float cumulative = 0f;
            int steps = Mathf.Max(2, _samplesPerSegment);

            for (int segment = 0; segment < segmentCount; segment++)
            {
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector3 sample = GetPointOnSegment(segment, t);
                    cumulative += Vector3.Distance(previous, sample);
                    _cachedSamples.Add(sample);
                    _sampleDistances.Add(cumulative);
                    previous = sample;
                }
            }

            _totalLength = cumulative;
        }

        private Vector3 GetPointOnSegment(int segment, float t)
        {
            if (_loop)
            {
                int count = _controlPoints.Count;
                Vector3 p0 = _controlPoints[RepeatIndex(segment - 1, count)];
                Vector3 p1 = _controlPoints[RepeatIndex(segment, count)];
                Vector3 p2 = _controlPoints[RepeatIndex(segment + 1, count)];
                Vector3 p3 = _controlPoints[RepeatIndex(segment + 2, count)];
                return CatmullRom(p0, p1, p2, p3, t);
            }

            segment = Mathf.Clamp(segment, 0, _controlPoints.Count - 4);
            Vector3 a = _controlPoints[segment];
            Vector3 b = _controlPoints[segment + 1];
            Vector3 c = _controlPoints[segment + 2];
            Vector3 d = _controlPoints[segment + 3];
            return CatmullRom(a, b, c, d, t);
        }

        private static int RepeatIndex(int index, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            int value = index % count;
            return value < 0 ? value + count : value;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private void EnsureDefaultPoints()
        {
            if (_controlPoints == null)
            {
                _controlPoints = new List<Vector3>();
            }

            if (_controlPoints.Count >= 4)
            {
                return;
            }

            _controlPoints.Clear();
            _controlPoints.Add(new Vector3(0f, 0f, 0f));
            _controlPoints.Add(new Vector3(0f, 0f, 30f));
            _controlPoints.Add(new Vector3(30f, 0f, 60f));
            _controlPoints.Add(new Vector3(60f, 0f, 30f));
            _controlPoints.Add(new Vector3(30f, 0f, 0f));
        }
    }
}
