using System;
using UnityEngine;

namespace HoverCar.Gameplay
{
    /// <summary>
    /// Runtime controllable gravity system used by the debug HUD to expose state and toggles.
    /// </summary>
    public class GravityController : MonoBehaviour
    {
        [SerializeField]
        private bool useCustomGravity = true;

        [SerializeField]
        private Vector3 defaultUnityGravity = new Vector3(0f, -9.81f, 0f);

        [SerializeField]
        private Vector3 customGravity = new Vector3(0f, -12f, 0f);

        public event Action<bool> CustomGravityChanged;

        /// <summary>
        /// Returns the currently active gravity vector.
        /// </summary>
        public Vector3 CurrentGravity => useCustomGravity ? customGravity : defaultUnityGravity;

        /// <summary>
        /// Gets a value indicating whether the controller is using a custom gravity vector.
        /// </summary>
        public bool UseCustomGravity => useCustomGravity;

        private void OnEnable()
        {
            ApplyGravity();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplyGravity();
            }
        }

        public void ToggleCustomGravity()
        {
            SetCustomGravityEnabled(!useCustomGravity);
        }

        public void SetCustomGravityEnabled(bool enabled)
        {
            if (useCustomGravity == enabled)
            {
                return;
            }

            useCustomGravity = enabled;
            ApplyGravity();
            CustomGravityChanged?.Invoke(useCustomGravity);
        }

        public void SetCustomGravity(Vector3 gravity)
        {
            customGravity = gravity;
            ApplyGravity();
        }

        private void ApplyGravity()
        {
            Physics.gravity = CurrentGravity;
        }
    }
}
