using UnityEngine;

namespace HoverCar.StationAssembly
{
    [ExecuteAlways]
    public class ModuleSocket : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<ModuleSocket> ActiveSocketsInternal = new();

        public static System.Collections.Generic.IReadOnlyList<ModuleSocket> ActiveSockets => ActiveSocketsInternal;

        [SerializeField]
        private ModuleInstance owner;

        [SerializeField]
        private string socketName;

        [SerializeField]
        private string splineId;

        [SerializeField]
        private bool occupied;

        public ModuleInstance Owner
        {
            get => owner;
            internal set => owner = value;
        }

        public string SocketName
        {
            get => socketName;
            internal set => socketName = value;
        }

        public string SplineId
        {
            get => splineId;
            internal set => splineId = value;
        }

        public bool Occupied
        {
            get => occupied;
            set => occupied = value;
        }

        public Vector3 Position => transform.position;

        public Vector3 Forward => transform.forward;

        public Vector3 Up => transform.up;

        private void OnEnable()
        {
            if (!ActiveSocketsInternal.Contains(this))
            {
                ActiveSocketsInternal.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSocketsInternal.Remove(this);
        }

        public void AlignToWorldPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
