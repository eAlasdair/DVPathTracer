using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DVPathTracer
{
    public static class BaseReporter
    {
        public static string fileName = "DVTracedPath.csv";
        private const string basePath = "./Mods/DVPathTracer/Sessions/";

        public const float seaLevel = 110;

        private static float startTime = 0f;      // Time that the tracer was activated, in seconds since the game was started
        private static float nextReportTime = 0f; // Time that the next report should be made, in seconds since the tracer was activated
        private static float nextUpdateTime = 0f; // Time that the list of tracked rolling stock should be updated, in seconds since the tracer was activated

        public static bool isReady = false;
        public static bool isActive = false;

        public static PlayerReporter player;

        /**
         * Informs the reporter that the PlayerManager is ready
         */
        public static void ManagerIsSet()
        {
            if (!isReady)
            {
                player = new PlayerReporter();
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
            nextReportTime = 0f;
            nextUpdateTime = 0f;
            Main.Log($"Reporting ended at {Time.time}");
        }

        /**
         * Creates an empty .csv file of the set name, overwriting any existing file
         * The file will be .csv regardless of whether or not ".csv" is included in the set name
         */
        private static void PrepareFile()
        {
            String DateTimeMaker()
            {
                DateTime nowTime = DateTime.Now;
                //time string format: Date=MM-DD-YYYY Time=HH'MM
                return $"Date={nowTime.Month.ToString("00")}-{nowTime.Day.ToString("00")}-{nowTime.Year.ToString("0000")} Time={nowTime.Hour.ToString("00")}'{nowTime.Minute.ToString("00")}";
            }

            fileName = Main.settings.fileName;
            if (Main.settings.betaMode)
            {
                fileName += DateTimeMaker();
            }
            if (!fileName.EndsWith(".csv"))
            {
                fileName += ".csv";
            }
            try
            {
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
                File.WriteAllText(basePath + fileName, $"Time,{PlayerReporter.Headings},{StockReporter.Headings},{StockReporter.Headings}\n");
                Main.Log($"File {fileName} readied");
            }
            catch (Exception e) when (e is IOException | e is DirectoryNotFoundException)
            {
                Main.Log($"Writing to {basePath + fileName} failed, reason {e.Message}");
            }

        }

        /**
         * Appends the given text to the set file
         */
        public static void WriteToFile(string text)
        {
            File.AppendAllText(basePath + fileName, text);
        }

        /**
         * Return a string of various current properties about the player and rolling stock ordered appropriately
         */
        public static string GetReport(float time)
        {
            if (!isReady)
            {
                throw new Exception("Not yet ready");
            }

            string report = $"{time},{player.Values},";
            List<int> toRemove = new List<int>();

            foreach (int index in StockFinder.TrackedStock.Keys)
            {
                if (StockFinder.TrackedStock[index] == null) // No car to report on
                {
                    report += $"{StockReporter.Headings},";
                }
                else if (StockFinder.TrackedStock[index].ID == String.Empty) // Car no longer exists
                {
                    Main.Log($"Removing deleted car {StockFinder.TrackedStock[index].ID}");
                    report += $"{StockReporter.Headings},";
                    toRemove.Add(index);
                }
                else
                {
                    report += $"{StockFinder.TrackedStock[index].Values},";
                }
            }
            foreach (int index in toRemove)
            {
                StockFinder.Remove(index);
            }
            return report;
        }

        /**
         * Periodically reports information to a local .csv file as set by the player.
         */
        public static void TimedReport()
        {
            float upTime = Time.time - startTime;

            if (isActive)
            {
                if (upTime >= nextReportTime)
                {
                    string report;
                    try
                    {
                        report = GetReport(upTime);
                    }
                    catch // No Report Available, skip
                          // Should only ever happen while the game is loading
                    {
                        Main.Log($"No report at {upTime} available");
                        nextReportTime += Main.settings.logRate;
                        return;
                    }
                    WriteToFile(report + "\n");

                    nextReportTime += Main.settings.logRate;
                }
                else if (upTime >= nextUpdateTime) // use 'else' to avoid doing too much in one cycle
                {
                    StockFinder.UpdateTrackedStock();
                    nextUpdateTime += 10;
                }
            }
        }
    }
}
