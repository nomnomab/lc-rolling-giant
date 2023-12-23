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

[BepInPlugin("nomnomab.rollinggiant", "Rolling Giant", "1.0.0")]
public class Plugin : BaseUnityPlugin {
    public static string PluginDirectory;
    public static ConfigEntry<float> ChanceForGiant;
    
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
        ChanceForGiant = Config.Bind("General", "ChanceForGiant", 0.5f, "0.0-1.0: Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant.");
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
            WalkSound = Bundle.LoadAsset<AudioClip>("Assets/Rolling Giant Moving.wav");
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