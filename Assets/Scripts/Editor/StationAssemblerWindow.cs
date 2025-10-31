using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HoverCar.StationAssembly.Editor
{
    public class StationAssemblerWindow : EditorWindow
    {
        private const float SocketHandleSize = 0.25f;

        private ModuleProfile selectedProfile;
        private GameObject previewInstance;
        private ModuleInstance previewModuleInstance;
        private string selectedPreviewSocketName;
        private ModuleSocket targetSocket;
        private GameObject stationRoot;
        private bool autoAlign = true;
        private readonly List<Vector3> blendedSplinePoints = new();

        [MenuItem("Stations/Station Assembler")]
        public static void OpenWindow()
        {
            GetWindow<StationAssemblerWindow>("Station Assembler");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyPreview();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Module Selection", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                selectedProfile = (ModuleProfile)EditorGUILayout.ObjectField("Module Profile", selectedProfile, typeof(ModuleProfile), false);
                if (EditorGUI.EndChangeCheck())
                {
                    CreatePreview();
                }

                if (selectedProfile == null)
                {
                    EditorGUILayout.HelpBox("Assign a Module Profile to begin assembling.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(selectedProfile == null))
                {
                    if (GUILayout.Button("Create/Refresh Preview"))
                    {
                        CreatePreview();
                    }

                    if (GUILayout.Button("Clear Preview"))
                    {
                        DestroyPreview();
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
                autoAlign = EditorGUILayout.Toggle("Auto Align", autoAlign);
                stationRoot = (GameObject)EditorGUILayout.ObjectField("Station Root", stationRoot, typeof(GameObject), true);

                if (GUILayout.Button("Create Station Root"))
                {
                    stationRoot = new GameObject("StationRoot");
                    Undo.RegisterCreatedObjectUndo(stationRoot, "Create Station Root");
                }

                if (stationRoot == null)
                {
                    EditorGUILayout.HelpBox("No station root assigned. A root will be created automatically when saving if none exists.", MessageType.Warning);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Selected Preview Socket", string.IsNullOrEmpty(selectedPreviewSocketName) ? "None" : selectedPreviewSocketName);
                EditorGUILayout.LabelField("Target Socket", targetSocket == null ? "None" : targetSocket.SocketName);

                using (new EditorGUI.DisabledScope(selectedProfile == null || previewInstance == null))
                {
                    if (GUILayout.Button("Commit Placement"))
                    {
                        CommitPlacement();
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(stationRoot == null))
                {
                    if (GUILayout.Button("Save Layout As Prefab"))
                    {
                        SaveLayoutAsPrefab();
                    }

                    if (GUILayout.Button("Save Layout As Scene"))
                    {
                        SaveLayoutAsScene();
                    }
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.zTest = CompareFunction.Always;

            DrawExistingSockets();
            DrawPreview();

            if (previewInstance != null && autoAlign && targetSocket != null && !string.IsNullOrEmpty(selectedPreviewSocketName))
            {
                AlignPreviewToTarget();
            }

            if (previewInstance != null && targetSocket != null && !string.IsNullOrEmpty(selectedPreviewSocketName))
            {
                DrawConnectionPreview();
            }
        }

        private void DrawExistingSockets()
        {
            var sockets = ModuleSocket.ActiveSockets;
            Handles.color = Color.cyan;
            for (int i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];
                if (socket == null || socket.Owner == previewModuleInstance)
                {
                    continue;
                }

                var position = socket.Position;
                var rotation = Quaternion.LookRotation(socket.Forward, socket.Up);
                if (Handles.Button(position, rotation, SocketHandleSize, SocketHandleSize, Handles.SphereHandleCap))
                {
                    targetSocket = socket;
                    Repaint();
                }

                Handles.ArrowHandleCap(0, position, rotation, SocketHandleSize * 1.5f, EventType.Repaint);
                Handles.Label(position, socket.SocketName);
            }
        }

        private void DrawPreview()
        {
            if (previewInstance == null || selectedProfile == null)
            {
                return;
            }

            Handles.color = Color.green;
            var renderers = previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                Handles.DrawWireCube(bounds.center, bounds.size);
            }
            else
            {
                Handles.DrawWireCube(previewInstance.transform.position, Vector3.one);
            }

            if (selectedProfile.sockets == null)
            {
                return;
            }

            foreach (var socket in selectedProfile.sockets)
            {
                Vector3 position = GetPreviewSocketPosition(socket);
                Quaternion rotation = GetPreviewSocketRotation(socket);

                Handles.color = socket.name == selectedPreviewSocketName ? Color.yellow : Color.green;
                if (Handles.Button(position, rotation, SocketHandleSize, SocketHandleSize, Handles.CubeHandleCap))
                {
                    selectedPreviewSocketName = socket.name;
                    if (autoAlign)
                    {
                        AlignPreviewToTarget();
                    }
                    Repaint();
                }

                Handles.ArrowHandleCap(0, position, rotation, SocketHandleSize * 1.5f, EventType.Repaint);
                Handles.Label(position, socket.name);
            }
        }

        private void DrawConnectionPreview()
        {
            if (previewInstance == null || targetSocket == null || string.IsNullOrEmpty(selectedPreviewSocketName) || selectedProfile == null)
            {
                return;
            }

            var previewSocketDef = selectedProfile.GetSocket(selectedPreviewSocketName);
            if (previewSocketDef == null)
            {
                return;
            }

            Vector3 sourcePos = GetPreviewSocketPosition(previewSocketDef);
            Vector3 targetPos = targetSocket.Position;
            Handles.color = Color.magenta;
            Handles.DrawLine(sourcePos, targetPos);

            BlendSplineSegments(previewSocketDef, targetSocket);
            if (blendedSplinePoints.Count >= 2)
            {
                Handles.color = Color.Lerp(Color.magenta, Color.cyan, 0.5f);
                for (int i = 1; i < blendedSplinePoints.Count; i++)
                {
                    Handles.DrawLine(blendedSplinePoints[i - 1], blendedSplinePoints[i]);
                }
            }
        }

        private void BlendSplineSegments(ModuleProfile.SocketDefinition previewSocket, ModuleSocket target)
        {
            blendedSplinePoints.Clear();

            if (string.IsNullOrEmpty(previewSocket.splineId) || string.IsNullOrEmpty(target.SplineId))
            {
                return;
            }

            var previewSpline = selectedProfile.GetSpline(previewSocket.splineId);
            var targetProfile = target.Owner != null ? target.Owner.Profile : null;
            var targetSpline = targetProfile != null ? targetProfile.GetSpline(target.SplineId) : null;

            if (previewSpline == null || targetSpline == null)
            {
                return;
            }

            var previewRoot = previewInstance != null ? previewInstance.transform : null;
            var targetRoot = target.Owner != null ? target.Owner.transform : target.transform.parent;

            if (previewRoot == null || targetRoot == null)
            {
                return;
            }

            foreach (var point in previewSpline.controlPoints)
            {
                blendedSplinePoints.Add(previewRoot.TransformPoint(point));
            }

            for (int i = 0; i < targetSpline.controlPoints.Count; i++)
            {
                var point = targetSpline.controlPoints[i];
                blendedSplinePoints.Add(targetRoot.TransformPoint(point));
            }
        }

        private void CommitPlacement()
        {
            if (selectedProfile == null)
            {
                return;
            }

            var prefab = selectedProfile.modulePrefab;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", "The selected Module Profile does not reference a prefab.", "OK");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Module");
            if (previewInstance != null)
            {
                instance.transform.SetPositionAndRotation(previewInstance.transform.position, previewInstance.transform.rotation);
            }

            if (stationRoot == null)
            {
                stationRoot = new GameObject("StationRoot");
                Undo.RegisterCreatedObjectUndo(stationRoot, "Create Station Root");
            }

            Undo.SetTransformParent(instance.transform, stationRoot.transform, "Assign Station Root");

            var moduleInstance = instance.GetComponent<ModuleInstance>() ?? instance.AddComponent<ModuleInstance>();
            moduleInstance.ApplyProfile(selectedProfile);

            if (targetSocket != null && !string.IsNullOrEmpty(selectedPreviewSocketName))
            {
                var placedSocket = moduleInstance.GetSocket(selectedPreviewSocketName);
                if (placedSocket != null)
                {
                    placedSocket.Occupied = true;
                }

                targetSocket.Occupied = true;

                CreateSplineAsset(moduleInstance, placedSocket, targetSocket);
            }

            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            targetSocket = null;
            selectedPreviewSocketName = null;
            CreatePreview();
        }

        private void CreateSplineAsset(ModuleInstance newModule, ModuleSocket newSocket, ModuleSocket target)
        {
            if (newModule == null || newSocket == null || target == null)
            {
                return;
            }

            var previewDef = selectedProfile?.GetSocket(newSocket.SocketName);
            var targetProfile = target.Owner != null ? target.Owner.Profile : null;
            if (previewDef == null || targetProfile == null)
            {
                return;
            }

            var sourceSpline = selectedProfile.GetSpline(previewDef.splineId);
            var targetSpline = targetProfile.GetSpline(target.SplineId);
            if (sourceSpline == null || targetSpline == null)
            {
                return;
            }

            var splineObject = new GameObject($"Spline_{newSocket.SocketName}_to_{target.SocketName}");
            Undo.RegisterCreatedObjectUndo(splineObject, "Create Spline Segment");
            Undo.SetTransformParent(splineObject.transform, stationRoot.transform, "Parent Spline Segment");

            var splineComponent = splineObject.AddComponent<SplineSegment>();
            var worldPoints = new List<Vector3>();
            foreach (var point in sourceSpline.controlPoints)
            {
                worldPoints.Add(newModule.transform.TransformPoint(point));
            }

            var targetRoot = target.Owner != null ? target.Owner.transform : target.transform.parent;
            if (targetRoot == null)
            {
                return;
            }

            foreach (var point in targetSpline.controlPoints)
            {
                worldPoints.Add(targetRoot.TransformPoint(point));
            }

            splineComponent.SetControlPoints(ToLocalPoints(worldPoints, splineObject.transform));
        }

        private IEnumerable<Vector3> ToLocalPoints(IEnumerable<Vector3> worldPoints, Transform parent)
        {
            foreach (var point in worldPoints)
            {
                yield return parent.InverseTransformPoint(point);
            }
        }

        private void SaveLayoutAsPrefab()
        {
            if (stationRoot == null)
            {
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject("Save Station Prefab", stationRoot.name + ".prefab", "prefab", "Select a location for the station prefab.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(stationRoot, path, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
        }

        private void SaveLayoutAsScene()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            if (!currentScene.IsValid())
            {
                return;
            }

            var directory = string.IsNullOrEmpty(currentScene.path) ? "Assets" : Path.GetDirectoryName(currentScene.path);
            var defaultName = string.IsNullOrEmpty(currentScene.name) ? "StationLayout" : currentScene.name + "_Station";
            var path = EditorUtility.SaveFilePanel("Save Station Scene", directory, defaultName + ".unity", "unity");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            EditorSceneManager.SaveScene(currentScene, path);
        }

        private void AlignPreviewToTarget()
        {
            if (previewInstance == null || targetSocket == null || selectedProfile == null)
            {
                return;
            }

            var socketDef = selectedProfile.GetSocket(selectedPreviewSocketName);
            if (socketDef == null)
            {
                return;
            }

            Quaternion sourceRotation = Quaternion.LookRotation(socketDef.forward, socketDef.up);
            Quaternion targetRotation = Quaternion.LookRotation(-targetSocket.Forward, targetSocket.Up);
            Quaternion alignedRotation = targetRotation * Quaternion.Inverse(sourceRotation);
            Vector3 alignedPosition = targetSocket.Position - alignedRotation * socketDef.position;

            previewInstance.transform.SetPositionAndRotation(alignedPosition, alignedRotation);
        }

        private Vector3 GetPreviewSocketPosition(ModuleProfile.SocketDefinition socket)
        {
            if (previewInstance == null)
            {
                return Vector3.zero;
            }

            return previewInstance.transform.TransformPoint(socket.position);
        }

        private Quaternion GetPreviewSocketRotation(ModuleProfile.SocketDefinition socket)
        {
            if (previewInstance == null)
            {
                return Quaternion.identity;
            }

            return previewInstance.transform.rotation * Quaternion.LookRotation(socket.forward, socket.up);
        }

        private void CreatePreview()
        {
            DestroyPreview();

            if (selectedProfile == null || selectedProfile.modulePrefab == null)
            {
                return;
            }

            previewInstance = Instantiate(selectedProfile.modulePrefab);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewInstance.name = $"{selectedProfile.name}_Preview";
            previewInstance.SetActive(true);

            previewModuleInstance = previewInstance.GetComponent<ModuleInstance>() ?? previewInstance.AddComponent<ModuleInstance>();
            previewModuleInstance.ApplyProfile(selectedProfile);

            foreach (var socket in previewModuleInstance.Sockets)
            {
                socket.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void DestroyPreview()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
            }

            previewModuleInstance = null;
            selectedPreviewSocketName = null;
            targetSocket = null;
        }
    }
}
