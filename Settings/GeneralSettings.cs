using BepInEx.Configuration;
using UnityEngine;

namespace RollingGiant.Settings; 

public class GeneralSettings {
    public ConfigEntry<float> ChanceForGiant;
    public ConfigEntry<float> GiantScaleMin;
    public ConfigEntry<float> GiantScaleMax;
    
    public GeneralSettings(ConfigFile configFile) {
        ChanceForGiant = configFile.Bind("General", "ChanceForGiant", 0.4f, "0.0-1.0: Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant.");
        GiantScaleMin = configFile.Bind("General", "GiantScaleMin", 1f, "The minimum scale of the Rolling Giant.");
        GiantScaleMax = configFile.Bind("General", "GiantScaleMax", 1f, "The maximum scale of the Rolling Giant.");
    }
    
    public float GetRandomScale(System.Random rng) {
        var value = (float)rng.NextDouble();
        var min = GiantScaleMin.Value;
        var max = GiantScaleMax.Value;
        return Mathf.Lerp(min, max, value);
    }
}