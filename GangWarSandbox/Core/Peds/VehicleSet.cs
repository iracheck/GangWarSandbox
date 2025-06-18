using GTA.Math;
using GTA;
using System.Collections.Generic;
using System;

namespace GangWarSandbox.Core
{
    public class VehicleSet
    {
        private Random rand = new Random();

        public BlipColor Color { get; set; } = BlipColor.White;

        public List<string> Vehicles { get; set; } = new List<string>();
        public List<string> WeaponizedVehicles { get; set; } = new List<string>();
        public List<string> Helicopters { get; set; } = new List<string>();

        public enum Type
        {
            Vehicle,
            WeaponizedVehicle,
            Helicopter,
        }

        protected Dictionary<Type, List<string>> VehicleTypes => new Dictionary<Type, List<string>>()
        {
            { Type.Vehicle, Vehicles },
            { Type.WeaponizedVehicle, WeaponizedVehicles },
            { Type.Helicopter, Helicopters }
        };

        public string ChooseVehicleModel(Type type)
        {
            if (!VehicleTypes.TryGetValue(type, out var list) || list.Count == 0)
                return null;

            return list[rand.Next(0, list.Count)];
        }

        // Optional: you could expose a Spawn method for convenience
        public Vehicle SpawnVehicle(Type type, Vector3 position)
        {
            string modelName = ChooseVehicleModel(type);
            if (modelName == null) return null;

            Model model = new Model(modelName);
            if (!model.IsValid || !model.IsVehicle) return null;

            Vehicle vehicle = World.CreateVehicle(model, position);

            return vehicle;
        }
    }
}
