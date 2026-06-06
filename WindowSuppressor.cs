using System;
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

        public static void Minimize(UnityModManager.ModEntry modEntry)
        {
            Apply(modEntry, SW_MINIMIZE, "minimized");
        }

        public static void Hide(UnityModManager.ModEntry modEntry)
        {
            Apply(modEntry, SW_HIDE, "hidden");
        }

        private static void Apply(UnityModManager.ModEntry modEntry, int command, string action)
        {
            try
            {
                IntPtr handle = FindUnityGameWindow();

                if (handle == IntPtr.Zero)
                {
                    modEntry.Logger.Warning("[DedicatedHost] Could not find Railroader game window handle. Console was not hidden.");
                    return;
                }

                ShowWindow(handle, command);
                modEntry.Logger.Log("[DedicatedHost] Game window " + action + ".");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to change game window visibility: " + ex);
            }
        }

        private static IntPtr FindUnityGameWindow()
        {
            uint currentPid = (uint)Process.GetCurrentProcess().Id;
            IntPtr bestHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);

                if (pid != currentPid)
                    return true;

                string className = GetClassNameSafe(hWnd);
                string title = GetWindowTextSafe(hWnd);

                if (string.Equals(className, "ConsoleWindowClass", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(className, "UnityWndClass", StringComparison.OrdinalIgnoreCase))
                {
                    bestHandle = hWnd;
                    return false;
                }

                if (IsWindowVisible(hWnd) && !string.IsNullOrWhiteSpace(title) &&
                    title.IndexOf("Railroader", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestHandle = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return bestHandle;
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
