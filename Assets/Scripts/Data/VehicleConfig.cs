using System.Collections.Generic;
using UnityEngine;

namespace HoverCar.Data
{
    [CreateAssetMenu(fileName = "VehicleConfig", menuName = "HoverCar/Data/Vehicle Config")]
    public class VehicleConfig : ScriptableObject
    {
        [System.Serializable]
        public class ModuleEntry
        {
            [Tooltip("Friendly label describing how this module is used in the vehicle (e.g. Chassis, Engine).")]
            public string slotId = "Module";

            [Tooltip("Profile of the module assigned to this slot.")]
            public ModuleProfile moduleProfile;
        }

        [Header("Metadata")]
        [SerializeField]
        private string displayName = "New Vehicle";

        [Header("Modules")]
        [SerializeField]
        private GravitySourceProfile gravitySource;

        [SerializeField]
        private List<ModuleEntry> modules = new();

        [Header("Stats")]
        [SerializeField]
        private float baseMass = 1200f;

        [SerializeField]
        private float maxSpeed = 45f;

        [SerializeField]
        private float acceleration = 12f;

        [SerializeField]
        private float handling = 0.7f;

        [SerializeField]
        private float energyCapacity = 100f;

        public string DisplayName => displayName;
        public GravitySourceProfile GravitySource => gravitySource;
        public IReadOnlyList<ModuleEntry> Modules => modules;
        public float BaseMass => baseMass;
        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Handling => handling;
        public float EnergyCapacity => energyCapacity;
    }
}
