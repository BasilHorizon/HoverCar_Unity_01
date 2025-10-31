using UnityEngine;

namespace HoverCar.Gravity
{
    /// <summary>
    /// A sampled contribution from a gravity source profile.
    /// </summary>
    public readonly struct GravitySample
    {
        public readonly Vector3 Force;
        public readonly Vector3 Up;
        public readonly float Weight;

        public GravitySample(Vector3 force, Vector3 up, float weight)
        {
            Force = force;
            Up = up;
            Weight = weight;
        }

        public bool IsValid => Weight > 0f && (Force.sqrMagnitude > 0f || Up.sqrMagnitude > 0f);
    }

    /// <summary>
    /// Base class for profile assets that define how a gravity source behaves.
    /// </summary>
    public abstract class GravityProfile : ScriptableObject
    {
        [SerializeField, Min(0f)]
        private float weight = 1f;

        /// <summary>
        /// Weight applied when combining multiple sources.
        /// </summary>
        public float Weight => Mathf.Max(0f, weight);

        /// <summary>
        /// Samples the gravity field at a world position.
        /// </summary>
        public abstract bool TrySample(Vector3 worldPosition, Transform sourceTransform, out GravitySample sample);

        /// <summary>
        /// Draws debug gizmos in the Scene view.
        /// </summary>
        public virtual void DrawGizmos(Transform sourceTransform)
        {
        }
    }

    /// <summary>
    /// A simple radial gravity profile that attracts or repels towards its origin.
    /// </summary>
    [CreateAssetMenu(menuName = "Gravity/Radial Profile", fileName = "RadialGravityProfile")]
    public sealed class RadialGravityProfile : GravityProfile
    {
        private enum Mode
        {
            Attraction,
            Repulsion
        }

        [SerializeField]
        private Mode mode = Mode.Attraction;

        [SerializeField, Min(0f)]
        private float radius = 25f;

        [SerializeField]
        private float gravity = 9.81f;

        [SerializeField]
        private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [SerializeField]
        private bool useSurfaceNormalAsUp = true;

        public float Radius => radius;

        public override bool TrySample(Vector3 worldPosition, Transform sourceTransform, out GravitySample sample)
        {
            var origin = sourceTransform.position;
            var offset = worldPosition - origin;
            var distance = offset.magnitude;

            if (radius > 0f && distance > radius)
            {
                sample = default;
                return false;
            }

            var direction = distance > Mathf.Epsilon ? offset / Mathf.Max(distance, 1e-3f) : sourceTransform.up;
            var normalizedDistance = radius > 0f ? Mathf.Clamp01(distance / radius) : 0f;
            var strength = gravity * falloff.Evaluate(normalizedDistance);
            if (Mathf.Approximately(strength, 0f))
            {
                sample = default;
                return false;
            }

            Vector3 force;
            Vector3 up;
            if (mode == Mode.Attraction)
            {
                force = -direction * strength;
                up = useSurfaceNormalAsUp ? direction : -force.normalized;
            }
            else
            {
                force = direction * strength;
                up = useSurfaceNormalAsUp ? -direction : -force.normalized;
            }

            sample = new GravitySample(force, up, Weight);
            return true;
        }

        public override void DrawGizmos(Transform sourceTransform)
        {
            if (radius <= 0f)
            {
                return;
            }

            var previous = Gizmos.color;
            Gizmos.color = mode == Mode.Attraction ? new Color(0.3f, 0.7f, 1f, 0.25f) : new Color(1f, 0.5f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(sourceTransform.position, radius);
            Gizmos.color = previous;
        }
    }
}
