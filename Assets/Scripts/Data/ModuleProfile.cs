using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.Data
{
    [CreateAssetMenu(fileName = "ModuleProfile", menuName = "HoverCar/Data/Module Profile")]
    public class ModuleProfile : ScriptableObject
    {
        [System.Serializable]
        public class ModuleSocket
        {
            [Tooltip("Identifier used when pairing sockets together.")]
            public string id = "Default";

            [Tooltip("Local position of the socket relative to the module origin.")]
            public Vector3 localPosition = Vector3.zero;

            [Tooltip("Local rotation (Euler angles) applied to the socket relative to the module origin.")]
            public Vector3 localEulerAngles = Vector3.zero;

            [Tooltip("Local scaling applied to attached modules when connected through this socket.")]
            public Vector3 localScale = Vector3.one;

            [Tooltip("Optional list of modules that can be connected to this socket.")]
            public List<ModuleProfile> compatibleModules = new();
        }

        [Header("Metadata")]
        [SerializeField]
        private string displayName = "New Module";

        [Header("Connectivity")]
        [SerializeField]
        private List<ModuleSocket> sockets = new();

        [Header("Spline Data")]
        [Tooltip("Reference to a spline asset that defines the local traversal path for this module.")]
        [SerializeField]
        private Object localSpline;

        public string DisplayName => displayName;
        public IReadOnlyList<ModuleSocket> Sockets => sockets;
        public Object LocalSpline => localSpline;
    }
}
