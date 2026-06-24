using System;
using System.IO;

namespace RailroaderDedicatedHost
{
    public static class DedicatedPaths
    {
        private static DedicatedServerConfig _config;
        private static string _modPath;
        private static string _gamePath;

        public static string GamePath
        {
            get
            {
                if (string.IsNullOrEmpty(_gamePath))
                    return Directory.GetCurrentDirectory();

                return _gamePath;
            }
        }

        public static string ModPath
        {
            get
            {
                return _modPath;
            }
        }

        public static void Init(DedicatedServerConfig config, string modPath)
        {
            _config = config;
            _modPath = modPath;
            _gamePath = ResolveGamePath(config, modPath);
        }

        public static string ResolveGamePath(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
                return GamePath;

            if (Path.IsPathRooted(relativeOrAbsolutePath))
                return relativeOrAbsolutePath;

            return Path.Combine(GamePath, relativeOrAbsolutePath);
        }

        private static string ResolveGamePath(DedicatedServerConfig config, string modPath)
        {
            string configuredPath = config != null ? config.RailroaderInstallPath : null;

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                string fullConfiguredPath = Path.GetFullPath(configuredPath.Trim());

                if (LooksLikeRailroaderInstall(fullConfiguredPath))
                    return fullConfiguredPath;

                DedicatedHostManager.LogError("Configured RailroaderInstallPath does not contain Railroader.exe: " + fullConfiguredPath);
            }

            string currentDirectory = Directory.GetCurrentDirectory();
            if (LooksLikeRailroaderInstall(currentDirectory))
                return Path.GetFullPath(currentDirectory);

            string fromModPath = TryFindFromModPath(modPath);
            if (!string.IsNullOrEmpty(fromModPath))
                return fromModPath;

            return Path.GetFullPath(currentDirectory);
        }

        private static string TryFindFromModPath(string modPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modPath))
                    return null;

                DirectoryInfo directory = new DirectoryInfo(modPath);

                for (int i = 0; i < 6 && directory != null; i++)
                {
                    if (LooksLikeRailroaderInstall(directory.FullName))
                        return directory.FullName;

                    directory = directory.Parent;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool LooksLikeRailroaderInstall(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "Railroader.exe"));
            }
            catch
            {
                return false;
            }
        }
    }
}
