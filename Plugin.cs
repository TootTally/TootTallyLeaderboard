﻿using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyModules;
using TootTallyLeaderboard.Replays;
using TootTallySettings;

namespace TootTallyLeaderboard
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyAccounts", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyGameModifiers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyThemes", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "TootTally.cfg";
        public Options option;
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => "TootTally Leaderboard"; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);
        public static void LogWarning(string msg) => Instance.Logger.LogWarning(msg);
        public static void LogDebug(string msg)
        {
            if (TootTallyCore.Plugin.Instance.DebugMode.Value)
                Instance.Logger.LogDebug(msg);
        }

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "TootTally Leaderboard", true, "Leaderboard and Replay features for TootTally");
            TootTallyModuleManager.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                ShowLeaderboard = Config.Bind("General", "Show Leaderboard", true, "Show TootTally Leaderboard on Song Select."),
                ShowCoolS = Config.Bind("General", "Show Cool S", false, "Show special graphic when getting SS and SSS on a song."),
                ChangePitchSpeed = Config.Bind("General", "Change Pitch Speed", false, "Change the pitch on speed changes"),
            };

            TootTallySettings.Plugin.MainTootTallySettingPage.AddToggle("Show Leaderboard", option.ShowLeaderboard);
            TootTallySettings.Plugin.MainTootTallySettingPage.AddToggle("Show Cool S", option.ShowCoolS);
            TootTallySettings.Plugin.MainTootTallySettingPage.AddToggle("Change Pitch Speed", option.ChangePitchSpeed);

            AssetManager.LoadAssets(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "Assets"));

            _harmony.PatchAll(typeof(LeaderboardFactory));
            _harmony.PatchAll(typeof(ReplaySystemManager));
            _harmony.PatchAll(typeof(GlobalLeaderboardManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public class Options
        {
            public ConfigEntry<bool> ShowLeaderboard { get; set; }
            public ConfigEntry<bool> ShowCoolS { get; set; }
            public ConfigEntry<bool> ChangePitchSpeed { get; set; }
        }
    }
}