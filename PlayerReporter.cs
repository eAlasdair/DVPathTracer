using UnityEngine;

// TODO: Learn the what and *why* of C# fields vs properties

namespace DVPathTracer
{
    public class PlayerReporter
    {
        // P = 'Player'
        public const string Headings = "PPosX,PPosY,PPosZ,PRotA";

        public string Values
        {
            get
            {
                return $"{Position.x},{Position.y - BaseReporter.seaLevel},{Position.z},{Rotation}";
            }
        }

        /**
         * Position in 3d space, no adjustments
         */
        public Vector3 Position
        {
            get
            {
                return PlayerManager.GetWorldAbsolutePlayerPosition();
            }
        }

        /**
         * Rotation from map-North, in degrees
         */
        public float Rotation
        {
            get
            {
                Transform playerTransform = PlayerManager.PlayerTransform;
                Vector3 planeRotation = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up);
                float rotationAngle = Mathf.Atan2(planeRotation.x, planeRotation.z) * Mathf.Rad2Deg;
                return rotationAngle >= 0 ? rotationAngle : 360 + rotationAngle;
            }
        }
    }
}
