using System;
using System.IO;
using UnityEngine;

namespace DVPathTracer
{
    public static class LocationReporter
    {
        public static string fileName = "DVTracedPath.csv";
        private const string basePath = "./Mods/DVPathTracer/";

        public const float seaLevel = 110;

        private static float startTime = 0f; // Time that the tracer was activated, in seconds since the game was started
        private static float nextTime = 0f;  // Time that the next report should be made, in seconds since the tracer was activated

        public static bool isReady = false;
        public static bool isActive = false;

        private static Settings settings;

        /**
         * Allows LocationReporter to read mod settings
         */
        public static void SetSettings(Settings modSettings)
        {
            settings = modSettings;
        }

        /**
         * Informs LocationReporter that the PlayerManager is ready
         */
        public static void ManagerIsSet()
        {
            if (!isReady)
            {
                isReady = true;
                Main.Log("Ready to go");
            }
        }

        /**
         * Prepares the file and allows the reporter to start reporting
         */
        public static void Activate()
        {
            PrepareFile();
            isActive = true;
            Main.settings.isActive = true;
            startTime = Time.time;
            Main.Log($"Reporting activated at {startTime}");
        }

        /**
         * Stops the reporter from reporting
         */
        public static void Deactivate()
        {
            isActive = false;
            Main.settings.isActive = false;
            startTime = 0f;
            nextTime = 0f;
            Main.Log($"Reporting ended at {Time.time}");
        }

        /**
         * Creates an empty .csv file of the set name, overwriting any existing file
         * The file will be .csv regardless of whether or not ".csv" is included in the set name
         */
        private static void PrepareFile()
        {
            fileName = settings.fileName;
            if (!fileName.EndsWith(".csv"))
            {
                fileName += ".csv";
            }
            File.WriteAllText(basePath + fileName, "Time,PosX,PosY,PosZ,RotA\n");
            Main.Log($"File {fileName} readied");
        }

        /**
         * Appends the given text to the set file
         */
        public static void WriteToFile(string text)
        {
            File.AppendAllText(basePath + fileName, text);
        }

        /**
         * Returns the player's position in the world, adjusted with respect to sea level
         */
        public static Vector3 GetPlayerPosition()
        {
            Vector3 position = PlayerManager.GetWorldAbsolutePlayerPosition();
            position.y -= seaLevel;
            return position;
        }

        /**
         * Returns the player's rotation about the y axis as an angle in degrees from in-game North
         */
        public static float GetPlayerRotation()
        {
            Transform playerTransform = PlayerManager.PlayerTransform;
            Vector3 planeRotation = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up);
            float rotationAngle = Mathf.Atan2(planeRotation.x, planeRotation.z) * Mathf.Rad2Deg;
            return rotationAngle >= 0 ? rotationAngle : 360 + rotationAngle;
        }

        /**
         * Return the player's current position and rotation
         * This string includes 5 comma-separated values indicating, in order:
         * - Time in seconds since the tracer was activated
         * - Player location in the world, adjusted to sea level, as x, y, z coordinates
         * - Player rotation about the y axis as an angle in degrees from in-game North (like a compass)
         */
        public static string GetReportOnPlayer(float time)
        {
            if (!isReady)
            {
                // Should only ever be possible while the game is loading afaict
                throw new Exception("Not yet ready");
            }

            Vector3 worldPosition = GetPlayerPosition();
            string pos = $"{worldPosition.x},{worldPosition.y},{worldPosition.z}";  // Position as a Vector

            float rotA = GetPlayerRotation();                                       // Rotation about the y axis as an angle in degrees from map North

            string report = $"{time},{pos},{rotA}";
            return report;
        }

        /**
         * Periodically reports the player's current position and rotation to a local .csv file as set by the player.
         */
        public static void TimedReportOnPlayer()
        {
            float upTime = Time.time - startTime;

            if (isActive && upTime >= nextTime)
            {
                string report;
                try
                {
                    report = GetReportOnPlayer(upTime);
                }
                catch // No Report Available, skip
                {
                    Main.Log($"No report at {upTime} available");
                    nextTime += settings.logRate;
                    return;
                }
                WriteToFile(report + "\n");

                nextTime += settings.logRate;
            }
        }
    }
}
