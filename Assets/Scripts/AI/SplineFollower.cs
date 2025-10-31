using HoverCar.Environment;
using HoverCar.Tracks;
using HoverCar.Vehicles;
using UnityEngine;

namespace HoverCar.AI
{
    /// <summary>
    /// Provides AI throttle and steering input by following a spline track with look-ahead steering.
    /// </summary>
    [RequireComponent(typeof(HoverController))]
    [RequireComponent(typeof(Rigidbody))]
    public class SplineFollower : MonoBehaviour, IHoverInputSource
    {
        [Header("Spline")]
        [SerializeField]
        private TrackSpline _track;

        [SerializeField]
        private float _lookAheadDistance = 8f;

        [SerializeField]
        private float _positionInterpolation = 6f;

        [Header("Speed")]
        [SerializeField]
        private float _targetSpeed = 45f;

        [SerializeField]
        private float _minimumAdvanceSpeed = 6f;

        [SerializeField]
        private float _accelerationResponsiveness = 4f;

        [Header("Steering")]
        [SerializeField]
        private float _steeringResponsiveness = 3f;

        [SerializeField]
        private float _headingAlignment = 8f;

        [Header("Dependencies")]
        [SerializeField]
        private GravityRegistry _gravityRegistry;

        private HoverController _controller;
        private Rigidbody _body;
        private float _distanceOnTrack;
        private float _throttleCommand;
        private float _steeringCommand;
        private Quaternion _desiredOrientation = Quaternion.identity;
        private Vector3 _lastForward = Vector3.forward;

        public float Throttle => _throttleCommand;
        public float Steering => _steeringCommand;
        public Quaternion DesiredOrientation => _desiredOrientation;

        private void Reset()
        {
            _controller = GetComponent<HoverController>();
            _body = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            _controller = GetComponent<HoverController>();
            _body = GetComponent<Rigidbody>();

            if (_gravityRegistry == null)
            {
                _gravityRegistry = GravityRegistry.Instance ?? FindObjectOfType<GravityRegistry>();
            }

            if (_track != null)
            {
                _distanceOnTrack = _track.FindClosestDistance(transform.position);
                _lastForward = _track.EvaluateTangent(_distanceOnTrack);
            }

            if (_controller != null)
            {
                _controller.SetInputSource(this);
            }
        }

        private void FixedUpdate()
        {
            if (_track == null)
            {
                return;
            }

            Vector3 expectedUp = _gravityRegistry != null ? _gravityRegistry.ExpectedUp : Vector3.up;
            float deltaTime = Time.fixedDeltaTime;

            float forwardSpeed = Vector3.Dot(_body.velocity, transform.forward);
            float advanceSpeed = Mathf.Max(_minimumAdvanceSpeed, Mathf.Abs(forwardSpeed));
            _distanceOnTrack = _track.AdvanceDistance(_distanceOnTrack, advanceSpeed, deltaTime);

            Vector3 trackPosition = _track.EvaluatePosition(_distanceOnTrack);
            Vector3 tangent = _track.EvaluateTangent(_distanceOnTrack);

            Vector3 lookAhead = _track.EvaluatePosition(_distanceOnTrack + _lookAheadDistance);
            Vector3 desiredForward = (lookAhead - trackPosition).normalized;
            if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
            {
                desiredForward = tangent.sqrMagnitude > Mathf.Epsilon ? tangent.normalized : _lastForward;
            }

            _lastForward = desiredForward;
            _desiredOrientation = Quaternion.LookRotation(desiredForward, expectedUp);

            float headingError = Vector3.SignedAngle(transform.forward, desiredForward, expectedUp);
            float steeringTarget = Mathf.Clamp(headingError / 45f, -1f, 1f);
            _steeringCommand = Mathf.MoveTowards(_steeringCommand, steeringTarget, _steeringResponsiveness * deltaTime);

            float speedError = _targetSpeed - forwardSpeed;
            float throttleTarget = Mathf.Clamp(speedError / Mathf.Max(_targetSpeed, 0.01f), -1f, 1f);
            _throttleCommand = Mathf.MoveTowards(_throttleCommand, throttleTarget, _accelerationResponsiveness * deltaTime);

            if (_body != null)
            {
                Vector3 position = Vector3.Lerp(_body.position, trackPosition, _positionInterpolation * deltaTime);
                _body.MovePosition(position);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, trackPosition, _positionInterpolation * deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, _desiredOrientation, _headingAlignment * deltaTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_track == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 position = _track.EvaluatePosition(_distanceOnTrack);
            Gizmos.DrawWireSphere(position, 0.5f);

            Gizmos.color = Color.green;
            Vector3 lookAhead = _track.EvaluatePosition(_distanceOnTrack + _lookAheadDistance);
            Gizmos.DrawLine(position, lookAhead);
        }
    }
}
