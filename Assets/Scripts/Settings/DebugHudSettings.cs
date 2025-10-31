using UnityEngine;

namespace HoverCar.Gameplay
{
    /// <summary>
    /// ScriptableObject based configuration for the debug HUD. The settings allow teams to disable
    /// the HUD in release builds without removing editor time tooling.
    /// </summary>
    [CreateAssetMenu(menuName = "HoverCar/Settings/Debug HUD Settings", fileName = "DebugHudSettings")]
    public class DebugHudSettings : ScriptableObject
    {
        private const bool DefaultHudEnabledInBuilds = true;
        private const bool DefaultHudVisibleOnStart = true;

        private static DebugHudSettings cachedSettings;

        [Tooltip("When disabled the HUD is never displayed outside the Unity Editor.")]
        [SerializeField]
        private bool enableHudInBuilds = true;

        [Tooltip("Should the HUD be visible immediately on scene load when it is allowed?")]
        [SerializeField]
        private bool showHudOnStart = true;

        /// <summary>
        /// Returns the resolved settings instance, falling back to defaults when no asset exists.
        /// </summary>
        public static DebugHudSettings Instance
        {
            get
            {
                if (cachedSettings == null)
                {
                    cachedSettings = Resources.Load<DebugHudSettings>("DebugHudSettings");
                }

                return cachedSettings;
            }
        }

        /// <summary>
        /// Determines whether the HUD is allowed to be displayed in the current build.
        /// </summary>
        public static bool IsHudAllowedInCurrentBuild()
        {
            if (Application.isEditor)
            {
                return true;
            }

            var instance = Instance;
            if (instance == null)
            {
                return DefaultHudEnabledInBuilds;
            }

            return instance.enableHudInBuilds;
        }

        /// <summary>
        /// Determines whether the HUD should start visible when the scene loads.
        /// </summary>
        public static bool ShouldHudStartVisible()
        {
            if (!IsHudAllowedInCurrentBuild())
            {
                return false;
            }

            var instance = Instance;
            if (instance == null)
            {
                return DefaultHudVisibleOnStart;
            }

            return instance.showHudOnStart;
        }
    }
}
