using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public class DedicatedServerConfig
    {
        public bool Enabled = false;

        public string SaveName = "DedicatedServerSave";
        public string ServerName = "Railroader Dedicated Host";
        public string Password = "";
        public int Port = 7777;

        public bool HideGraphics = true;
        public bool AggressiveGraphicsDisable = false;

        public int TargetServerFps = 20;
        public int AutosaveSeconds = 300;

        public static DedicatedServerConfig Load(UnityModManager.ModEntry modEntry)
        {
            string path = Path.Combine(modEntry.Path, "dedicated_host.json");

            if (!File.Exists(path))
            {
                var cfg = new DedicatedServerConfig();
                File.WriteAllText(path, JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented));
                modEntry.Logger.Log("[DedicatedHost] Created default dedicated_host.json");
                return cfg;
            }

            try
            {
                return JsonConvert.DeserializeObject<DedicatedServerConfig>(
                    File.ReadAllText(path)
                ) ?? new DedicatedServerConfig();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Failed to load config: " + ex);
                return new DedicatedServerConfig();
            }
        }
    }
}