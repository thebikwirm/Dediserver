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
        private const uint AttachParentProcess = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        private static bool _initialized;
        private static Thread _inputThread;
        private static readonly object _lock = new object();
        private static string _pendingCommand;
        private static bool _running;
        private static bool _hasConsole;

        public static void Init(DedicatedServerConfig config)
        {
            if (_initialized || config == null || !config.TerminalMode)
                return;

            _initialized = true;
            _running = true;

            _hasConsole = TryAttachOrCreateConsole(config.AllocateConsoleWindow);

            if (_hasConsole)
            {
                try
                {
                    System.Console.Title = "Railroader Dedicated Host";
                }
                catch
                {
                }

                WriteLine("Railroader Dedicated Host");
                WriteLine("Type 'help' for commands.");
                WritePrompt();
            }

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
            if (!_hasConsole)
                return;

            try
            {
                System.Console.WriteLine("[DedicatedHost] " + message);
            }
            catch
            {
            }
        }

        private static bool TryAttachOrCreateConsole(bool allowAllocConsole)
        {
            bool attached = AttachConsole(AttachParentProcess);

            if (!attached && allowAllocConsole)
            {
                attached = AllocConsole();
            }

            if (!attached)
                return false;

            try
            {
                StreamWriter stdout = new StreamWriter(System.Console.OpenStandardOutput())
                {
                    AutoFlush = true
                };

                StreamWriter stderr = new StreamWriter(System.Console.OpenStandardError())
                {
                    AutoFlush = true
                };

                StreamReader stdin = new StreamReader(System.Console.OpenStandardInput());

                System.Console.SetOut(stdout);
                System.Console.SetError(stderr);
                System.Console.SetIn(stdin);
            }
            catch
            {
            }

            return true;
        }

        private static void ReadLoop()
        {
            while (_running)
            {
                try
                {
                    if (!_hasConsole)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    string line = System.Console.ReadLine();

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
                WriteLine("Commands: help, status, save, restart, restartstatus, shutdown, quit, exit");
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
                WriteLine(RestartManager.GetStatus());
                return;
            }

            if (lower == "restartstatus")
            {
                WriteLine(RestartManager.GetStatus());
                return;
            }

            if (lower == "save")
            {
                DedicatedHostManager.RequestSave("terminal command");
                return;
            }

            if (lower == "restart")
            {
                DedicatedHostManager.RequestRestart("terminal command");
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
            if (!_hasConsole)
                return;

            try
            {
                System.Console.Write("> ");
            }
            catch
            {
            }
        }
    }
}
