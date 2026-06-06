using Game.Persistence;
using System;
using UnityEngine;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public static class DedicatedHostManager
    {
        public static bool IsDedicated { get; private set; }
        public static DedicatedServerConfig Config => _config;

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

            Log("Dedicated mode enabled.");
            Log("BatchMode: " + Application.isBatchMode);
            Log("GraphicsDevice: " + SystemInfo.graphicsDeviceType);

            Application.runInBackground = true;
            Application.targetFrameRate = Mathf.Clamp(config.TargetServerFps, 5, 60);

            if (config.TerminalMode)
            {
                TerminalManager.Init(config);
            }

            if (config.HideGraphics)
            {
                GraphicsSuppressor.Apply(config, _modEntry);

                if (config.HideWindow)
                {
                    WindowSuppressor.Hide(_modEntry);
                }
                else if (config.MinimizeWindow)
                {
                    WindowSuppressor.Minimize(_modEntry);
                }
            }
        }

        public static void Update(float deltaTime)
        {
            if (!IsDedicated || _config == null)
                return;

            TerminalManager.Update();

            if (_config.HideGraphics)
                GraphicsSuppressor.Tick();

            if (_config.AutosaveSeconds > 0)
            {
                _autosaveTimer += deltaTime;

                if (_autosaveTimer >= _config.AutosaveSeconds)
                {
                    _autosaveTimer = 0f;
                    RequestSave("autosave timer");
                }
            }
        }

        public static void RequestSave(string reason)
        {
            try
            {
                if (_config == null || string.IsNullOrEmpty(_config.SaveName))
                {
                    LogError("Save requested but config/save name is missing.");
                    return;
                }

                Log("Save requested: " + reason);
                WorldStore.Save(_config.SaveName);
                Log("Saved world: " + _config.SaveName);
            }
            catch (Exception ex)
            {
                LogError("Save failed: " + ex);
            }
        }

        public static void RequestShutdown()
        {
            try
            {
                RequestSave("shutdown");
            }
            catch
            {
            }

            Log("Application quit requested.");
            Application.Quit();
        }

        public static void Log(string msg)
        {
            _modEntry?.Logger.Log("[DedicatedHost] " + msg);
        }

        public static void LogError(string msg)
        {
            _modEntry?.Logger.Error("[DedicatedHost] " + msg);
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
