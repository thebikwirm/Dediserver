using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class RestartManager
    {
        private static DedicatedServerConfig _config;
        private static string _modRestartFlagPath;
        private static string _gameRestartFlagPath;
        private static string _modShutdownFlagPath;
        private static string _gameShutdownFlagPath;
        private static DateTime _startedAt;
        private static DateTime? _nextRestartAt;
        private static readonly HashSet<int> _warningsSent = new HashSet<int>();
        private static bool _restartRequested;

        public static void Init(DedicatedServerConfig config, string modPath)
        {
            _config = config;
            _startedAt = DateTime.Now;
            _modRestartFlagPath = Path.Combine(modPath, "restart.flag");
            _gameRestartFlagPath = DedicatedPaths.ResolveGamePath("restart.flag");
            _modShutdownFlagPath = Path.Combine(modPath, "shutdown.flag");
            _gameShutdownFlagPath = DedicatedPaths.ResolveGamePath("shutdown.flag");
            _warningsSent.Clear();
            _restartRequested = false;
            _nextRestartAt = CalculateNextRestartTime(DateTime.Now);

            DedicatedHostManager.Log("Restart flag path: " + _gameRestartFlagPath);
            DedicatedHostManager.Log("Shutdown flag path: " + _gameShutdownFlagPath);

            if (_nextRestartAt.HasValue)
            {
                DedicatedHostManager.Log("Next automatic restart: " + _nextRestartAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public static void Update()
        {
            if (_config == null || _restartRequested || !_nextRestartAt.HasValue)
                return;

            DateTime now = DateTime.Now;
            TimeSpan remaining = _nextRestartAt.Value - now;

            SendWarnings(remaining);

            if (remaining.TotalSeconds <= 0.0)
            {
                RequestRestart("scheduled restart");
            }
        }

        public static void RequestRestart(string reason)
        {
            if (_restartRequested)
                return;

            _restartRequested = true;

            WriteFlag(_modRestartFlagPath);
            WriteFlag(_gameRestartFlagPath);
            DeleteFlag(_modShutdownFlagPath);
            DeleteFlag(_gameShutdownFlagPath);

            DedicatedHostManager.Log("Restart requested: " + reason);
            TerminalManager.WriteLine("Restart requested: " + reason);
            DedicatedHostManager.RequestShutdown(saveBeforeQuit: true, writeShutdownFlag: false);
        }

        public static void RequestCleanShutdown(string reason)
        {
            WriteFlag(_modShutdownFlagPath);
            WriteFlag(_gameShutdownFlagPath);
            DeleteFlag(_modRestartFlagPath);
            DeleteFlag(_gameRestartFlagPath);

            DedicatedHostManager.Log("Clean shutdown requested: " + reason);
            TerminalManager.WriteLine("Clean shutdown requested: " + reason);
        }

        public static string GetStatus()
        {
            string flagInfo = "Restart flag: " + _gameRestartFlagPath + "; Shutdown flag: " + _gameShutdownFlagPath;

            if (!_nextRestartAt.HasValue)
                return "Automatic restart: disabled. " + flagInfo;

            TimeSpan remaining = _nextRestartAt.Value - DateTime.Now;
            if (remaining.TotalSeconds < 0.0)
                remaining = TimeSpan.Zero;

            return "Next restart: " + _nextRestartAt.Value.ToString("yyyy-MM-dd HH:mm:ss") +
                   " (in " + FormatDuration(remaining) + "). " + flagInfo;
        }

        private static void WriteFlag(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("Failed to write flag " + path + ": " + ex);
            }
        }

        private static void DeleteFlag(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void SendWarnings(TimeSpan remaining)
        {
            if (_config.RestartWarningMinutes == null)
                return;

            foreach (int warningMinute in _config.RestartWarningMinutes.OrderByDescending(x => x))
            {
                if (warningMinute <= 0 || _warningsSent.Contains(warningMinute))
                    continue;

                double warningSeconds = warningMinute * 60.0;

                if (remaining.TotalSeconds <= warningSeconds && remaining.TotalSeconds > 0.0)
                {
                    _warningsSent.Add(warningMinute);
                    string msg = "Server restart in " + warningMinute + " minute" + (warningMinute == 1 ? "" : "s") + ".";
                    DedicatedHostManager.Log(msg);
                    TerminalManager.WriteLine(msg);
                }
            }
        }

        private static DateTime? CalculateNextRestartTime(DateTime now)
        {
            List<DateTime> candidates = new List<DateTime>();

            if (_config.RestartEveryHours > 0f)
            {
                candidates.Add(_startedAt.AddHours(_config.RestartEveryHours));
            }

            if (_config.RestartAtLocalTimes != null)
            {
                foreach (string timeText in _config.RestartAtLocalTimes)
                {
                    if (string.IsNullOrWhiteSpace(timeText))
                        continue;

                    if (!TimeSpan.TryParseExact(timeText.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan timeOfDay) &&
                        !TimeSpan.TryParseExact(timeText.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out timeOfDay))
                    {
                        DedicatedHostManager.LogError("Invalid restart time: " + timeText + " expected HH:mm");
                        continue;
                    }

                    DateTime candidate = now.Date.Add(timeOfDay);
                    if (candidate <= now)
                        candidate = candidate.AddDays(1);

                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
                return null;

            return candidates.OrderBy(x => x).First();
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalHours >= 1.0)
                return string.Format(CultureInfo.InvariantCulture, "{0:0}h {1:00}m", Math.Floor(span.TotalHours), span.Minutes);

            if (span.TotalMinutes >= 1.0)
                return string.Format(CultureInfo.InvariantCulture, "{0:0}m {1:00}s", Math.Floor(span.TotalMinutes), span.Seconds);

            return string.Format(CultureInfo.InvariantCulture, "{0:0}s", Math.Max(0.0, span.TotalSeconds));
        }
    }
}
