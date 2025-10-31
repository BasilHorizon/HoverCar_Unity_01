using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.Gravity
{
    [DisallowMultipleComponent]
    public sealed class GravitySource : MonoBehaviour
    {
        [SerializeField]
        private GravityProfile profile;

        [SerializeField]
        private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.4f);

        private readonly List<GravityReceiver> receivers = new List<GravityReceiver>();

        public GravityProfile Profile => profile;

        private void OnEnable()
        {
            if (GravityRegistry.TryGetInstance(out var registry))
            {
                registry.RegisterSource(this);
            }
        }

        private void OnDisable()
        {
            if (GravityRegistry.TryGetInstance(out var registry))
            {
                registry.UnregisterSource(this);
            }
            receivers.Clear();
        }

        public bool TrySample(Vector3 worldPosition, out GravitySample sample)
        {
            if (profile != null && isActiveAndEnabled)
            {
                return profile.TrySample(worldPosition, transform, out sample);
            }

            sample = default;
            return false;
        }

        public Vector3 ExpectedUp(Vector3 worldPosition)
        {
            if (TrySample(worldPosition, out var sample))
            {
                if (sample.Up.sqrMagnitude > Mathf.Epsilon)
                {
                    return sample.Up.normalized;
                }

                if (sample.Force.sqrMagnitude > Mathf.Epsilon)
                {
                    return -sample.Force.normalized;
                }
            }

            return transform.up;
        }

        internal void RegisterReceiver(GravityReceiver receiver)
        {
            if (!receivers.Contains(receiver))
            {
                receivers.Add(receiver);
            }
        }

        internal void UnregisterReceiver(GravityReceiver receiver)
        {
            receivers.Remove(receiver);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawGizmosInternal(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmosInternal(true);
        }

        private void DrawGizmosInternal(bool selected)
        {
            if (!enabled)
            {
                return;
            }

            var previousColor = Gizmos.color;
            profile?.DrawGizmos(transform);

            Gizmos.color = gizmoColor;

            if (!selected)
            {
                Gizmos.color = previousColor;
                return;
            }

            foreach (var receiver in receivers)
            {
                if (receiver == null)
                {
                    continue;
                }

                var origin = receiver.transform.position;
                if (TrySample(origin, out var sample))
                {
                    Gizmos.color = new Color(0.1f, 0.4f, 1f, 1f);
                    var force = sample.Force;
                    if (force.sqrMagnitude > Mathf.Epsilon)
                    {
                        var direction = force.normalized;
                        var magnitude = Mathf.Clamp(force.magnitude, 0.5f, 5f);
                        Gizmos.DrawLine(origin, origin + direction * magnitude * 0.5f);
                        Gizmos.DrawSphere(origin + direction * magnitude * 0.5f, 0.05f);
                    }

                    if (sample.Up.sqrMagnitude > Mathf.Epsilon)
                    {
                        Gizmos.color = new Color(0.1f, 1f, 0.4f, 1f);
                        var upDir = sample.Up.normalized;
                        Gizmos.DrawLine(origin, origin + upDir * 1.5f);
                    }
                }
            }

            Gizmos.color = previousColor;
        }
#endif
    }
}
