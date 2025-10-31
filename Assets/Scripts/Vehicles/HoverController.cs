using UnityEngine;

namespace Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public class HoverController : MonoBehaviour
    {
        [System.Serializable]
        public class PIDController
        {
            [Tooltip("Proportional gain applied to the error vector.")]
            public float kp = 12f;
            [Tooltip("Integral gain applied to the accumulated error.")]
            public float ki = 0.4f;
            [Tooltip("Derivative gain applied to the error derivative.")]
            public float kd = 4f;

            private Vector3 _integral;
            private Vector3 _lastError;
            private bool _firstUpdate = true;

            public void Reset()
            {
                _integral = Vector3.zero;
                _lastError = Vector3.zero;
                _firstUpdate = true;
            }

            public Vector3 Update(Vector3 error, float deltaTime)
            {
                _integral += error * deltaTime;
                Vector3 derivative;

                if (_firstUpdate)
                {
                    derivative = Vector3.zero;
                    _firstUpdate = false;
                }
                else
                {
                    derivative = (error - _lastError) / Mathf.Max(deltaTime, Mathf.Epsilon);
                }

                _lastError = error;
                return kp * error + ki * _integral + kd * derivative;
            }
        }

        [Header("Movement")]
        [SerializeField] private float forwardAcceleration = 30f;
        [SerializeField] private float strafeAcceleration = 25f;
        [SerializeField] private float verticalAcceleration = 35f;
        [SerializeField] private float boostDamp = 2f;
        [SerializeField] private float maxSpeed = 45f;

        [Header("Steering")]
        [SerializeField] private float yawRate = 120f;
        [SerializeField] private float pitchRate = 90f;
        [SerializeField] private float rollRate = 110f;
        [SerializeField] private PIDController orientationPid = new PIDController();

        [Header("Camera")] 
        [SerializeField] private Transform orientationVisual;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = false;

        private Rigidbody _body;
        private Quaternion _targetRotation;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _targetRotation = _body.rotation;
        }

        private void OnEnable()
        {
            orientationPid.Reset();
        }

        private void Update()
        {
            UpdateTargetRotation();
            UpdateOrientationVisual();
        }

        private void FixedUpdate()
        {
            ApplyOrientationControl();
            ApplyTranslationalForces();
            ApplyStabilization();
        }

        private void UpdateTargetRotation()
        {
            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > Mathf.Epsilon
                ? -Physics.gravity.normalized
                : Vector3.up;

            // Align the target rotation so its up axis matches gravity.
            Quaternion gravityAlignment = Quaternion.FromToRotation(_targetRotation * Vector3.up, gravityDirection);
            _targetRotation = gravityAlignment * _targetRotation;

            float deltaTime = Time.deltaTime;
            float yawInput = Input.GetAxis("Mouse X") * yawRate * deltaTime;
            float pitchInput = -Input.GetAxis("Mouse Y") * pitchRate * deltaTime;
            float rollInput = GetRollInput() * rollRate * deltaTime;

            if (!Mathf.Approximately(yawInput, 0f))
            {
                _targetRotation = Quaternion.AngleAxis(yawInput, gravityDirection) * _targetRotation;
            }

            if (!Mathf.Approximately(pitchInput, 0f))
            {
                Vector3 pitchAxis = _targetRotation * Vector3.right;
                _targetRotation = Quaternion.AngleAxis(pitchInput, pitchAxis) * _targetRotation;
            }

            if (!Mathf.Approximately(rollInput, 0f))
            {
                Vector3 rollAxis = _targetRotation * Vector3.forward;
                _targetRotation = Quaternion.AngleAxis(rollInput, rollAxis) * _targetRotation;
            }
        }

        private float GetRollInput()
        {
            float roll = 0f;
            if (Input.GetKey(KeyCode.Q))
            {
                roll -= 1f;
            }

            if (Input.GetKey(KeyCode.E))
            {
                roll += 1f;
            }

            return roll;
        }

        private void ApplyOrientationControl()
        {
            Quaternion currentRotation = _body.rotation;
            Quaternion deltaRotation = _targetRotation * Quaternion.Inverse(currentRotation);
            deltaRotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);

            if (float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
            {
                return;
            }

            if (angleDegrees > 180f)
            {
                angleDegrees -= 360f;
            }

            Vector3 error = axis * (angleDegrees * Mathf.Deg2Rad);
            Vector3 torque = orientationPid.Update(error, Time.fixedDeltaTime);
            _body.AddTorque(torque, ForceMode.Acceleration);
        }

        private void ApplyTranslationalForces()
        {
            float forwardInput = Input.GetAxis("Vertical");
            float strafeInput = Input.GetAxis("Horizontal");
            float verticalInput = GetVerticalInput();

            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > Mathf.Epsilon
                ? -Physics.gravity.normalized
                : Vector3.up;

            Vector3 desiredAcceleration =
                transform.forward * forwardInput * forwardAcceleration +
                transform.right * strafeInput * strafeAcceleration +
                gravityDirection * verticalInput * verticalAcceleration;

            _body.AddForce(desiredAcceleration, ForceMode.Acceleration);

            if (_body.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                _body.velocity = Vector3.ClampMagnitude(_body.velocity, maxSpeed);
            }
        }

        private float GetVerticalInput()
        {
            float vertical = 0f;
            if (Input.GetKey(KeyCode.Space))
            {
                vertical += 1f;
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                vertical -= 1f;
            }

            return Mathf.Clamp(vertical, -1f, 1f);
        }

        private void ApplyStabilization()
        {
            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > Mathf.Epsilon
                ? -Physics.gravity.normalized
                : Vector3.up;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(_body.velocity, gravityDirection);
            Vector3 dampingForce = -projectedVelocity * boostDamp;
            _body.AddForce(dampingForce, ForceMode.Acceleration);
        }

        private void UpdateOrientationVisual()
        {
            if (orientationVisual == null)
            {
                return;
            }

            orientationVisual.rotation = Quaternion.Slerp(orientationVisual.rotation, _targetRotation, 8f * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug || _body == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + _body.velocity);

            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > Mathf.Epsilon
                ? -Physics.gravity.normalized
                : Vector3.up;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + gravityDirection * 5f);
        }
    }
}
