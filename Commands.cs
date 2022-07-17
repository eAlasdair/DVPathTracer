using System;
using UnityEngine;
using HarmonyLib;
using CommandTerminal;

namespace DVPathTracer
{
    /**
     * This class adapts code created by Miles "Zeibach" Spielberg
     * Copyright 2020 Miles Spielberg. Licensed under MIT License
     * Primary source: https://github.com/mspielberg/dv-steamcutoff/blob/master/Commands.cs
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
                StockFinder.UpdateTrackedStock();
                Terminal.Log(BaseReporter.GetReport(Time.time));
            });

            Register("playerLocation", _ => {
                Vector3 pos = BaseReporter.player.Position;
                Terminal.Log($"x = {pos.x}\ny = {pos.y - BaseReporter.seaLevel}\nz = {pos.z}");
            });

            Register("playerRotation", _ => {
                Terminal.Log(BaseReporter.player.Rotation.ToString());
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
                if (BaseReporter.isActive)
                {
                    Terminal.Log($"Tracer is active, writing to file: {BaseReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"Tracer is not active");
                }
            });

            Register("activate", _ => {
                if (!BaseReporter.isActive)
                {
                    BaseReporter.Activate();
                    Terminal.Log($"Tracing begun to file: {BaseReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"ERROR: Tracer already active! Writing to file: {BaseReporter.fileName}");
                }
            });

            Register("deactivate", _ => {
                if (BaseReporter.isActive)
                {
                    BaseReporter.Deactivate();
                    Terminal.Log($"Tracing ended using file: {BaseReporter.fileName}");
                }
                else
                {
                    Terminal.Log($"ERROR: Tracer is not running!");
                }
            });

            Register("disablePreventActivationOnStartup", _ => {
                Main.settings.forceStartInactive = false;
                Terminal.Log("Tracer will remember if it was active when you end the game and will, if active on startup, overwrite any existing file.");
            });

            Register("enablePreventActivationOnStartup", _ => {
                Main.settings.forceStartInactive = true;
                Terminal.Log("Tracer will never start active, you must enable it manually");
            });
        }
    }
}
