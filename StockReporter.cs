using System;
using UnityEngine;
using DV.Logic.Job;

// TODO: Learn the what and *why* of C# fields vs properties

namespace DVPathTracer
{
    public class StockReporter
    {
        public TrainCar Target { get; set; }

        /**
         * Rolling stock reporter, reports on the given train car (Currently exclusively a loco or the caboose)
         */
        public StockReporter(TrainCar car)
        {
            Target = car;
        }

        // C = 'Car' (includes locos)
        // Cg = 'Cargo' loaded in car (if applicable)
        public const string Headings = "CID,CType,CPosX,CPosY,CPosZ,CRotA,CSpd,C%,Cargo,CgTyp,Cg%";

        public string Values
        {
            get
            {
                string values = $"{ID},{Type},{Position.x},{Position.y - BaseReporter.seaLevel},{Position.z},{Rotation},{SpeedUnits(Speed)},{CarHealth}";
                if (Target.LoadedCargo == CargoType.None)
                {
                    values += ",None,N/A,N/A";
                }
                else
                {
                    values += $",{Cargo},{CargoCategory},{CargoHealth}";
                }
                return values;
            }
        }

        /**
         * L-001 etc
         */
        public string ID
        {
            get
            {
                return Target.ID;
            }
        }

        /**
         * Reader-friendly descriptor of the type of car
         */
        public string Type
        {
            get
            {
                string type = "";
                if (Main.cclEnabled)
                {
                    try
                    {
                        type = CCLInterface.CustomCarIndentifier(Target.carType);
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
                    type = Target.carType.ToString();
                }
                /*switch (Target.carType)
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
                        if (Main.cclEnabled)
                        {
                            try
                            {
                                type = CCLInterface.CustomCarIndentifier(Target.carType);
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
                            type = Target.carType.ToString();
                        }
                        break;
                }*/
                return type;
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
                //Main.Log($"Car health of {ID}: {Target.CarDamage.EffectiveHealthPercentage100Notation}");
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
                //Main.Log($"Cargo health of {ID}: {Target.CargoDamage.EffectiveHealthPercentage100Notation}");
                return Target.CargoDamage.EffectiveHealthPercentage100Notation;
            }
        }

        /**
         * Cargo name
         */
        public string Cargo
        {
            get
            {
                Main.Log($"Cargo of {ID}: {CargoTypes.GetCargoName(Target.LoadedCargo)}");
                return CargoTypes.GetCargoName(Target.LoadedCargo);
            }
        }

        /**
         * DVPT-specific human-readable category for use in animator
         */
        public string CargoCategory
        {
            get
                // ???????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????? idk
            {
                return "TODO";
                /*
                // Mil>Haz>Inert>Empty if more than one apply
                if (CargoTypes.Hazmat3Cargo.Contains(Target.LoadedCargo))
                {
                    return "HZMT1";
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
                }*/
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
    }
}
