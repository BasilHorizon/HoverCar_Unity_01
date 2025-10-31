using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.StationAssembly
{
    [ExecuteAlways]
    public class ModuleInstance : MonoBehaviour
    {
        [SerializeField]
        private ModuleProfile profile;

        [SerializeField]
        private List<ModuleSocket> sockets = new();

        public ModuleProfile Profile => profile;

        public IReadOnlyList<ModuleSocket> Sockets => sockets;

        public void ApplyProfile(ModuleProfile moduleProfile)
        {
            profile = moduleProfile;
            RebuildSockets();
        }

        private void OnValidate()
        {
            if (profile != null)
            {
                RebuildSockets();
            }
        }

        public ModuleSocket GetSocket(string socketName)
        {
            foreach (var socket in sockets)
            {
                if (socket != null && socket.SocketName == socketName)
                {
                    return socket;
                }
            }

            return null;
        }

        public void RebuildSockets()
        {
            CleanupSockets();

            if (profile == null)
            {
                return;
            }

            var toRemove = new List<GameObject>();
            foreach (Transform child in transform)
            {
                var existingSocket = child.GetComponent<ModuleSocket>();
                if (existingSocket != null)
                {
                    toRemove.Add(existingSocket.gameObject);
                }
            }

            foreach (var go in toRemove)
            {
                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }

            sockets.Clear();

            foreach (var socketDefinition in profile.sockets)
            {
                var socketObject = new GameObject(socketDefinition.name)
                {
                    hideFlags = HideFlags.None
                };
                socketObject.transform.SetParent(transform);
                socketObject.transform.localPosition = socketDefinition.position;
                socketObject.transform.localRotation = Quaternion.LookRotation(socketDefinition.forward, socketDefinition.up);
                socketObject.transform.localScale = Vector3.one;

                var socketComponent = socketObject.AddComponent<ModuleSocket>();
                socketComponent.Owner = this;
                socketComponent.SocketName = socketDefinition.name;
                socketComponent.SplineId = socketDefinition.splineId;

                sockets.Add(socketComponent);
            }
        }

        private void CleanupSockets()
        {
            sockets.RemoveAll(s => s == null);
        }
    }
}
