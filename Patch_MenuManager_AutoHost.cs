using HarmonyLib;
using Network;
using Network.Steam;
using System;
using System.Collections.Generic;
using UI.Menu;
using Game.State;

namespace RailroaderDedicatedHost
{
    [HarmonyPatch(typeof(MenuManager), "Start")]
    public static class Patch_MenuManager_AutoHost
    {
        private static bool _started;

        private static void Postfix(MenuManager __instance)
        {
            if (_started)
                return;

            if (!DedicatedHostManager.IsDedicated)
                return;

            _started = true;

            try
            {
                DedicatedServerConfig config = DedicatedHostManager.Config;

                if (config == null)
                {
                    DedicatedHostManager.LogError("Config was null; cannot auto-host.");
                    return;
                }

                DedicatedHostManager.Log("Auto-host starting from MenuManager.Start.");

                GlobalGameManager gameManager = AccessTools.Field(typeof(MenuManager), "gameManager")
                    .GetValue(__instance) as GlobalGameManager;

                if (gameManager == null)
                {
                    DedicatedHostManager.LogError("Could not find GlobalGameManager on MenuManager.");
                    return;
                }

                List<SceneDescriptor> scenes = new List<SceneDescriptor>
                {
                    SceneDescriptor.BushnellWhittier,
                    SceneDescriptor.EnvironmentEnviro
                };

                GlobalGameManager.SceneLoadSetup sceneSetup = new GlobalGameManager.SceneLoadSetup(
                    scenes,
                    SceneDescriptor.BushnellWhittier
                );

                GameSetup gameSetup = new GameSetup(config.SaveName);
                StartMultiplayerHostSetup networkSetup = new StartMultiplayerHostSetup(
                    config.ServerName,
                    LobbyType.Public
                );

                gameManager.Launch(sceneSetup, gameSetup, networkSetup);

                DedicatedHostManager.Log(
                    "Launch called. Save=" + config.SaveName +
                    ", ServerName=" + config.ServerName
                );
            }
            catch (Exception ex)
            {
                DedicatedHostManager.LogError("Auto-host failed: " + ex);
            }
        }
    }
}
