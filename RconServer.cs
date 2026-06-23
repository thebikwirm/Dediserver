using Game.State;
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

            DedicatedHostManager.Log("RCON init called.");

            if (config == null)
            {
                DedicatedHostManager.LogError("RCON config is null.");
                return;
            }

            DedicatedHostManager.Log(
                "RCON config: EnableRcon=" + config.EnableRcon +
                " Bind=" + config.RconBindAddress +
                " Port=" + config.RconPort +
                " PasswordSet=" + !string.IsNullOrEmpty(config.RconPassword));

            if (!config.EnableRcon)
            {
                DedicatedHostManager.Log("RCON disabled by config.");
                return;
            }

            if (_running)
            {
                DedicatedHostManager.Log("RCON already running.");
                return;
            }

            try
            {
                string bindText = string.IsNullOrWhiteSpace(config.RconBindAddress) ? "127.0.0.1" : config.RconBindAddress.Trim();
                IPAddress bindAddress;

                if (!IPAddress.TryParse(bindText, out bindAddress))
                {
                    DedicatedHostManager.LogError("Invalid RCON bind address: " + bindText);
                    return;
                }

                if (config.RconPort <= 0 || config.RconPort > 65535)
                {
                    DedicatedHostManager.LogError("Invalid RCON port: " + config.RconPort);
                    return;
                }

                _listener = new TcpListener(bindAddress, config.RconPort);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
                _running = false;
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
                DedicatedHostManager.Log("RCON stopped.");
            }
            catch
            {
            }
        }

        private static void ListenLoop()
        {
            DedicatedHostManager.Log("RCON listener thread started.");

            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    client.NoDelay = true;
                    client.ReceiveTimeout = 30000;
                    client.SendTimeout = 30000;

                    string remote = "unknown";
                    try
                    {
                        remote = client.Client.RemoteEndPoint != null ? client.Client.RemoteEndPoint.ToString() : "unknown";
                    }
                    catch
                    {
                    }

                    DedicatedHostManager.Log("RCON client connected: " + remote);

                    Thread clientThread = new Thread(() => ClientLoop(client))
                    {
                        IsBackground = true,
                        Name = "DedicatedHostRconClient"
                    };
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        DedicatedHostManager.LogError("RCON accept failed: " + ex.Message);
                        Thread.Sleep(250);
                    }
                }
            }

            DedicatedHostManager.Log("RCON listener thread stopped.");
        }

        private static void ClientLoop(TcpClient client)
        {
            bool authed = false;

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
                {
                    writer.WriteLine("DEDISERVER RCON READY");
                    writer.WriteLine("AUTH <password> required");

                    string line;
                    while (_running && (line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0)
                            continue;

                        DedicatedHostManager.Log("RCON command received: " + Redact(line));

                        string response = HandleCommand(line, ref authed);
                        writer.WriteLine(response);

                        if (response == "BYE")
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("RCON client error: " + ex.Message);
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
                return BuildHelp();

            if (lower == "ping")
                return "OK PONG";

            if (lower == "status")
                return BuildStatus();

            if (lower == "uptime")
                return BuildUptime();

            if (lower == "version")
                return BuildVersion();

            if (lower == "players")
                return BuildPlayers();

            if (lower == "save")
            {
                _mainThreadActions.Enqueue(() => DedicatedHostManager.RequestSave("rcon command"));
                return "OK SAVE QUEUED";
            }

            if (lower == "saveandrestart" || lower == "save_restart" || lower == "save-restart")
            {
                _mainThreadActions.Enqueue(() =>
                {
                    DedicatedHostManager.RequestSave("rcon saveandrestart command");
                    DedicatedHostManager.RequestRestart("rcon saveandrestart command");
                });
                return "OK SAVEANDRESTART QUEUED";
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

        private static string BuildHelp()
        {
            return "OK HELP\n" +
                   "AUTH <password>\n" +
                   "PING\n" +
                   "STATUS\n" +
                   "UPTIME\n" +
                   "VERSION\n" +
                   "PLAYERS\n" +
                   "SAVE\n" +
                   "SAVEANDRESTART\n" +
                   "RESTART\n" +
                   "SHUTDOWN\n" +
                   "GAME <command>\n" +
                   "QUIT";
        }

        private static string BuildStatus()
        {
            DedicatedServerConfig config = DedicatedHostManager.Config;
            TimeSpan uptime = DateTime.Now - _startedAt;

            return "OK STATUS " +
                   "dedicated=" + DedicatedHostManager.IsDedicated + ";" +
                   "serverName=" + Safe(config != null ? config.ServerName : null) + ";" +
                   "saveName=" + Safe(config != null ? config.SaveName : null) + ";" +
                   "players=" + GetPlayerCountSafe() + ";" +
                   "uptimeSeconds=" + (int)uptime.TotalSeconds + ";" +
                   "version=" + Safe(GetVersionString()) + ";" +
                   "batchMode=" + Application.isBatchMode + ";" +
                   "targetFps=" + Application.targetFrameRate;
        }

        private static string BuildUptime()
        {
            return "OK UPTIME " + (int)(DateTime.Now - _startedAt).TotalSeconds;
        }

        private static string BuildVersion()
        {
            return "OK VERSION " + GetVersionString();
        }

        private static string BuildPlayers()
        {
            try
            {
                StateManager state = StateManager.Shared;
                if (state == null || state.PlayersManager == null)
                    return "OK PLAYERS 0";

                StringBuilder sb = new StringBuilder();
                int count = 0;

                foreach (IPlayer player in state.PlayersManager.AllPlayers)
                {
                    if (player == null)
                        continue;

                    count++;
                    sb.AppendLine(Safe(player.Name));
                }

                string playersText = sb.ToString().TrimEnd('\r', '\n');
                if (playersText.Length == 0)
                    return "OK PLAYERS " + count;

                return "OK PLAYERS " + count + "\n" + playersText;
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("PLAYERS failed: " + ex);
                return "ERR PLAYERS FAILED";
            }
        }

        private static int GetPlayerCountSafe()
        {
            try
            {
                StateManager state = StateManager.Shared;
                if (state == null || state.PlayersManager == null)
                    return 0;

                int count = 0;
                foreach (IPlayer player in state.PlayersManager.AllPlayers)
                {
                    if (player != null)
                        count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetVersionString()
        {
            try
            {
                Version version = typeof(RconServer).Assembly.GetName().Version;
                return version != null ? version.ToString() : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private static string Safe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }

        private static string Redact(string line)
        {
            if (line == null)
                return string.Empty;

            if (line.StartsWith("auth ", StringComparison.OrdinalIgnoreCase))
                return "AUTH <redacted>";

            return line;
        }
    }
}
