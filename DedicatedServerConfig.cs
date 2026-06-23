using Newtonsoft.Json;
using System;
using System.IO;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public class DedicatedServerConfig
    {
        public bool Enabled = false;

        // Existing saves only. Do not include .shortsave.
        public string SaveName = "DedicatedServerSave";
        public string ServerName = "Railroader Dedicated Host";
        public string Password = "";
        public int Port = 7777;

        // Dedicated v2: keep UI systems alive for compatibility, but do not require the UI to be visible.
        public bool RequireExistingSave = true;
        public bool NoGuiMode = true;
        public bool RequireBatchModeArgs = false;
        public bool TerminalMode = true;
        public bool AllocateConsoleWindow = true;

        // If true, wrapper scripts should close the terminal on normal server stop.
        // If false, wrapper scripts can pause so you can read the final output.
        public bool CloseTerminalOnStop = true;

        // Realtime console log mirror. Set path relative to Railroader.exe, or use an absolute path.
        public bool MirrorLogToTerminal = true;
        public string MirrorLogPath = "railloader.log";
        public bool MirrorLogFromStart = false;
        public bool MirrorOwnDedicatedLogs = true;
        public float MirrorLogPollSeconds = 0.25f;
        public int MirrorLogMaxLinesPerPoll = 50;

        // Fallback when Unity still creates a window. This does not disable Canvas or Camera objects.
        public bool HideGraphics = true;
        public bool MinimizeWindow = false;
        public bool HideWindow = true;
        public bool AggressiveGraphicsDisable = false;

        public bool MuteAudio = true;
        public float AudioVolume = 0f;

        public int TargetServerFps = 20;
        public int AutosaveSeconds = 300;

        // Restart support. RestartEveryHours <= 0 disables interval restarts.
        public float RestartEveryHours = 0f;

        // Local 24-hour times, e.g. "04:00" or "16:30". Empty disables fixed-time restarts.
        public string[] RestartAtLocalTimes = new string[0];

        // Warning times before restart, in minutes.
        public int[] RestartWarningMinutes = new int[] { 15, 5, 1 };

        public static DedicatedServerConfig Load(UnityModManager.ModEntry modEntry)
        {
            string path = Path.Combine(modEntry.Path, "dedicated_host.json");

            if (!File.Exists(path))
            {
                var cfg = new DedicatedServerConfig();
                File.WriteAllText(path, JsonConvert.SerializeObject(cfg, Formatting.Indented));
                modEntry.Logger.Log("[DedicatedHost] Created default dedicated_host.json");
                return cfg;
            }

            try
            {
                return JsonConvert.DeserializeObject<DedicatedServerConfig>(
                    File.ReadAllText(path)
                ) ?? new DedicatedServerConfig();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to load config: " + ex);
                return new DedicatedServerConfig();
            }
        }
    }
}
