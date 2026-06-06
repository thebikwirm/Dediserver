using HarmonyLib;
using RailroaderDedicatedHost;
using System;
using UnityEngine;
using UnityModManagerNet;

namespace Dediserver
{
    static class Main
    {
        public static bool enabled;
        private static DedicatedServerConfig _config;
        private static Harmony _harmony;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            enabled = true;

            _config = DedicatedServerConfig.Load(modEntry);
            DedicatedHostManager.Init(modEntry, _config);

            _harmony = new Harmony(modEntry.Info.Id);
            _harmony.PatchAll();

            modEntry.OnUpdate = OnUpdate;
            modEntry.OnUnload = OnUnload;

            modEntry.Logger.Log("[DedicatedHost] Loaded.");
            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            DedicatedHostManager.Update(deltaTime);
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                _harmony?.UnpatchAll(modEntry.Info.Id);
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to unpatch Harmony: " + ex);
            }

            enabled = false;
            return true;
        }

        static void OnGUI()
        {
        }
    }
}
