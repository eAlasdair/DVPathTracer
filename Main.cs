using UnityModManagerNet;
using HarmonyLib;

namespace DVPathTracer
{
    public static class Main
    {
        public static bool enabled;
        public static Settings settings = new Settings();
        public static UnityModManager.ModEntry entry;
        internal static bool cclEnabled = false;

        /**
         * Mod entrypoint
         * Returns true if load is successful
         */
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            entry = modEntry;

            try
            {
                settings = Settings.Load<Settings>(modEntry);
                if (settings.version != modEntry.Info.Version)
                {
                    modEntry.Logger.Log("Settings is out of date. Creating new");
                    settings = new Settings();
                    settings.version = modEntry.Info.Version;
                }
                if (settings.forceStartInactive)
                {
                    settings.isActive = false;
                }
                modEntry.Logger.Log("Settings loaded");
            }
            catch
            {
                modEntry.Logger.Log("Could not load settings. Creating new");
                settings.version = modEntry.Info.Version;
            }

            try
            {
                if (UnityModManager.FindMod("Mph").Loaded)
                {
                    modEntry.Logger.Log("MPH mod is active, recording speeds in mph");
                    settings.mph = true;
                }
                else if (settings.mph)
                {
                    modEntry.Logger.Log("MPH mod is not active but speeds are still being recorded in mph");
                }
            }
            catch
            {
                // MPH mod is not installed, UMM reports this itself
                if (settings.mph)
                {
                    modEntry.Logger.Log("Speeds are being recorded in mph");
                }
            }

            try
            {
                if (UnityModManager.FindMod("DVCustomCarLoader").Loaded)
                {
                    modEntry.Logger.Log("CCL mod is active");
                    cclEnabled = true;
                }
            }
            catch
            {
                // CCL is not installed
            }

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            modEntry.OnGUI = settings.Draw;
            
            modEntry.OnSaveGUI = settings.Save;

            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;
            modEntry.OnUpdate = OnUpdate;

            return true;
        }

        /**
         * Toggle enabled state
         */
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            return true;
        }

        /**
         * Unpatch the mod (hopefully) safely
         */
        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        /**
         * Activate/deactivate the tracer appropriately
         */
        public static void OnUpdate(UnityModManager.ModEntry _, float __)
        {
            if (settings.isActive)
            {
                if (!BaseReporter.isActive && BaseReporter.isReady)
                {
                    BaseReporter.Activate();
                }
            }
            else
            {
                if (BaseReporter.isActive)
                {
                    BaseReporter.Deactivate();
                }
            }

            BaseReporter.TimedReport();
        }

        /**
         * Patch the PlayerManager to let LocationReporter know when it's ready
         * There's probably a better way to do this... entire mod
         */
        [HarmonyPatch(typeof(PlayerManager), "SetPlayer")]
        public static class PlayerManagerPatch
        {
            static void Postfix()
            {
                BaseReporter.ManagerIsSet();
            }
        }

        /**
         * Log to Mod Manager console (& log file)
         */
        public static void Log(string message)
        {
            entry.Logger.Log(message);
        }
    }
}
