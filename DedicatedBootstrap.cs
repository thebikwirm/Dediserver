using System;
using Game.Persistence;
using Network;
using Network.Steam;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class DedicatedBootstrap
    {
        private static bool _started;
        private static float _timer;

        public static void Update(DedicatedServerConfig config)
        {
            if (_started)
                return;

            _timer += Time.deltaTime;

            // wait for game startup fix
            if (_timer < 10f)
                return;

            try
            {
                Debug.Log("[DedicatedHost] Loading save: " + config.SaveName);

                WorldStore.Load(config.SaveName);

                Debug.Log("[DedicatedHost] Starting multiplayer server");

                Multiplayer.StartMultiplayerServer(
                    config.ServerName,
                    LobbyType.Public
                );

                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
}