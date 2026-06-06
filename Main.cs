using HarmonyLib;
using RailroaderDedicatedHost;
using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace Dediserver
{
    public static class Main
    {
        public static bool enabled;
        private static DedicatedServerConfig _config;
        private static Harmony _harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            File.AppendAllText(
                @"D:\deditest.txt",
                "LOAD CALLED " + DateTime.Now + Environment.NewLine
            );

            enabled = true;

            modEntry.Logger.Log("[DedicatedHost] Main.Load entered.");

            _config = DedicatedServerConfig.Load(modEntry);
            DedicatedHostManager.Init(modEntry, _config);

            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll();

            modEntry.OnUpdate = OnUpdate;
            modEntry.OnUnload = OnUnload;

            modEntry.Logger.Log("[DedicatedHost] Loaded.");
            return true;
        }

        public static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            DedicatedHostManager.Update(deltaTime);
        }

        public static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                _harmony?.UnpatchAll(modEntry.Info.Id);
                TerminalManager.Shutdown();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to unload cleanly: " + ex);
            }

            enabled = false;
            return true;
        }

        public static void OnGUI()
        {
        }
    }
}
