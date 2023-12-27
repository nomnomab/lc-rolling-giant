using BepInEx.Configuration;
using UnityEngine;

namespace RollingGiant.Settings;

public abstract class BaseAiTypeSettings {
    public const string MoveSpeedDescription = "The speed of the Rolling Giant.";
    public const string MoveBuildUpDurationDescription = "How long it takes the Rolling Giant to get to its movement speed.";
    public const string RotateToLookAtPlayerDescription = "If the Rolling Giant should rotate to look at the player.";
    public const string DelayBeforeLookingAtPlayerDescription = "The delay before the Rolling Giant looks at the player.";
    public const string LookAtPlayerDurationDescription = "The duration the Rolling Giant looks at the player.";
    
    public ConfigEntry<float> MoveSpeed;
    public ConfigEntry<float> MoveBuildUpDuration;
    public ConfigEntry<bool> RotateToLookAtPlayer;
    public ConfigEntry<float> DelayBeforeLookingAtPlayer;
    public ConfigEntry<float> LookAtPlayerDuration;
    

    public BaseAiTypeSettings(ConfigFile configFile, string group) {
        MoveSpeed = configFile.Bind(group, "MoveSpeed", 6f, MoveSpeedDescription);
        MoveBuildUpDuration = configFile.Bind(group, "MoveBuildUpDuration", 1f, MoveBuildUpDurationDescription);
        RotateToLookAtPlayer = configFile.Bind(group, "RotateToLookAtPlayer", true, RotateToLookAtPlayerDescription);
        DelayBeforeLookingAtPlayer = configFile.Bind(group, "DelayBeforeLookingAtPlayer", 2f, DelayBeforeLookingAtPlayerDescription);
        LookAtPlayerDuration = configFile.Bind(group, "LookAtPlayerDuration", 3f, LookAtPlayerDurationDescription);
    }
}

public class WanderAi: BaseAiTypeSettings {
    public ConfigEntry<bool> CanWander {get; set;}
    public ConfigEntry<float> ChaseMaxDistance {get; set;}

    public WanderAi(ConfigFile configFile, string group) : base(configFile, group) {
        CanWander = configFile.Bind(group, "CanWander", true, "If the Rolling Giant can go back to wandering after the player gets far enough away from it.");
        ChaseMaxDistance = configFile.Bind(group, "ChaseMaxDistance", 50f, "The distance between the Rolling Giant and the player before it stops chasing and goes back to wandering.");
    }
    
    public (bool, float) GetWanderSettings() {
        return (CanWander.Value, ChaseMaxDistance.Value);
    }
}

public class CoilheadAiTypeSettings : WanderAi {
    public CoilheadAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
    }
}

public class InverseCoilheadAiTypeSettings : WanderAi {
    public InverseCoilheadAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
    }
}

public class RandomlyMoveWhileLookingAiTypeSettings : WanderAi {
    public ConfigEntry<float> WaitTimeMin;
    public ConfigEntry<float> WaitTimeMax;
    public ConfigEntry<float> RandomMoveTimeMin;
    public ConfigEntry<float> RandomMoveTimeMax;
    
    public RandomlyMoveWhileLookingAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
        WaitTimeMin = configFile.Bind("AI.RandomlyMoveWhileLooking", "WaitTimeMin", 1f, "The minimum duration the Rolling Giant waits before moving again.");
        WaitTimeMax = configFile.Bind("AI.RandomlyMoveWhileLooking", "WaitTimeMax", 3f, "The maximum duration the Rolling Giant waits before moving again.");
        RandomMoveTimeMin = configFile.Bind("AI.RandomlyMoveWhileLooking", "RandomMoveTimeMin", 1f, "The minimum duration the Rolling Giant moves toward the player.");
        RandomMoveTimeMax = configFile.Bind("AI.RandomlyMoveWhileLooking", "RandomMoveTimeMax", 3f, "The maximum duration the Rolling Giant moves toward the player.");
    }
    
    public float GetRandomWaitTime(System.Random rng) {
        var value = rng.Next();
        var min = WaitTimeMin.Value;
        var max = WaitTimeMax.Value;
        return Mathf.Lerp(min, max, value);
    }
    
    public float GetRandomMoveTime(System.Random rng) {
        var value = rng.Next();
        var min = RandomMoveTimeMin.Value;
        var max = RandomMoveTimeMax.Value;
        return Mathf.Lerp(min, max, value);
    }
}

public class LookingTooLongKeepsAgroAiTypeSettings : BaseAiTypeSettings {
    public ConfigEntry<float> LookTimeBeforeAgro;
    
    public LookingTooLongKeepsAgroAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
        LookTimeBeforeAgro = configFile.Bind("AI.LookingTooLongKeepsAgro", "LookTimeBeforeAgro", 10f, "How long the player can look at the Rolling Giant before it starts chasing.");
    }
}

public class FollowOnceAgroAiTypeSettings : WanderAi {
    public FollowOnceAgroAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
    }
}

public class OnceSeenAgroAfterTimerAiTypeSettings : BaseAiTypeSettings {
    public ConfigEntry<float> WaitTimeMin;
    public ConfigEntry<float> WaitTimeMax;
    
    public OnceSeenAgroAfterTimerAiTypeSettings(ConfigFile configFile, string group) : base(configFile, group) {
        WaitTimeMin = configFile.Bind("AI.OnceSeenAgroAfterTimer", "WaitTimeMin", 30f, "The minimum duration the Rolling Giant waits before chasing the player.");
        WaitTimeMax = configFile.Bind("AI.OnceSeenAgroAfterTimer", "WaitTimeMax", 60f, "The minimum duration the Rolling Giant waits before chasing the player.");
    }
    
    public float GetRandomWaitTime(System.Random rng) {
        var value = rng.Next();
        var min = WaitTimeMin.Value;
        var max = WaitTimeMax.Value;
        return Mathf.Lerp(min, max, value);
    }
}