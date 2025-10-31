using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.StationAssembly
{
    [CreateAssetMenu(menuName = "Stations/Module Profile", fileName = "ModuleProfile")]
    public class ModuleProfile : ScriptableObject
    {
        [Tooltip("Prefab that represents this module in the world.")]
        public GameObject modulePrefab;

        [Serializable]
        public class SocketDefinition
        {
            public string name = "Socket";
            public Vector3 position = Vector3.zero;
            public Vector3 forward = Vector3.forward;
            public Vector3 up = Vector3.up;
            [Tooltip("Optional spline identifier used to blend spline segments when connecting modules.")]
            public string splineId;
        }

        [Serializable]
        public class SplineDefinition
        {
            public string id = Guid.NewGuid().ToString();
            [Tooltip("Control points in local space relative to the module root.")]
            public List<Vector3> controlPoints = new();
        }

        [Tooltip("Sockets that can be connected to other modules.")]
        public List<SocketDefinition> sockets = new();

        [Tooltip("Spline segments that can be blended when sockets are connected.")]
        public List<SplineDefinition> splines = new();

        public SocketDefinition GetSocket(string socketName)
        {
            return sockets.Find(s => string.Equals(s.name, socketName, StringComparison.Ordinal));
        }

        public SplineDefinition GetSpline(string id)
        {
            return splines.Find(s => string.Equals(s.id, id, StringComparison.Ordinal));
        }
    }
}
