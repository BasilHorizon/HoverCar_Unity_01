using UnityEngine;

namespace HoverCar.Environment
{
    /// <summary>
    /// Centralised place for systems to query the world up vector that hovercrafts should align to.
    /// </summary>
    public class GravityRegistry : MonoBehaviour
    {
        private static GravityRegistry _instance;

        [SerializeField]
        [Tooltip("Optional transform whose up axis defines the expected up direction.")]
        private Transform _upReference;

        [SerializeField]
        [Tooltip("Fallback up vector when no reference transform is provided.")]
        private Vector3 _expectedUp = Vector3.up;

        /// <summary>
        /// Global access to the active registry instance.
        /// </summary>
        public static GravityRegistry Instance => _instance;

        /// <summary>
        /// The up vector hovercrafts should align to.
        /// </summary>
        public Vector3 ExpectedUp
        {
            get
            {
                if (_upReference != null)
                {
                    return _upReference.up.sqrMagnitude > Mathf.Epsilon
                        ? _upReference.up.normalized
                        : Vector3.up;
                }

                if (_expectedUp.sqrMagnitude < Mathf.Epsilon)
                {
                    return Vector3.up;
                }

                return _expectedUp.normalized;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Multiple gravity registries detected. The newest instance will be used.");
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Updates the expected up vector with a custom direction.
        /// </summary>
        public void SetExpectedUp(Vector3 worldUp)
        {
            _upReference = null;
            _expectedUp = worldUp.normalized;
        }

        /// <summary>
        /// Uses the provided transform's up axis as the world up reference.
        /// </summary>
        public void RegisterUpReference(Transform reference)
        {
            _upReference = reference;
        }

        /// <summary>
        /// Removes the up reference if it matches the current one.
        /// </summary>
        public void UnregisterUpReference(Transform reference)
        {
            if (_upReference == reference)
            {
                _upReference = null;
            }
        }
    }
}
