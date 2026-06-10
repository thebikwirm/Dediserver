using System;
using UnityEngine;
using UnityModManagerNet;

namespace RailroaderDedicatedHost
{
    public static class GraphicsSuppressor
    {
        private static bool _applied;
        private static float _scanTimer;
        private static DedicatedServerConfig _config;

        public static void Apply(DedicatedServerConfig config, UnityModManager.ModEntry modEntry)
        {
            _config = config;

            if (_applied)
                return;

            _applied = true;

            try
            {
                modEntry.Logger.Log("[DedicatedHost] Applying graphics suppression.");

                Screen.SetResolution(320, 200, false);

                Application.runInBackground = true;
                Application.targetFrameRate = 15;

                QualitySettings.SetQualityLevel(0, true);
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
                QualitySettings.masterTextureLimit = 3;

                MuteAudioSources();

                if (config.AggressiveGraphicsDisable)
                    DisableAggressiveObjects();

                modEntry.Logger.Log("[DedicatedHost] Graphics suppression applied.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error("[DedicatedHost] Graphics suppression failed: " + ex);
            }
        }

        public static void Tick()
        {
            _scanTimer += Time.unscaledDeltaTime;

            if (_scanTimer < 5f)
                return;

            _scanTimer = 0f;

            MuteAudioSources();
        }

        private static void MuteAudioSources()
        {
            if (_config == null || !_config.MuteAudio)
                return;

            AudioListener.volume = Mathf.Clamp01(_config.AudioVolume);

            foreach (AudioSource audio in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (audio == null)
                    continue;

                audio.mute = true;
                audio.volume = 0f;
            }
        }

        private static void DisableAggressiveObjects()
        {
            foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                light.enabled = false;
            }

            foreach (ParticleSystem ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }

            foreach (ReflectionProbe probe in UnityEngine.Object.FindObjectsOfType<ReflectionProbe>())
            {
                probe.enabled = false;
            }
        }
    }
}
