using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Video;
using PluginInfo = LethalCompanyTemplate.PluginInfo;

namespace RollingGiant;

public enum RollingGiantAiType {
    Coilhead,
    MoveWhenLooking,
    RandomlyMoveWhileLooking,
    LookingTooLongKeepsAgro
}

[BepInPlugin("nomnomab.rollinggiant", "Rolling Giant", "1.1.0")]
public class Plugin : BaseUnityPlugin {
    public static string PluginDirectory;
    // general
    public static ConfigEntry<float> ChanceForGiant;
    public static ConfigEntry<float> GiantScale;
    public static ConfigEntry<bool> RotateToLookAtPlayer;
    public static ConfigEntry<float> DelayBeforeLookingAtPlayer;
    public static ConfigEntry<float> LookAtPlayerDuration;
    // ai
    public static ConfigEntry<RollingGiantAiType> AiType;
    public static ConfigEntry<float> AiMoveSpeed;
    public static ConfigEntry<float> AiWaitTimeMin;
    public static ConfigEntry<float> AiWaitTimeMax;
    public static ConfigEntry<float> AiRandomMoveTimeMin;
    public static ConfigEntry<float> AiRandomMoveTimeMax;
    
    public static AssetBundle Bundle;
    public static GameObject RollingGiantModel;
    public static AudioClip WalkSound;
    public static AudioClip[] QuickWalkSounds;
    public static VideoClip MovieTexture;
    
    new internal static ManualLogSource Log;

    private void Awake() {
        Log = Logger;
        PluginDirectory = Info.Location;
        LoadSettings();
        LoadAssets();
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void LoadSettings() {
        // general
        ChanceForGiant = Config.Bind("General", "ChanceForGiant", 0.4f, "0.0-1.0: Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant.");
        GiantScale = Config.Bind("General", "GiantScale", 1f, "The scale of the giant in-game. Only affects the visuals.");
        RotateToLookAtPlayer = Config.Bind("General", "RotateToLookAtPlayer", true, "If the Rolling Giant should rotate to look at the player.");
        DelayBeforeLookingAtPlayer = Config.Bind("General", "DelayBeforeLookingAtPlayer", 2f, "The delay before the Rolling Giant looks at the player.");
        LookAtPlayerDuration = Config.Bind("General", "LookAtPlayerDuration", 3f, "The duration the Rolling Giant looks at the player.");
        
        // ai
        AiType = Config.Bind("AI", "AiType", RollingGiantAiType.RandomlyMoveWhileLooking, "The AI type of the Rolling Giant.\nCoilhead = Coilhead AI\nMoveWhenLooking = Move when player is looking at it\nRandomlyMoveWhileLooking = Randomly move while the player is looking at it\nLookingTooLongKeepsAgro = If the player looks at it for too long it doesn't stop chasing");
        AiMoveSpeed = Config.Bind("AI", "AiMoveSpeed", 6f, "The speed of the Rolling Giant.");
        AiWaitTimeMin = Config.Bind("AI", "AiWaitTimeMin", 1f, "The minimum time the Rolling Giant waits before moving again.");
        AiWaitTimeMax = Config.Bind("AI", "AiWaitTimeMax", 3f, "The maximum time the Rolling Giant waits before moving again.");
        AiRandomMoveTimeMin = Config.Bind("AI", "AiRandomMoveTimeMin", 1f, "The minimum time the Rolling Giant moves toward the player.");
        AiRandomMoveTimeMax = Config.Bind("AI", "AiRandomMoveTimeMax", 3f, "The maximum time the Rolling Giant moves toward the player.");
    }

    private void LoadAssets() {
        try {
            // Bundle = AssetBundle.LoadFromFile($"{Application.dataPath}/../BepInEx/plugins/rollinggiant");
            var dirName = Path.GetDirectoryName(PluginDirectory);
            Bundle = AssetBundle.LoadFromFile(Path.Combine(dirName, "rollinggiant"));
        } catch (System.Exception e) {
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
        } catch (System.Exception e) {
            Log.LogError($"Failed to load assets! {e}");
        }
    }
}