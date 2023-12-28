using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RollingGiant.Settings;
using UnityEngine;
using UnityEngine.Video;

namespace RollingGiant;

public enum RollingGiantAiType {
    Coilhead,
    MoveWhenLooking,
    RandomlyMoveWhileLooking,
    LookingTooLongKeepsAgro,
    FollowOnceAgro,
    OnceSeenAgroAfterTimer
}

[BepInPlugin("nomnomab.rollinggiant", "Rolling Giant", "1.2.0")]
public class Plugin : BaseUnityPlugin {
    public static BaseAiTypeSettings CurrentAiTypeSettings { get; private set; }
    public static GeneralSettings GeneralSettings { get; private set; }
    public static AiSettings AiSettings { get; private set; }
    public static CoilheadAiTypeSettings CoilheadAiSettings { get; private set; }
    public static InverseCoilheadAiTypeSettings InverseCoilheadAiSettings { get; private set; }
    public static RandomlyMoveWhileLookingAiTypeSettings RandomlyMoveWhileLookingAiSettings { get; private set; }
    public static LookingTooLongKeepsAgroAiTypeSettings LookingTooLongKeepsAgroAiSettings { get; private set; }
    public static FollowOnceAgroAiTypeSettings FollowOnceAgroAiSettings { get; private set; }
    public static OnceSeenAgroAfterTimerAiTypeSettings OnceSeenAgroAfterTimerAiSettings { get; private set; }

    public static string PluginDirectory;

    public static AssetBundle Bundle;
    public static GameObject RollingGiantModel;
    public static AudioClip WalkSound;
    public static AudioClip[] QuickWalkSounds;
    public static VideoClip MovieTexture;

    internal static ManualLogSource Log;

    private void Awake() {
        Log = Logger;
        PluginDirectory = Info.Location;
        LoadSettings();
        LoadAssets();
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void LoadSettings() {
        Log.LogMessage(string.Join("\n", Config.Keys));
        
        GeneralSettings = new GeneralSettings(Config);
        AiSettings = new AiSettings(Config);
        CoilheadAiSettings = new CoilheadAiTypeSettings(Config, "AI.Coilhead");
        InverseCoilheadAiSettings = new InverseCoilheadAiTypeSettings(Config, "AI.InverseCoilhead");
        RandomlyMoveWhileLookingAiSettings = new RandomlyMoveWhileLookingAiTypeSettings(Config, "AI.RandomlyMoveWhileLooking");
        LookingTooLongKeepsAgroAiSettings = new LookingTooLongKeepsAgroAiTypeSettings(Config, "AI.LookingTooLongKeepsAgro");
        FollowOnceAgroAiSettings = new FollowOnceAgroAiTypeSettings(Config, "AI.FollowOnceAgro");
        OnceSeenAgroAfterTimerAiSettings = new OnceSeenAgroAfterTimerAiTypeSettings(Config, "AI.OnceSeenAgroAfterTimer");
        
        RemoveOldSettings();

        CurrentAiTypeSettings = AiSettings.AiType.Value switch {
            RollingGiantAiType.Coilhead                 => CoilheadAiSettings,
            RollingGiantAiType.MoveWhenLooking          => InverseCoilheadAiSettings,
            RollingGiantAiType.RandomlyMoveWhileLooking => RandomlyMoveWhileLookingAiSettings,
            RollingGiantAiType.LookingTooLongKeepsAgro  => LookingTooLongKeepsAgroAiSettings,
            RollingGiantAiType.FollowOnceAgro           => FollowOnceAgroAiSettings,
            RollingGiantAiType.OnceSeenAgroAfterTimer   => OnceSeenAgroAfterTimerAiSettings,
            _                                           => throw new System.NotImplementedException("Unknown AI type")
        };
    }

    private void RemoveOldSettings() {
        var previousSetting = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;

        var save = false;
        if (!Config.ContainsKey(new ConfigDefinition("General", "RotateToLookAtPlayer"))) {
            Config.Bind("General", "RotateToLookAtPlayer", true);   
            save |= Config.Remove(new ConfigDefinition("General", "GiantScale"));
        }
        
        Config.Bind("General", "DelayBeforeLookingAtPlayer", 1f);
        Config.Bind("General", "LookAtPlayerDuration", 1f);
        Config.Bind("AI", "AiMoveSpeed", 1f);
        Config.Bind("AI", "AiWaitTimeMin", 1f);
        Config.Bind("AI", "AiWaitTimeMax", 1f);
        Config.Bind("AI", "AiRandomMoveTimeMin", 1f);
        Config.Bind("AI", "AiRandomMoveTimeMax", 1f);
        
        save |= Config.Remove(new ConfigDefinition("General", "RotateToLookAtPlayer"));
        save |= Config.Remove(new ConfigDefinition("General", "DelayBeforeLookingAtPlayer"));
        save |= Config.Remove(new ConfigDefinition("General", "LookAtPlayerDuration"));
        
        save |= Config.Remove(new ConfigDefinition("AI", "AiMoveSpeed"));
        save |= Config.Remove(new ConfigDefinition("AI", "AiWaitTimeMin"));
        save |= Config.Remove(new ConfigDefinition("AI", "AiWaitTimeMax"));
        save |= Config.Remove(new ConfigDefinition("AI", "AiRandomMoveTimeMin"));
        save |= Config.Remove(new ConfigDefinition("AI", "AiRandomMoveTimeMax"));
        
        if (save) {
            Log.LogWarning("Removed old settings");
            Config.Save();
        }
        
        Config.SaveOnConfigSet = previousSetting;
    }

    private void LoadAssets() {
        try {
            var dirName = Path.GetDirectoryName(PluginDirectory);
            Bundle = AssetBundle.LoadFromFile(Path.Combine(dirName, "rollinggiant"));
        }
        catch (System.Exception e) {
            Log.LogError($"Failed to load asset bundle! {e}");
        }

        try {
            RollingGiantModel = Bundle.LoadAsset<GameObject>("Assets/Rolling Giant.prefab");
            WalkSound = Bundle.LoadAsset<AudioClip>("Assets/mrtheoldestview.wav");
            QuickWalkSounds = new AudioClip[5];
            for (int i = 1; i < 5; i++) {
                QuickWalkSounds[i] = Bundle.LoadAsset<AudioClip>($"Assets/Rolling Giant Moving-{i + 1}.wav");
            }
            MovieTexture = Bundle.LoadAsset<VideoClip>("Assets/rolling_giant.mp4");
        }
        catch (System.Exception e) {
            Log.LogError($"Failed to load assets! {e}");
        }
    }
}

#if DEBUG
[HarmonyPatch]
public class DebugEditorPatch {
    [HarmonyPatch(typeof(Application), "isEditor", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ForceIsEditor(ref bool __result) {
        __result = true;
    }
}
#endif