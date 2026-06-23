using System;
using System.Reflection;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class GameConsoleBridge
    {
        private static bool _searched;
        private static object _handlerInstance;
        private static MethodInfo _onConsoleUserInput;

        public static bool TryExecute(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                TerminalManager.WriteLine("No game console command provided.");
                return true;
            }

            EnsureResolved();

            if (_handlerInstance == null || _onConsoleUserInput == null)
            {
                TerminalManager.WriteLine("Game console bridge is not connected yet.");
                TerminalManager.WriteLine("Command not sent: " + command);
                return true;
            }

            try
            {
                if (!command.StartsWith("/"))
                    command = "/" + command;

                _onConsoleUserInput.Invoke(_handlerInstance, new object[] { command });
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

            if (_handlerInstance == null || _onConsoleUserInput == null)
                return "Game console bridge: not connected";

            return "Game console bridge: UI.Console.ConsoleCommandHandler.OnConsoleUserInput";
        }

        public static void DebugSearchCandidates()
        {
            EnsureResolved();
            TerminalManager.WriteLine(GetStatus());
        }

        private static void EnsureResolved()
        {
            if (_searched && _handlerInstance != null && _onConsoleUserInput != null)
                return;

            _searched = true;
            ResolveConsoleExecutor();
        }

        private static void ResolveConsoleExecutor()
        {
            Type handlerType = Type.GetType("UI.Console.ConsoleCommandHandler, Assembly-CSharp");

            if (handlerType == null)
                return;

            _handlerInstance = UnityEngine.Object.FindObjectOfType(handlerType);

            if (_handlerInstance == null)
                return;

            _onConsoleUserInput = handlerType.GetMethod(
                "OnConsoleUserInput",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }
    }
}