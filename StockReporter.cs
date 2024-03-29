﻿using System;
using UnityEngine;
using DV.Logic.Job;

// TODO: Learn the what and *why* of C# fields vs properties

namespace DVPathTracer
{
    public class StockReporter
    {
        public TrainCar Target { get; private set; }
        public string TargetID { get; private set; }
        public string TargetType { get; private set; }

        private string prevValues = "";

        /**
         * Rolling stock reporter, reports on the given train car (inc. locos, caboose)
         */
        public StockReporter(TrainCar car)
        {
            Target = car;
            TargetID = car.ID;
            TargetType = GetCarType(car);
        }

        // C = 'Car' (includes locos)
        // Cg = 'Cargo' loaded in car (if applicable)
        public const string Headings = "CID,CType,CPosX,CPosY,CPosZ,CRotA,CSpd,C%,CgType,CgCat,Cg%";

        public string Values
        {
            get
            {
                string newValues = $"{TargetID},{TargetType},{Position.x},{Position.y - BaseReporter.seaLevel},{Position.z},{Rotation},{SpeedUnits(Speed)},{CarHealth}";
                if (Target.LoadedCargo == CargoType.None)
                {
                    if (CargoTypes.Military1CarContainers.Contains(CargoTypes.CarTypeToContainerType[Target.carType]))
                    {
                        // Military 1 licence allows LH jobs of military cars - it's more interesting to class as MIL1 despite being empty
                        newValues += ",None,MIL1,N/A";
                    }
                    else
                    {
                        // Loco, caboose, or otherwise unloaded car
                        newValues += ",None,N/A,N/A";
                    }
                }
                else
                {
                    newValues += $",{Cargo},{CargoCategory},{CargoHealth}";
                }
                if (Main.verboseTracing || newValues != prevValues)
                {
                    prevValues = newValues;
                    return newValues;
                }
                return "";
            }
        }

        public string Removed
        {
            get
            {
                return $"{TargetID},{TargetType},Removed,N/A,N/A,N/A,N/A,N/A,N/A,N/A,N/A";
            }
        }

        /**
         * In-game speed - metres per second
         */
        public float Speed
        {
            get
            {
                return Target.GetForwardSpeed();
            }
        }

        /**
         * Position in 3d space, no adjustments
         */
        public Vector3 Position
        {
            get
            {
                return Target.transform.position - WorldMover.currentMove;
            }
        }

        /**
         * Rotation from map-North, in degrees
         */
        public float Rotation
        {
            get
            {
                Transform locoTransform = Target.transform;
                Vector3 planeRotation = Vector3.ProjectOnPlane(locoTransform.forward, Vector3.up);
                float rotationAngle = Mathf.Atan2(planeRotation.x, planeRotation.z) * Mathf.Rad2Deg;
                return rotationAngle >= 0 ? rotationAngle : 360 + rotationAngle;
            }
        }

        /**
         * Car health, %
         */
        public float CarHealth
        {
            get
            {
                return Target.CarDamage.EffectiveHealthPercentage100Notation;
            }
        }

        /**
         * Cargo health, %
         */
        public float CargoHealth
        {
            get
            {
                return Target.CargoDamage.EffectiveHealthPercentage100Notation;
            }
        }

        /**
         * In-game name of the cargo
         */
        public string Cargo
        {
            get
            {
                return CargoTypes.GetCargoName(Target.LoadedCargo);
            }
        }

        /**
         * DVPT-specific human-readable category of loaded cargo for use in animator
         * Assumes there is loaded cargo to categorise
         */
        public string CargoCategory
        {
            get
            {
                // Mil>Haz if more than one apply
                if (CargoTypes.Military3Cargo.Contains(Target.LoadedCargo))
                {
                    return "MIL3";
                }
                if (CargoTypes.Military2Cargo.Contains(Target.LoadedCargo))
                {
                    return "MIL2";
                }
                if (CargoTypes.Hazmat3Cargo.Contains(Target.LoadedCargo))
                {
                    return "HZMT3";
                }
                if (CargoTypes.Hazmat2Cargo.Contains(Target.LoadedCargo))
                {
                    return "HZMT2";
                }
                if (CargoTypes.Hazmat1Cargo.Contains(Target.LoadedCargo))
                {
                    return "HZMT1";
                }
                return "Inert";
            }
        }

        /**
         * Returns the given speed in m/s converted to mph or kph as appropriate
         */
        public static float SpeedUnits(float speed)
        {
            if (Main.settings.mph)
            {
                return speed * (float) 2.23694;
            }
            return speed * (float) 3.6;
        }

        /**
         * Returns a reader-friendly descriptor of the type of car
         */
        private string GetCarType(TrainCar car)
        {
            string type = "";
            if (Main.cclEnabled)
            {
                try
                {
                    type = CCLInterface.CustomCarIndentifier(car.carType);
                }
                catch //(Exception e)
                {
                    //Main.Log(e.ToString());
                    // It's either an error or just not CCL stock
                    // TODO: this better
                }
            }
            if (type == "")
            {
                switch (car.carType)
                {
                    case TrainCarType.LocoShunter:
                        type = "DE2";
                        break;
                    case TrainCarType.LocoSteamHeavy:
                        type = "SH282";
                        break;
                    case TrainCarType.LocoDiesel:
                        type = "DE6";
                        break;
                    case TrainCarType.CabooseRed:
                        type = "Caboose";
                        break;
                    case TrainCarType.NotSet:
                    default:
                        type = car.carType.ToString();
                        break;
                }
            }

            return type;
        }
    }
}
