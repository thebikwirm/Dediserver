using System;
using System.Linq;
using System.Reflection;

namespace RailroaderDedicatedHost
{
    public static class GameConsoleBridge
    {
        private static bool _searched;
        private static MethodInfo _executeMethod;
        private static object _targetInstance;
        private static string _methodDescription;

        public static bool TryExecute(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                TerminalManager.WriteLine("No game console command provided.");
                return true;
            }

            EnsureResolved();

            if (_executeMethod == null)
            {
                TerminalManager.WriteLine("Game console bridge is not connected yet.");
                TerminalManager.WriteLine("Command not sent: " + command);
                TerminalManager.WriteLine("Use findconsole, then send me the candidate output.");
                return true;
            }

            try
            {
                _executeMethod.Invoke(_targetInstance, new object[] { command });
                TerminalManager.WriteLine("Game command sent: " + command);
                return true;
            }
            catch (Exception ex)
            {
                TerminalManager.WriteLine("Game command failed: " + ex.GetBaseException().Message);
                return true;
            }
        }

        public static string GetStatus()
        {
            EnsureResolved();

            if (_executeMethod == null)
                return "Game console bridge: not connected";

            return "Game console bridge: " + _methodDescription;
        }

        public static void DebugSearchCandidates()
        {
            string[] typeHints = { "console", "command", "cheat", "debug" };
            string[] methodHints = { "execute", "submit", "run", "command", "process" };

            int found = 0;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    string typeName = type.FullName ?? type.Name;
                    string typeLower = typeName.ToLowerInvariant();
                    bool typeMatches = typeHints.Any(h => typeLower.Contains(h));

                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static |
                        BindingFlags.Instance))
                    {
                        string methodLower = method.Name.ToLowerInvariant();
                        bool methodMatches = methodHints.Any(h => methodLower.Contains(h));

                        if (!typeMatches && !methodMatches)
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                            continue;

                        TerminalManager.WriteLine(
                            "Console candidate: " +
                            typeName + "." +
                            method.Name +
                            "(string)"
                        );

                        found++;

                        if (found >= 50)
                        {
                            TerminalManager.WriteLine("Console candidate search stopped after 50 results.");
                            return;
                        }
                    }
                }
            }

            TerminalManager.WriteLine("Console candidate search complete. Found " + found + " candidates.");
        }

        private static void EnsureResolved()
        {
            if (_searched)
                return;

            _searched = true;
            ResolveConsoleExecutor();
        }

        private static void ResolveConsoleExecutor()
        {
            // TODO:
            // Once we know Railroader's real console command method,
            // wire it here.
            //
            // Example static method:
            //
            // Type type = AccessTools.TypeByName("Some.Console.Type");
            // _executeMethod = AccessTools.Method(type, "ExecuteCommand", new[] { typeof(string) });
            // _targetInstance = null;
            //
            // Example instance method:
            //
            // object instance = UnityEngine.Object.FindObjectOfType(type);
            // _executeMethod = AccessTools.Method(type, "ExecuteCommand", new[] { typeof(string) });
            // _targetInstance = instance;

            _executeMethod = null;
            _targetInstance = null;
            _methodDescription = null;
        }
    }
}