using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.Gravity
{
    [DefaultExecutionOrder(-200)]
    public sealed class GravityRegistry : MonoBehaviour
    {
        private static GravityRegistry instance;

        private readonly HashSet<GravitySource> sources = new HashSet<GravitySource>();
        private readonly HashSet<GravityReceiver> receivers = new HashSet<GravityReceiver>();

        public static GravityRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GravityRegistry>();
                }

                return instance;
            }
        }

        public static bool TryGetInstance(out GravityRegistry registry)
        {
            registry = Instance;
            return registry != null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Multiple GravityRegistry instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            sources.Clear();
            receivers.Clear();
        }

        public void RegisterSource(GravitySource source)
        {
            if (source == null)
            {
                return;
            }

            if (sources.Add(source))
            {
                foreach (var receiver in receivers)
                {
                    source.RegisterReceiver(receiver);
                }
            }
        }

        public void UnregisterSource(GravitySource source)
        {
            if (source == null)
            {
                return;
            }

            if (sources.Remove(source))
            {
                foreach (var receiver in receivers)
                {
                    source.UnregisterReceiver(receiver);
                }
            }
        }

        public void RegisterReceiver(GravityReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }

            receivers.Add(receiver);

            foreach (var source in sources)
            {
                source?.RegisterReceiver(receiver);
            }
        }

        public void UnregisterReceiver(GravityReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }

            receivers.Remove(receiver);

            foreach (var source in sources)
            {
                source?.UnregisterReceiver(receiver);
            }
        }

        public bool TryGetGravity(Vector3 worldPosition, out Vector3 gravity, out Vector3 expectedUp)
        {
            var totalForce = Vector3.zero;
            var upAccumulator = Vector3.zero;
            float upWeight = 0f;
            var hasContribution = false;

            foreach (var source in sources)
            {
                if (source == null || !source.isActiveAndEnabled)
                {
                    continue;
                }

                if (!source.TrySample(worldPosition, out var sample))
                {
                    continue;
                }

                totalForce += sample.Force;
                hasContribution |= sample.Force.sqrMagnitude > Mathf.Epsilon;

                if (sample.Up.sqrMagnitude > Mathf.Epsilon && sample.Weight > 0f)
                {
                    upAccumulator += sample.Up.normalized * sample.Weight;
                    upWeight += sample.Weight;
                }
            }

            gravity = totalForce;

            if (upWeight > 0f)
            {
                expectedUp = upAccumulator.normalized;
            }
            else if (gravity.sqrMagnitude > Mathf.Epsilon)
            {
                expectedUp = -gravity.normalized;
            }
            else
            {
                expectedUp = Vector3.up;
            }

            return hasContribution || gravity.sqrMagnitude > Mathf.Epsilon;
        }

        public IReadOnlyCollection<GravitySource> Sources => sources;
        public IReadOnlyCollection<GravityReceiver> Receivers => receivers;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.7f, 0.6f);

            foreach (var receiver in receivers)
            {
                if (receiver == null)
                {
                    continue;
                }

                var origin = receiver.transform.position;
                var gravityVector = receiver.LastGravity;
                if (gravityVector.sqrMagnitude > Mathf.Epsilon)
                {
                    var direction = gravityVector.normalized;
                    var magnitude = Mathf.Clamp(gravityVector.magnitude, 0.5f, 5f);
                    Gizmos.DrawLine(origin, origin + direction * magnitude * 0.35f);
                    Gizmos.DrawSphere(origin + direction * magnitude * 0.35f, 0.04f);
                }

                var upVector = receiver.LastExpectedUp;
                if (upVector.sqrMagnitude > Mathf.Epsilon)
                {
                    Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.75f);
                    Gizmos.DrawLine(origin, origin + upVector.normalized * 1.2f);
                    Gizmos.color = new Color(0.2f, 0.9f, 0.7f, 0.6f);
                }
            }
        }
#endif
    }
}
