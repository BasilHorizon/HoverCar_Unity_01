using UnityEngine;

namespace HoverCar.Gravity
{
    [DisallowMultipleComponent]
    public sealed class GravityReceiver : MonoBehaviour
    {
        [SerializeField]
        private bool autoSample = true;

        [SerializeField]
        private float smoothing = 10f;

        private Vector3 lastGravity = Vector3.zero;
        private Vector3 lastExpectedUp = Vector3.up;
        private bool hasGravity;

        public Vector3 LastGravity => lastGravity;
        public Vector3 LastExpectedUp => lastExpectedUp;
        public bool HasGravity => hasGravity;

        private void OnEnable()
        {
            if (GravityRegistry.TryGetInstance(out var registry))
            {
                registry.RegisterReceiver(this);
            }

            SampleImmediate();
        }

        private void OnDisable()
        {
            if (GravityRegistry.TryGetInstance(out var registry))
            {
                registry.UnregisterReceiver(this);
            }
        }

        private void Update()
        {
            if (autoSample)
            {
                Sample(Time.deltaTime);
            }
        }

        public void SampleImmediate()
        {
            if (TryGetGravity(out var gravity, out var up))
            {
                lastGravity = gravity;
                lastExpectedUp = up;
                hasGravity = true;
            }
            else
            {
                lastGravity = Vector3.zero;
                lastExpectedUp = Vector3.up;
                hasGravity = false;
            }
        }

        public bool Sample(float deltaTime)
        {
            if (!TryGetGravity(out var gravity, out var up))
            {
                hasGravity = false;
                lastGravity = Vector3.Lerp(lastGravity, Vector3.zero, Mathf.Clamp01(deltaTime * smoothing));
                lastExpectedUp = Vector3.Lerp(lastExpectedUp, Vector3.up, Mathf.Clamp01(deltaTime * smoothing));
                return false;
            }

            hasGravity = true;
            var lerpFactor = smoothing > 0f ? Mathf.Clamp01(deltaTime * smoothing) : 1f;
            lastGravity = Vector3.Lerp(lastGravity, gravity, lerpFactor);
            lastExpectedUp = Vector3.Slerp(lastExpectedUp, up, lerpFactor);
            return true;
        }

        public bool TryGetGravity(out Vector3 gravity, out Vector3 expectedUp)
        {
            if (GravityRegistry.TryGetInstance(out var registry))
            {
                return registry.TryGetGravity(transform.position, out gravity, out expectedUp);
            }

            gravity = Vector3.zero;
            expectedUp = Vector3.up;
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                SampleImmediate();
            }

            Gizmos.color = hasGravity ? Color.cyan : new Color(0.4f, 0.4f, 0.4f, 0.75f);
            var origin = transform.position;
            if (lastGravity.sqrMagnitude > Mathf.Epsilon)
            {
                var dir = lastGravity.normalized;
                Gizmos.DrawLine(origin, origin + dir * Mathf.Clamp(lastGravity.magnitude, 0.5f, 3f) * 0.5f);
            }

            if (lastExpectedUp.sqrMagnitude > Mathf.Epsilon)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, origin + lastExpectedUp.normalized * 1.5f);
            }
        }
#endif
    }
}
