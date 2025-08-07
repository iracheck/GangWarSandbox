using GTA.Math;
using GTA;
using System.Collections.Generic;
using System;

namespace GangWarSandbox
{
    public class VehicleSet
    {
        private Random rand = new Random();

        public BlipColor Color { get; set; } = BlipColor.White;

        public List<Model> Vehicles { get; set; } = new List<Model>();
        public List<Model> WeaponizedVehicles { get; set; } = new List<Model>();
        public List<Model> Helicopters { get; set; } = new List<Model>();

        public enum Type
        {
            Vehicle,
            WeaponizedVehicle,
            Helicopter,
        }

        public Dictionary<Type, List<Model>> VehicleTypes => new Dictionary<Type, List<Model>>()
        {
            { Type.Vehicle, Vehicles },
            { Type.WeaponizedVehicle, WeaponizedVehicles },
            { Type.Helicopter, Helicopters }
        };

        public Model ChooseVehicleModel(Type type)
        {
            if (!VehicleTypes.TryGetValue(type, out var list) || list.Count == 0)
                return null;

            return list[rand.Next(0, list.Count)];
        }
    }
}
