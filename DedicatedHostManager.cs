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
        private static bool _logging;

        public static void Init(UnityModManager.ModEntry modEntry, DedicatedServerConfig config)
        {
            _modEntry = modEntry;
            _config = config;

            IsDedicated = config.Enabled || HasLaunchArg("-dedicated") || HasLaunchArg("-dedicated-server");

            if (!IsDedicated)
            {
                Log("Dedicated mode disabled.");
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

            RestartManager.Init(config, modEntry.Path);
            LogTailer.Init(config);
            RconServer.Init(config);

            if (config.HideGraphics)
            {
                GraphicsSuppressor.Apply(config, _modEntry);
            }

            if (config.HideWindow)
            {
                WindowSuppressor.Hide(_modEntry);
            }
            else if (config.MinimizeWindow)
            {
                WindowSuppressor.Minimize(_modEntry);
            }
        }

        public static void Update(float deltaTime)
        {
            if (!IsDedicated || _config == null)
                return;

            TerminalManager.Update();
            RestartManager.Update();
            LogTailer.Update(deltaTime);
            RconServer.Update();

            if (_config.HideGraphics)
                GraphicsSuppressor.Tick();

            if (_config.HideWindow || _config.MinimizeWindow)
                WindowSuppressor.Tick();

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

        public static void RequestShutdown(bool saveBeforeQuit = true, bool writeShutdownFlag = true)
        {
            if (writeShutdownFlag)
            {
                RestartManager.RequestCleanShutdown("shutdown");
            }

            if (saveBeforeQuit)
            {
                try
                {
                    RequestSave("shutdown");
                }
                catch
                {
                }
            }

            Log("Application quit requested.");
            RconServer.Shutdown();
            TerminalManager.Shutdown();
            Application.Quit();
        }

        public static void RequestRestart(string reason)
        {
            RestartManager.RequestRestart(reason);
        }

        public static void Log(string msg)
        {
            SafeLog(msg, false);
        }

        public static void LogError(string msg)
        {
            SafeLog(msg, true);
        }

        private static void SafeLog(string msg, bool error)
        {
            if (_logging || _modEntry == null)
                return;

            try
            {
                _logging = true;

                string prefix = "[DedicatedHost] ";
                while (!string.IsNullOrEmpty(msg) && msg.StartsWith(prefix, StringComparison.Ordinal))
                    msg = msg.Substring(prefix.Length);

                msg = prefix + msg;

                if (error)
                    _modEntry.Logger.Error(msg);
                else
                    _modEntry.Logger.Log(msg);
            }
            catch
            {
            }
            finally
            {
                _logging = false;
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
