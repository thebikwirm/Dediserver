using Game.Persistence;
using Network;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class TerminalManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        private static bool _initialized;
        private static Thread _inputThread;
        private static readonly object _lock = new object();
        private static string _pendingCommand;
        private static bool _running;

        public static void Init(DedicatedServerConfig config)
        {
            if (_initialized || config == null || !config.TerminalMode)
                return;

            _initialized = true;
            _running = true;

            if (config.AllocateConsoleWindow)
            {
                AllocConsole();
            }

            try
            {
                Console.Title = "Railroader Dedicated Host";
            }
            catch
            {
            }

            WriteLine("Railroader Dedicated Host");
            WriteLine("Type 'help' for commands.");
            WritePrompt();

            _inputThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "DedicatedHostTerminal"
            };
            _inputThread.Start();
        }

        public static void Update()
        {
            if (!_initialized)
                return;

            string command = null;

            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(_pendingCommand))
                {
                    command = _pendingCommand;
                    _pendingCommand = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(command))
            {
                Execute(command.Trim());
                WritePrompt();
            }
        }

        public static void Shutdown()
        {
            _running = false;
        }

        public static void WriteLine(string message)
        {
            try
            {
                Console.WriteLine("[DedicatedHost] " + message);
            }
            catch
            {
            }

            DedicatedHostManager.Log(message);
        }

        private static void ReadLoop()
        {
            while (_running)
            {
                try
                {
                    string line = Console.ReadLine();

                    if (line == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    lock (_lock)
                    {
                        _pendingCommand = line;
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static void Execute(string command)
        {
            string lower = command.ToLowerInvariant();

            if (lower == "help" || lower == "?")
            {
                WriteLine("Commands: help, status, save, shutdown, quit, exit");
                return;
            }

            if (lower == "status")
            {
                DedicatedServerConfig config = DedicatedHostManager.Config;
                WriteLine("Dedicated: " + DedicatedHostManager.IsDedicated);
                WriteLine("Save: " + (config != null ? config.SaveName : "<null>"));
                WriteLine("Server: " + (config != null ? config.ServerName : "<null>"));
                WriteLine("BatchMode: " + Application.isBatchMode);
                WriteLine("GraphicsDevice: " + SystemInfo.graphicsDeviceType);
                WriteLine("Multiplayer active: " + Multiplayer.IsClientActive);
                return;
            }

            if (lower == "save")
            {
                DedicatedHostManager.RequestSave("terminal command");
                return;
            }

            if (lower == "shutdown" || lower == "quit" || lower == "exit")
            {
                WriteLine("Shutdown requested.");
                DedicatedHostManager.RequestShutdown();
                return;
            }

            WriteLine("Unknown command: " + command);
        }

        private static void WritePrompt()
        {
            try
            {
                Console.Write("> ");
            }
            catch
            {
            }
        }
    }
}
