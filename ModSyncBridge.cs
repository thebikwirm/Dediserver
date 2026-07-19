using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RailroaderDedicatedHost
{
    public static class ModSyncBridge
    {
        private static Type _commandsType;
        private static MethodInfo _executeMethod;
        private static string _lastResolveError;

        public static string Execute(string commandLine)
        {
            return Execute(ResolveGamePath(), commandLine);
        }

        public static string Execute(string gamePath, string commandLine)
        {
            try
            {
                if (!EnsureResolved(gamePath, out string error))
                    return "ERR MODSYNC UNAVAILABLE: " + error;

                object result = _executeMethod.Invoke(null, new object[] { gamePath, commandLine });
                string text = result as string;
                if (string.IsNullOrWhiteSpace(text))
                    text = "ModSync command completed.";

                return "OK MODSYNC\n" + text;
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.GetBaseException();
                return "ERR MODSYNC FAILED: " + (inner != null ? inner.Message : ex.Message);
            }
            catch (Exception ex)
            {
                return "ERR MODSYNC FAILED: " + ex.Message;
            }
        }

        public static string GetStatus()
        {
            if (EnsureResolved(ResolveGamePath(), out string error))
                return "ModSync: connected to ModPackDownloader.ModSyncCommands";

            return "ModSync: unavailable - " + error;
        }

        private static bool EnsureResolved(string gamePath, out string error)
        {
            error = string.Empty;

            if (_commandsType != null && _executeMethod != null)
                return true;

            _commandsType = FindCommandsTypeInLoadedAssemblies();

            if (_commandsType == null)
                _commandsType = TryLoadCommandsTypeFromDisk(gamePath, out _lastResolveError);

            if (_commandsType == null)
            {
                error = string.IsNullOrWhiteSpace(_lastResolveError)
                    ? "ModPackDownloader is not loaded and ModPackDownloader.dll was not found."
                    : _lastResolveError;
                return false;
            }

            _executeMethod = _commandsType.GetMethod(
                "Execute",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);

            if (_executeMethod == null)
            {
                error = "ModPackDownloader.ModSyncCommands.Execute(string gamePath, string commandLine) was not found.";
                return false;
            }

            return true;
        }

        private static Type FindCommandsTypeInLoadedAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type type = assembly.GetType("ModPackDownloader.ModSyncCommands", false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private static Type TryLoadCommandsTypeFromDisk(string gamePath, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(gamePath))
            {
                error = "Could not resolve Railroader game path.";
                return null;
            }

            string[] candidates =
            {
                Path.Combine(gamePath, "Mods", "ModPackDownloader", "ModPackDownloader.dll"),
                Path.Combine(gamePath, "Mods", "modpackdownloader", "ModPackDownloader.dll")
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (!File.Exists(candidate))
                        continue;

                    Assembly assembly = Assembly.LoadFrom(candidate);
                    Type type = assembly.GetType("ModPackDownloader.ModSyncCommands", false);
                    if (type != null)
                        return type;

                    error = "Loaded " + candidate + " but ModSyncCommands was not found.";
                }
                catch (Exception ex)
                {
                    error = "Failed to load " + candidate + ": " + ex.Message;
                }
            }

            if (string.IsNullOrWhiteSpace(error))
                error = "ModPackDownloader.dll was not found in Railroader/Mods/ModPackDownloader/.";

            return null;
        }

        private static string ResolveGamePath()
        {
            try
            {
                string dataPath = Application.dataPath;
                if (!string.IsNullOrWhiteSpace(dataPath))
                {
                    DirectoryInfo dataDir = new DirectoryInfo(dataPath);
                    if (dataDir.Exists && dataDir.Parent != null)
                        return dataDir.Parent.FullName;
                }
            }
            catch
            {
            }

            try
            {
                string current = Environment.CurrentDirectory;
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                    return current;
            }
            catch
            {
            }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
                    return baseDir;
            }
            catch
            {
            }

            return string.Empty;
        }
    }
}
