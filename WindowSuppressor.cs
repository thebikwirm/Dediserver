using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public static class WindowSuppressor
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;
        private const int SW_HIDE = 0;

        public static void Minimize(UnityModManager.ModEntry modEntry)
        {
            try
            {
                IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                if (handle == IntPtr.Zero)
                {
                    modEntry.Logger.Warning("[DedicatedHost] Could not find game window handle.");
                    return;
                }

                ShowWindow(handle, SW_MINIMIZE);
                modEntry.Logger.Log("[DedicatedHost] Window minimized.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to minimize window: " + ex);
            }
        }

        public static void Hide(UnityModManager.ModEntry modEntry)
        {
            try
            {
                IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

                if (handle == IntPtr.Zero)
                {
                    modEntry.Logger.Warning("[DedicatedHost] Could not find game window handle.");
                    return;
                }

                ShowWindow(handle, SW_HIDE);
                modEntry.Logger.Log("[DedicatedHost] Window hidden.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to hide window: " + ex);
            }
        }
    }
}