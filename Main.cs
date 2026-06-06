using RailroaderDedicatedHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace Dediserver
{
    static class Main
    {
        public static bool enabled;
        private static DedicatedServerConfig _config;

        // Simply call. Can be compiled without dependencies on UnityModManagerNet.

        // Transfer a variable with data about the mod.
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            _config = DedicatedServerConfig.Load(modEntry);
            DedicatedHostManager.Init(modEntry, _config);

            modEntry.OnUpdate = OnUpdate;

            return true;
        }
        static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            DedicatedHostManager.Update(deltaTime);
            DedicatedBootstrap.Update(_config);
        }
        static void OnGUI()
        {
        }
    }
}
