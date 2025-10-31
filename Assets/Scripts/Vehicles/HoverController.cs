using HoverCar.Environment;
using UnityEngine;

namespace HoverCar.Vehicles
{
    public interface IHoverInputSource
    {
        float Throttle { get; }
        float Steering { get; }
        Quaternion DesiredOrientation { get; }
    }

    /// <summary>
    /// Simple hover vehicle controller that applies forces based on input commands.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class HoverController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Component that provides throttle and steering values.")]
        private MonoBehaviour _inputProvider;

        [SerializeField]
        private float _thrustAcceleration = 40f;

        [SerializeField]
        private float _maxSpeed = 60f;

        [SerializeField]
        private float _steeringTorque = 8f;

        [SerializeField]
        private float _bankTorque = 4f;

        [SerializeField]
        private float _orientationResponsiveness = 5f;

        private IHoverInputSource _input;
        private Rigidbody _body;
        private GravityRegistry _gravityRegistry;

        /// <summary>
        /// Current forward speed of the craft.
        /// </summary>
        public float CurrentSpeed => Vector3.Dot(_body.velocity, transform.forward);

        public void SetInputSource(IHoverInputSource source)
        {
            _input = source;
            _inputProvider = source as MonoBehaviour;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            CacheInputProvider();
            _gravityRegistry = GravityRegistry.Instance ?? FindObjectOfType<GravityRegistry>();
        }

        private void OnValidate()
        {
            if (_inputProvider != null && !(_inputProvider is IHoverInputSource))
            {
                Debug.LogWarning($"{_inputProvider.name} does not implement {nameof(IHoverInputSource)} and cannot drive the hover controller.", this);
                _inputProvider = null;
            }
        }

        private void FixedUpdate()
        {
            if (_input == null)
            {
                CacheInputProvider();
                if (_input == null)
                {
                    return;
                }
            }

            Vector3 expectedUp = _gravityRegistry != null ? _gravityRegistry.ExpectedUp : Vector3.up;
            float deltaTime = Time.fixedDeltaTime;

            Quaternion alignUp = Quaternion.FromToRotation(transform.up, expectedUp) * _body.rotation;
            Quaternion desiredOrientation = _input.DesiredOrientation;
            if (desiredOrientation == Quaternion.identity)
            {
                desiredOrientation = Quaternion.LookRotation(transform.forward, expectedUp);
            }

            Quaternion targetRotation = Quaternion.Slerp(alignUp, desiredOrientation, 0.65f);
            _body.MoveRotation(Quaternion.Slerp(_body.rotation, targetRotation, _orientationResponsiveness * deltaTime));

            float throttle = Mathf.Clamp(_input.Throttle, -1f, 1f);
            float steering = Mathf.Clamp(_input.Steering, -1f, 1f);

            Vector3 forward = transform.forward;
            _body.AddForce(forward * throttle * _thrustAcceleration, ForceMode.Acceleration);

            if (_body.velocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            {
                _body.velocity = Vector3.ClampMagnitude(_body.velocity, _maxSpeed);
            }

            _body.AddTorque(expectedUp * steering * _steeringTorque, ForceMode.Acceleration);

            Vector3 lateralVelocity = Vector3.ProjectOnPlane(_body.velocity, expectedUp);
            if (lateralVelocity.sqrMagnitude > Mathf.Epsilon)
            {
                Vector3 sideways = Vector3.Cross(expectedUp, forward).normalized;
                float bank = Mathf.Clamp(Vector3.Dot(lateralVelocity.normalized, sideways), -1f, 1f);
                _body.AddTorque(forward * -bank * _bankTorque, ForceMode.Acceleration);
            }
        }

        private void CacheInputProvider()
        {
            if (_inputProvider != null)
            {
                _input = _inputProvider as IHoverInputSource;
            }
        }
    }
}
