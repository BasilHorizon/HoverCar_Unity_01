using System.Text;
using HoverCar.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace HoverCar.UI
{
    /// <summary>
    /// Displays live hovercraft and gravity telemetry along with runtime toggles via Unity UI.
    /// </summary>
    public class DebugHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private HoverVehicleController vehicleController;

        [SerializeField]
        private GravityController gravityController;

        [SerializeField]
        private CanvasGroup hudCanvasGroup;

        [Header("UI Widgets")]
        [SerializeField]
        private Text vehicleStateText;

        [SerializeField]
        private Text gravityStateText;

        [SerializeField]
        private Button alignmentBoostButton;

        [SerializeField]
        private Button autoLevelButton;

        [SerializeField]
        private Button gravityModeButton;

        [Header("Input")]
        [SerializeField]
        private KeyCode toggleHudKey = KeyCode.BackQuote;

        [SerializeField]
        private KeyCode toggleAlignmentBoostKey = KeyCode.B;

        [SerializeField]
        private KeyCode toggleAutoLevelKey = KeyCode.N;

        [SerializeField]
        private KeyCode toggleGravityKey = KeyCode.G;

        private bool hudAllowed;
        private bool hudVisible;

        private void Awake()
        {
            hudAllowed = DebugHudSettings.IsHudAllowedInCurrentBuild();
            hudVisible = hudAllowed && DebugHudSettings.ShouldHudStartVisible();
            ApplyHudVisibility();
        }

        private void OnEnable()
        {
            if (alignmentBoostButton != null)
            {
                alignmentBoostButton.onClick.AddListener(OnAlignmentBoostClicked);
            }

            if (autoLevelButton != null)
            {
                autoLevelButton.onClick.AddListener(OnAutoLevelClicked);
            }

            if (gravityModeButton != null)
            {
                gravityModeButton.onClick.AddListener(OnGravityModeClicked);
            }

            if (vehicleController != null)
            {
                vehicleController.AlignmentBoostChanged += OnVehicleAlignmentChanged;
                vehicleController.AutoLevelChanged += OnVehicleAutoLevelChanged;
            }

            if (gravityController != null)
            {
                gravityController.CustomGravityChanged += OnGravityModeChanged;
            }

            UpdateButtonLabels();
            RefreshTelemetry();
        }

        private void OnDisable()
        {
            if (alignmentBoostButton != null)
            {
                alignmentBoostButton.onClick.RemoveListener(OnAlignmentBoostClicked);
            }

            if (autoLevelButton != null)
            {
                autoLevelButton.onClick.RemoveListener(OnAutoLevelClicked);
            }

            if (gravityModeButton != null)
            {
                gravityModeButton.onClick.RemoveListener(OnGravityModeClicked);
            }

            if (vehicleController != null)
            {
                vehicleController.AlignmentBoostChanged -= OnVehicleAlignmentChanged;
                vehicleController.AutoLevelChanged -= OnVehicleAutoLevelChanged;
            }

            if (gravityController != null)
            {
                gravityController.CustomGravityChanged -= OnGravityModeChanged;
            }
        }

        private void Update()
        {
            if (!hudAllowed)
            {
                return;
            }

            HandleInput();
            RefreshTelemetry();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleHudKey))
            {
                ToggleHud();
            }

            if (!hudVisible)
            {
                return;
            }

            if (Input.GetKeyDown(toggleAlignmentBoostKey))
            {
                ToggleAlignmentBoost();
            }

            if (Input.GetKeyDown(toggleAutoLevelKey))
            {
                ToggleAutoLevel();
            }

            if (Input.GetKeyDown(toggleGravityKey))
            {
                ToggleGravityMode();
            }
        }

        private void RefreshTelemetry()
        {
            if (!hudVisible)
            {
                return;
            }

            if (vehicleStateText != null)
            {
                vehicleStateText.text = BuildVehicleTelemetry();
            }

            if (gravityStateText != null)
            {
                gravityStateText.text = BuildGravityTelemetry();
            }
        }

        private string BuildVehicleTelemetry()
        {
            if (vehicleController == null)
            {
                return "Vehicle controller: n/a";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Speed: {vehicleController.CurrentSpeed,6:0.0} m/s");
            builder.AppendLine($"Hover Force: {vehicleController.LastAppliedHoverForce.magnitude,6:0.0} N");
            builder.AppendLine($"Alignment Boost: {(vehicleController.AlignmentBoostEnabled ? "ON" : "OFF")}");
            builder.Append($"Auto Level: {(vehicleController.AutoLevelEnabled ? "ON" : "OFF")}");
            return builder.ToString();
        }

        private string BuildGravityTelemetry()
        {
            if (gravityController == null)
            {
                return "Gravity controller: n/a";
            }

            var gravity = gravityController.CurrentGravity;
            var builder = new StringBuilder();
            builder.AppendLine($"Mode: {(gravityController.UseCustomGravity ? "Custom" : "Unity")}");
            builder.Append($"Vector: ({gravity.x:0.00}, {gravity.y:0.00}, {gravity.z:0.00})");
            return builder.ToString();
        }

        private void ToggleHud()
        {
            hudVisible = !hudVisible;
            ApplyHudVisibility();
        }

        private void ApplyHudVisibility()
        {
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = hudVisible && hudAllowed ? 1f : 0f;
                hudCanvasGroup.interactable = hudVisible && hudAllowed;
                hudCanvasGroup.blocksRaycasts = hudVisible && hudAllowed;
            }

            if (!hudAllowed && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void ToggleAlignmentBoost()
        {
            if (vehicleController == null)
            {
                return;
            }

            vehicleController.ToggleAlignmentBoost();
        }

        private void ToggleAutoLevel()
        {
            if (vehicleController == null)
            {
                return;
            }

            vehicleController.ToggleAutoLevel();
        }

        private void ToggleGravityMode()
        {
            if (gravityController == null)
            {
                return;
            }

            gravityController.ToggleCustomGravity();
        }

        private void UpdateButtonLabels()
        {
            UpdateButtonLabel(alignmentBoostButton, vehicleController != null && vehicleController.AlignmentBoostEnabled, "Alignment Boost");
            UpdateButtonLabel(autoLevelButton, vehicleController != null && vehicleController.AutoLevelEnabled, "Auto Level");
            UpdateButtonLabel(gravityModeButton, gravityController != null && gravityController.UseCustomGravity, "Gravity");
        }

        private void UpdateButtonLabel(Button button, bool state, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = $"{label}: {(state ? "ON" : "OFF")}";
            }
        }

        private void OnAlignmentBoostClicked()
        {
            ToggleAlignmentBoost();
        }

        private void OnAutoLevelClicked()
        {
            ToggleAutoLevel();
        }

        private void OnGravityModeClicked()
        {
            ToggleGravityMode();
        }

        private void OnVehicleAlignmentChanged(bool value)
        {
            UpdateButtonLabels();
        }

        private void OnVehicleAutoLevelChanged(bool value)
        {
            UpdateButtonLabels();
        }

        private void OnGravityModeChanged(bool value)
        {
            UpdateButtonLabels();
        }
    }
}
