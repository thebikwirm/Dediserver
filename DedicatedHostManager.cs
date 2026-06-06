using System;
using UnityEngine;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public static class DedicatedHostManager
    {
        public static bool IsDedicated { get; private set; }

        private static UnityModManager.ModEntry _modEntry;
        private static DedicatedServerConfig _config;

        private static float _autosaveTimer;

        public static void Init(UnityModManager.ModEntry modEntry, DedicatedServerConfig config)
        {
            _modEntry = modEntry;
            _config = config;

            IsDedicated = config.Enabled || HasLaunchArg("-dedicated") || HasLaunchArg("-dedicated-server");

            if (!IsDedicated)
            {
                _modEntry.Logger.Log("[DedicatedHost] Dedicated mode disabled.");
                return;
            }

            _modEntry.Logger.Log("[DedicatedHost] Dedicated mode enabled.");

            Application.runInBackground = true;
            Application.targetFrameRate = Mathf.Clamp(config.TargetServerFps, 5, 60);

            if (config.HideGraphics)
                GraphicsSuppressor.Apply(config, _modEntry);
                WindowSuppressor.Minimize(_modEntry);
            // Later:
            // AutoHostManager.LoadSaveAndHost(config);
        }

        public static void Update(float deltaTime)
        {
            if (!IsDedicated || _config == null)
                return;

            if (_config.HideGraphics)
                GraphicsSuppressor.Tick();

            if (_config.AutosaveSeconds > 0)
            {
                _autosaveTimer += deltaTime;

                if (_autosaveTimer >= _config.AutosaveSeconds)
                {
                    _autosaveTimer = 0f;
                    TryAutosave();
                }
            }
        }

        private static void TryAutosave()
        {
            try
            {
                _modEntry.Logger.Log("[DedicatedHost] Autosave tick.");

                // TODO once we find Railroader save method:
                // SaveManager.SaveCurrentGame();
            }
            catch (Exception ex)
            {
                _modEntry.Logger.Error("[DedicatedHost] Autosave failed: " + ex);
            }
        }

        private static bool HasLaunchArg(string arg)
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (string a in args)
            {
                if (string.Equals(a, arg, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}