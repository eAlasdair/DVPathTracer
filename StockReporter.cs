using System;
using UnityEngine;

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
        public const string Headings = "CID,CType,CPosX,CPosY,CPosZ,CRotA,CSpd";

        public string Values
        {
            get
            {
                return $"{ID},{Type},{Position.x},{Position.y - BaseReporter.seaLevel},{Position.z},{Rotation},{SpeedUnits(Speed)}";
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
                switch (Target.carType)
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
                }
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
