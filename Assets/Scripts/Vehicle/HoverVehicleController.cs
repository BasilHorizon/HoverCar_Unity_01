using System;
using UnityEngine;

namespace HoverCar.Gameplay
{
    /// <summary>
    /// Simplified hover vehicle controller that exposes runtime toggles for gameplay and debug UI.
    /// The controller focuses on surfacing state for the HUD rather than providing a full simulation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class HoverVehicleController : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField]
        private float targetHoverHeight = 2.0f;

        [SerializeField]
        private float hoverForce = 9.81f;

        [SerializeField]
        private float alignmentBoostMultiplier = 2.0f;

        [SerializeField]
        private bool alignmentBoostEnabled = true;

        [Header("Stability")]
        [SerializeField]
        private float stabilityStrength = 4.5f;

        [SerializeField]
        private bool autoLevelEnabled = true;

        private Rigidbody cachedRigidbody;

        public event Action<bool> AlignmentBoostChanged;
        public event Action<bool> AutoLevelChanged;

        /// <summary>
        /// Returns the rigidbody used by the hovercraft.
        /// </summary>
        public Rigidbody Body
        {
            get
            {
                if (cachedRigidbody == null)
                {
                    cachedRigidbody = GetComponent<Rigidbody>();
                }

                return cachedRigidbody;
            }
        }

        /// <summary>
        /// Current magnitude of the applied hover force during the last FixedUpdate.
        /// </summary>
        public Vector3 LastAppliedHoverForce { get; private set; }

        /// <summary>
        /// Current hover height target.
        /// </summary>
        public float TargetHoverHeight => targetHoverHeight;

        /// <summary>
        /// The most recent linear speed of the hovercraft in metres per second.
        /// </summary>
        public float CurrentSpeed => Body != null ? Body.velocity.magnitude : 0f;

        /// <summary>
        /// Indicates whether the alignment boost is currently engaged.
        /// </summary>
        public bool AlignmentBoostEnabled => alignmentBoostEnabled;

        /// <summary>
        /// Indicates whether the auto leveling stabilizer is active.
        /// </summary>
        public bool AutoLevelEnabled => autoLevelEnabled;

        private void Reset()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (Body == null)
            {
                return;
            }

            ApplyHoverForce();
            ApplyStability();
        }

        private void ApplyHoverForce()
        {
            var desiredForce = hoverForce;
            if (alignmentBoostEnabled)
            {
                desiredForce *= alignmentBoostMultiplier;
            }

            LastAppliedHoverForce = Vector3.up * desiredForce;
            Body.AddForce(LastAppliedHoverForce, ForceMode.Acceleration);
        }

        private void ApplyStability()
        {
            if (!autoLevelEnabled)
            {
                return;
            }

            var uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            var correctiveTorque = uprightRotation.eulerAngles;
            correctiveTorque.x = Mathf.DeltaAngle(0f, correctiveTorque.x);
            correctiveTorque.y = 0f;
            correctiveTorque.z = Mathf.DeltaAngle(0f, correctiveTorque.z);

            var torque = -new Vector3(correctiveTorque.x, correctiveTorque.y, correctiveTorque.z) * stabilityStrength;
            Body.AddTorque(torque, ForceMode.Acceleration);
        }

        public void ToggleAlignmentBoost()
        {
            SetAlignmentBoostEnabled(!alignmentBoostEnabled);
        }

        public void SetAlignmentBoostEnabled(bool enabled)
        {
            if (alignmentBoostEnabled == enabled)
            {
                return;
            }

            alignmentBoostEnabled = enabled;
            AlignmentBoostChanged?.Invoke(alignmentBoostEnabled);
        }

        public void ToggleAutoLevel()
        {
            SetAutoLevelEnabled(!autoLevelEnabled);
        }

        public void SetAutoLevelEnabled(bool enabled)
        {
            if (autoLevelEnabled == enabled)
            {
                return;
            }

            autoLevelEnabled = enabled;
            AutoLevelChanged?.Invoke(autoLevelEnabled);
        }
    }
}
