using UnityEngine;

namespace Vehicles
{
    public class HoverCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 2.5f, -6f);
        [SerializeField] private float positionSmoothTime = 0.2f;
        [SerializeField] private float rotationSmoothSpeed = 6f;
        [SerializeField] private float lookAheadDistance = 4f;
        [SerializeField] private float horizonAlignWeight = 0.85f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > Mathf.Epsilon
                ? -Physics.gravity.normalized
                : Vector3.up;

            Vector3 targetPoint = target.TransformPoint(targetOffset);
            transform.position = Vector3.SmoothDamp(transform.position, targetPoint, ref _velocity, positionSmoothTime);

            Vector3 focusPoint = target.position + target.forward * lookAheadDistance;
            Vector3 forward = (focusPoint - transform.position).normalized;
            if (forward.sqrMagnitude < Mathf.Epsilon)
            {
                forward = target.forward;
            }

            Vector3 desiredUp = Vector3.Slerp(target.up, gravityDirection, horizonAlignWeight).normalized;
            Quaternion desiredRotation = Quaternion.LookRotation(forward, desiredUp);

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
