using BepInEx.Configuration;
using UnityEngine;

namespace RollingGiant.Settings; 

public class GeneralSettings {
    public const string Name = "1. General Settings";
    public ConfigEntry<float> ChanceForGiant;
    public ConfigEntry<float> GiantScaleMin;
    public ConfigEntry<float> GiantScaleMax;
    public ConfigEntry<bool> SpawnInAllLevels;
    public ConfigEntry<bool> SpawnInLevelsWithCoilHead;
    public ConfigEntry<bool> SpawnInside;
    public ConfigEntry<bool> SpawnDaytime;
    public ConfigEntry<bool> SpawnOutside;
    public ConfigEntry<int> Version;
    public ConfigEntry<string> GotoPreviousAiTypeKey;
    public ConfigEntry<string> GotoNextAiTypeKey;
    
    public GeneralSettings(ConfigFile configFile) {
        ChanceForGiant = configFile.Bind(Name, nameof(ChanceForGiant), 0.4f, "0.0-1.0: Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant.");
        GiantScaleMin = configFile.Bind(Name, nameof(GiantScaleMin), 1f, "The minimum scale of the Rolling Giant.");
        GiantScaleMax = configFile.Bind(Name, nameof(GiantScaleMax), 1f, "The maximum scale of the Rolling Giant.");
        SpawnInAllLevels = configFile.Bind(Name, nameof(SpawnInAllLevels), false, "If the Rolling Giant should spawn in all levels.");
        SpawnInLevelsWithCoilHead = configFile.Bind(Name, nameof(SpawnInLevelsWithCoilHead), true, "If the Rolling Giant should spawn in levels with a Coilhead.");
        SpawnInside = configFile.Bind(Name, nameof(SpawnInside), true, "If the Rolling Giant should spawn inside.");
        SpawnDaytime = configFile.Bind(Name, nameof(SpawnDaytime), false, "If the Rolling Giant should spawn during the day.");
        SpawnOutside = configFile.Bind(Name, nameof(SpawnOutside), false, "If the Rolling Giant should spawn outside.");
        Version = configFile.Bind("z_Ignore", "__version", 0, "The version of this config file. Do not change this.");
        GotoPreviousAiTypeKey = configFile.Bind("Dev", nameof(GotoPreviousAiTypeKey), "<Keyboard>/numpad7", "The key to go to the previous AI type. This uses Unity's New Input System's key-bind names.");
        GotoNextAiTypeKey = configFile.Bind("Dev", nameof(GotoNextAiTypeKey), "<Keyboard>/numpad9", "The key to go to the next AI type. This uses Unity's New Input System's key-bind names.");
    }
    
    public float GetRandomScale(System.Random rng) {
        var value = (float)rng.NextDouble();
        var min = GiantScaleMin.Value;
        var max = GiantScaleMax.Value;
        return Mathf.Lerp(min, max, value);
    }
}