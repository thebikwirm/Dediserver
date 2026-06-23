using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class RconServer
    {
        private static DedicatedServerConfig _config;
        private static TcpListener _listener;
        private static Thread _listenerThread;
        private static bool _running;
        private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private static DateTime _startedAt;

        public static void Init(DedicatedServerConfig config)
        {
            _config = config;
            _startedAt = DateTime.Now;

            if (config == null || !config.EnableRcon)
                return;

            try
            {
                IPAddress bindAddress = IPAddress.Parse(string.IsNullOrWhiteSpace(config.RconBindAddress) ? "127.0.0.1" : config.RconBindAddress);
                _listener = new TcpListener(bindAddress, config.RconPort);
                _listener.Start();

                _running = true;
                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "DedicatedHostRcon"
                };
                _listenerThread.Start();

                DedicatedHostManager.Log("RCON listening on " + bindAddress + ":" + config.RconPort);
                TerminalManager.WriteLine("RCON listening on " + bindAddress + ":" + config.RconPort);
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("Failed to start RCON: " + ex);
            }
        }

        public static void Update()
        {
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    DedicatedHostManager.LogError("RCON action failed: " + ex);
                }
            }
        }

        public static void Shutdown()
        {
            _running = false;

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }
        }

        private static void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => ClientLoop(client))
                    {
                        IsBackground = true,
                        Name = "DedicatedHostRconClient"
                    };
                    clientThread.Start();
                }
                catch
                {
                    if (_running)
                        Thread.Sleep(250);
                }
            }
        }

        private static void ClientLoop(TcpClient client)
        {
            bool authed = false;

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    writer.WriteLine("DEDISERVER RCON READY");
                    writer.WriteLine("AUTH <password> required");

                    string line;
                    while (_running && (line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0)
                            continue;

                        string response = HandleCommand(line, ref authed);
                        writer.WriteLine(response);

                        if (response == "BYE")
                            break;
                    }
                }
            }
            catch
            {
            }
        }

        private static string HandleCommand(string line, ref bool authed)
        {
            string lower = line.ToLowerInvariant();

            if (lower.StartsWith("auth "))
            {
                string password = line.Substring(5).Trim();
                if (string.Equals(password, _config.RconPassword ?? string.Empty, StringComparison.Ordinal))
                {
                    authed = true;
                    return "OK AUTH";
                }

                return "ERR AUTH FAILED";
            }

            if (!authed)
                return "ERR NOT AUTHENTICATED";

            if (lower == "help")
                return "OK COMMANDS: PING STATUS SAVE RESTART SHUTDOWN GAME <command> QUIT";

            if (lower == "ping")
                return "OK PONG";

            if (lower == "status")
                return BuildStatus();

            if (lower == "save")
            {
                _mainThreadActions.Enqueue(() => DedicatedHostManager.RequestSave("rcon command"));
                return "OK SAVE QUEUED";
            }

            if (lower == "restart")
            {
                _mainThreadActions.Enqueue(() => DedicatedHostManager.RequestRestart("rcon command"));
                return "OK RESTART QUEUED";
            }

            if (lower == "shutdown" || lower == "stop")
            {
                _mainThreadActions.Enqueue(() => DedicatedHostManager.RequestShutdown());
                return "OK SHUTDOWN QUEUED";
            }

            if (lower.StartsWith("game "))
            {
                string gameCommand = line.Substring(5).Trim();
                _mainThreadActions.Enqueue(() => GameConsoleBridge.TryExecute(gameCommand));
                return "OK GAME COMMAND QUEUED";
            }

            if (lower == "quit" || lower == "exit")
                return "BYE";

            return "ERR UNKNOWN COMMAND";
        }

        private static string BuildStatus()
        {
            DedicatedServerConfig config = DedicatedHostManager.Config;
            TimeSpan uptime = DateTime.Now - _startedAt;

            return "OK STATUS " +
                   "dedicated=" + DedicatedHostManager.IsDedicated + ";" +
                   "serverName=" + Safe(config != null ? config.ServerName : null) + ";" +
                   "saveName=" + Safe(config != null ? config.SaveName : null) + ";" +
                   "uptimeSeconds=" + (int)uptime.TotalSeconds + ";" +
                   "batchMode=" + Application.isBatchMode + ";" +
                   "targetFps=" + Application.targetFrameRate;
        }

        private static string Safe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
