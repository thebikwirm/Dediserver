using System;
using System.IO;
using System.Text;

namespace RailroaderDedicatedHost
{
    public static class LogTailer
    {
        private static DedicatedServerConfig _config;
        private static string _logPath;
        private static long _position;
        private static float _timer;
        private static bool _initialized;
        private static bool _warnedMissing;

        public static void Init(DedicatedServerConfig config)
        {
            _config = config;
            _initialized = false;
            _warnedMissing = false;
            _position = 0L;
            _timer = 0f;

            if (config == null || !config.MirrorLogToTerminal)
                return;

            _logPath = ResolveLogPath(config.MirrorLogPath);
            DedicatedHostManager.Log("Log mirror path: " + _logPath);
        }

        public static void Update(float deltaTime)
        {
            if (_config == null || !_config.MirrorLogToTerminal || string.IsNullOrEmpty(_logPath))
                return;

            _timer += deltaTime;
            if (_timer < Math.Max(0.1f, _config.MirrorLogPollSeconds))
                return;

            _timer = 0f;
            Poll();
        }

        private static void Poll()
        {
            try
            {
                if (!File.Exists(_logPath))
                {
                    if (!_warnedMissing)
                    {
                        _warnedMissing = true;
                        TerminalManager.WriteLine("Log mirror waiting for file: " + _logPath);
                    }
                    return;
                }

                _warnedMissing = false;

                FileInfo info = new FileInfo(_logPath);

                if (!_initialized)
                {
                    _initialized = true;
                    _position = _config.MirrorLogFromStart ? 0L : info.Length;
                    TerminalManager.WriteLine("Log mirror attached: " + _logPath);
                    return;
                }

                if (info.Length < _position)
                    _position = 0L;

                if (info.Length == _position)
                    return;

                using (FileStream fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    fs.Seek(_position, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8, true))
                    {
                        string line;
                        int lines = 0;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!ShouldSkip(line))
                            {
                                TerminalManager.WriteRawLine(line);
                                lines++;
                            }

                            if (lines >= Math.Max(1, _config.MirrorLogMaxLinesPerPoll))
                                break;
                        }

                        _position = fs.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("Log mirror failed: " + ex.Message);
            }
        }

        private static bool ShouldSkip(string line)
        {
            if (string.IsNullOrEmpty(line))
                return true;

            if (!_config.MirrorOwnDedicatedLogs)
            {
                if (line.IndexOf("[DedicatedHost]", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (line.IndexOf("[DediServer]", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string ResolveLogPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = "railloader.log";

            return DedicatedPaths.ResolveGamePath(path);
        }
    }
}
