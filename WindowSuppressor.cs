using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public static class WindowSuppressor
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SW_MINIMIZE = 6;
        private const int SW_HIDE = 0;

        private static UnityModManager.ModEntry _modEntry;
        private static bool _active;
        private static int _command;
        private static string _action;
        private static float _retryTimer;
        private static float _elapsed;
        private static bool _loggedWaiting;
        private static int _successfulApplies;

        public static void Minimize(UnityModManager.ModEntry modEntry)
        {
            StartSuppression(modEntry, SW_MINIMIZE, "minimized");
        }

        public static void Hide(UnityModManager.ModEntry modEntry)
        {
            StartSuppression(modEntry, SW_HIDE, "hidden");
        }

        public static void Tick()
        {
            if (!_active)
                return;

            _elapsed += UnityEngine.Time.unscaledDeltaTime;
            _retryTimer -= UnityEngine.Time.unscaledDeltaTime;

            if (_retryTimer > 0f)
                return;

            _retryTimer = 0.5f;
            TryApply();

            if (_elapsed > 60f)
            {
                _active = false;
                _modEntry?.Logger.Log("[DedicatedHost] Finished startup window suppression. Applies=" + _successfulApplies);
                DumpOwnedWindows();
            }
        }

        private static void StartSuppression(UnityModManager.ModEntry modEntry, int command, string action)
        {
            _modEntry = modEntry;
            _command = command;
            _action = action;
            _active = true;
            _retryTimer = 0f;
            _elapsed = 0f;
            _loggedWaiting = false;
            _successfulApplies = 0;
            TryApply();
        }

        private static void TryApply()
        {
            try
            {
                List<IntPtr> handles = FindUnityGameWindows();

                if (handles.Count == 0)
                {
                    if (!_loggedWaiting)
                    {
                        _loggedWaiting = true;
                        _modEntry?.Logger.Warning("[DedicatedHost] Waiting for Railroader game window. Console will not be hidden.");
                    }
                    return;
                }

                foreach (IntPtr handle in handles)
                {
                    ShowWindow(handle, _command);
                    _successfulApplies++;
                }

                if (_successfulApplies == handles.Count)
                {
                    _modEntry?.Logger.Log("[DedicatedHost] Game window " + _action + ". Continuing to enforce during startup.");
                }
            }
            catch (Exception ex)
            {
                _active = false;
                _modEntry?.Logger.Error("[DedicatedHost] Failed to change game window visibility: " + ex);
            }
        }

        private static List<IntPtr> FindUnityGameWindows()
        {
            uint currentPid = (uint)Process.GetCurrentProcess().Id;
            List<IntPtr> handles = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);

                if (pid != currentPid)
                    return true;

                string className = GetClassNameSafe(hWnd);
                string title = GetWindowTextSafe(hWnd);

                if (string.Equals(className, "ConsoleWindowClass", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (className.IndexOf("Unity", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    handles.Add(hWnd);
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(title) &&
                    title.IndexOf("Railroader", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    handles.Add(hWnd);
                    return true;
                }

                return true;
            }, IntPtr.Zero);

            return handles;
        }

        private static void DumpOwnedWindows()
        {
            try
            {
                uint currentPid = (uint)Process.GetCurrentProcess().Id;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid != currentPid)
                        return true;

                    string className = GetClassNameSafe(hWnd);
                    string title = GetWindowTextSafe(hWnd);
                    bool visible = IsWindowVisible(hWnd);

                    _modEntry?.Logger.Log("[DedicatedHost] Window dump: class='" + className + "' title='" + title + "' visible=" + visible);
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
        }

        private static string GetClassNameSafe(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            int length = GetClassName(hWnd, sb, sb.Capacity);
            return length > 0 ? sb.ToString() : string.Empty;
        }

        private static string GetWindowTextSafe(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            int length = GetWindowText(hWnd, sb, sb.Capacity);
            return length > 0 ? sb.ToString() : string.Empty;
        }
    }
}
