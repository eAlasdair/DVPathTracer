using System;
using UnityEngine;
using HarmonyLib;
using CommandTerminal;

namespace DVPathTracer
{
    /**
     * This class adapts code created by Miles "Zeibach" Spielberg
     * Copyright 2020 Miles Spielberg. Licensed under MIT License
     * Primary source: https://github.com/mspielberg/dv-steamcutoff
     */
    public static class Commands
    {
        [HarmonyPatch(typeof(Terminal), "Start")]
        public static class RegisterCommandsPatch
        {
            public static void Postfix()
            {
                Register();
            }
        }

        private static void Register(string name, Action<CommandArg[]> proc)
        {
            name = Main.entry.Info.Id + "." + name;
            if (Terminal.Shell == null)
                return;
            if (Terminal.Shell.Commands.Remove(name.ToUpper()))
                Main.Log($"replacing existing command {name}");
            else
                Terminal.Autocomplete.Register(name);
            Terminal.Shell.AddCommand(name, proc);
        }

        public static void Register()
        {
            Register("reportHere", _ => {
                Terminal.Log(LocationReporter.GetReportOnPlayer(Time.time));
            });

            Register("playerLocation", _ => {
                Vector3 pos = LocationReporter.GetPlayerPosition();
                Terminal.Log($"x = {pos.x}\ny = {pos.y}\nz = {pos.z}");
            });

            Register("playerRotation", _ => {
                Terminal.Log(LocationReporter.GetPlayerRotation().ToString());
            });

            Register("whatFile", _ => {
                Terminal.Log($"Tracer is set to use file: {Main.settings.fileName}");
            });

            Register("setFileTo", args => {
                if (!LocationReporter.isActive)
                {
                    if (args.Length > 0)
                    {
                        string newFile = args[0].String;
                        if (!newFile.EndsWith(".csv"))
                        {
                            newFile += ".csv";
                        }
                        Main.settings.fileName = newFile;
                        Terminal.Log($"Tracer set to use file: {newFile}");
                    }
                    else
                    {
                        Terminal.Log($"ERROR: Provide a file to use!");
                    }
                }
                else
                {
                    Terminal.Log($"ERROR: Tracer is still running!");
                }
            });

            Register("whatReportInterval", _ => {
                Terminal.Log($"Tracer is set to report every {Main.settings.logRate} seconds");
            });

            Register("setReportIntervalTo", args => {
                if (args.Length > 0)
                {
                    float newPeriod = float.Parse(args[0].String);
                    if (newPeriod >= 1)
                    {
                        Main.settings.logRate = newPeriod;
                        Terminal.Log($"Tracer set to report every {newPeriod} seconds");
                    }
                    else
                    {
                        Terminal.Log($"ERROR: Tracer will not report in intervals of less than 1 second");
                    }
                }
                else
                {
                    Terminal.Log($"ERROR: Provide a new report interval!");
                }
            });

            Register("isActive", _ => {
                if (LocationReporter.isActive)
                {
                    Terminal.Log($"Tracer is active, writing to file: {LocationReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"Tracer is not active");
                }
            });

            Register("activate", _ => {
                if (!LocationReporter.isActive)
                {
                    LocationReporter.Activate();
                    Terminal.Log($"Tracing begun to file: {LocationReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"ERROR: Tracer already active! Writing to file: {LocationReporter.fileName}");
                }
            });

            Register("deactivate", _ => {
                if (LocationReporter.isActive)
                {
                    LocationReporter.Deactivate();
                    Terminal.Log($"Tracing ended using file: {LocationReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"ERROR: Tracer is not running!");
                }
            });

            Register("disablePreventActivationOnStartup", _ => {
                Main.settings.forceStartInactive = false;
                Terminal.Log("Tracer will remember if it was active when you end the game and will, if active on startup, overwrite any existing file.\n" +
                    "Be wary of side-effects in .csv output while the game is loading.");
            });

            Register("enablePreventActivationOnStartup", _ => {
                Main.settings.forceStartInactive = true;
                Terminal.Log("Tracer will never start active, you must enable it manually");
            });
        }
    }
}
