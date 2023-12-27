using System.IO;
using System.Reflection;
using BepInEx;
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

[BepInPlugin("nomnomab.rollinggiant", "Rolling Giant", "1.1.0")]
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
        GeneralSettings = new GeneralSettings(Config);
        AiSettings = new AiSettings(Config);
        CoilheadAiSettings = new CoilheadAiTypeSettings(Config, "AI.Coilhead");
        InverseCoilheadAiSettings = new InverseCoilheadAiTypeSettings(Config, "AI.InverseCoilhead");
        RandomlyMoveWhileLookingAiSettings = new RandomlyMoveWhileLookingAiTypeSettings(Config, "AI.RandomlyMoveWhileLooking");
        LookingTooLongKeepsAgroAiSettings = new LookingTooLongKeepsAgroAiTypeSettings(Config, "AI.LookingTooLongKeepsAgro");
        FollowOnceAgroAiSettings = new FollowOnceAgroAiTypeSettings(Config, "AI.FollowOnceAgro");
        OnceSeenAgroAfterTimerAiSettings = new OnceSeenAgroAfterTimerAiTypeSettings(Config, "AI.OnceSeenAgroAfterTimer");

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