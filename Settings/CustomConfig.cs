using System;
using System.ComponentModel;
using BepInEx.Configuration;
using Unity.Collections;
using Unity.Netcode;

namespace RollingGiant.Settings;

[Serializable]
public class CustomConfig : SyncedInstance<CustomConfig> {
    public const string ROLLINGGIANT_ONREQUESTCONFIGSYNC = "RollingGiant_OnRequestConfigSync";
    public const string ROLLINGGIANT_ONRECEIVECONFIGSYNC = "RollingGiant_OnReceiveConfigSync";

    private static ConfigFile _config;

    public static SharedAiSettings SharedAiSettings { get; private set; }
    public static ConfigEntry<string> GotoPreviousAiTypeKey { get; private set; }
    public static ConfigEntry<string> GotoNextAiTypeKey { get; private set; }
    public static ConfigEntry<string> ReloadConfigKey { get; private set; }
    public static ConfigEntry<string> SpawnInEntry { get; private set; }
    public static ConfigEntry<int> SpawnInOutsideChanceEntry { get; private set; }
    public static ConfigEntry<bool> SpawnInAnyEntry { get; private set; }
    public static ConfigEntry<int> SpawnInAnyChanceEntry { get; private set; }
    public static ConfigEntry<int> SpawnInAnyOutsideChanceEntry { get; private set; }
    public static ConfigEntry<bool> CanSpawnInsideEntry { get; private set; }
    public static ConfigEntry<bool> CanSpawnOutsideEntry { get; private set; }
    public static ConfigEntry<bool> DisableOutsideAtNightEntry { get; private set; }
    public static ConfigEntry<int> MaxPerLevelEntry { get; private set; }
    public static ConfigEntry<string> SpawnPosterInEntry { get; private set; }

    // general settings
    public const string Name1 = "1. General Settings";
    public float GiantScaleInsideMin { get; private set; }
    public float GiantScaleInsideMax { get; private set; }
    public float GiantScaleOutsideMin { get; private set; }
    public float GiantScaleOutsideMax { get; private set; }
    public static string SpawnIn { get; private set; }
    public static int SpawnInOutsideChance { get; private set; }
    public static bool SpawnInAny { get; private set; }
    public static int SpawnInAnyChance { get; private set; }
    public static int SpawnInAnyOutsideChance { get; private set; }
    public static bool CanSpawnInside { get; private set; }
    public static bool CanSpawnOutside { get; private set; }
    public static bool DisableOutsideAtNight { get; private set; }
    public static int MaxPerLevel { get; private set; }
    public static string SpawnPosterIn { get; private set; }

    // ai settings
    public const string Name2 = "2. AI Settings";

    public const string AiTypeDescription =
        "The AI type of the Rolling Giant.\n(Putting multiple will randomly choose between them each time you land on a moon)";
    public const string AiTypeChangeOnHourIntervalDescription =
        "If the AI type should change every X hours. This will affect already spawned Rolling Giants!\nIf set to 0 it will not change.\nWill pick from the values set in AiType.";

    public static RollingGiantAiType AiType { get; internal set; }
    public static int AiTypeChangeOnHourInterval { get; private set; }

    public const string MoveSpeedDescription = "The speed of the Rolling Giant in m/s\u00b2.";
    public const string MoveAccelerationDescription = "How long it takes the Rolling Giant to get to its movement speed. in seconds";
    public const string MoveDecelerationDescription = "How long it takes the Rolling Giant to stop moving in seconds.";
    public const string RotateToLookAtPlayerDescription = "If the Rolling Giant should rotate to look at the player.";
    public const string DelayBeforeLookingAtPlayerDescription = "The delay before the Rolling Giant looks at the player in seconds.";
    public const string LookAtPlayerDurationDescription = "The duration the Rolling Giant looks at the player in seconds.";

    public float MoveSpeed { get; private set; }
    public float MoveAcceleration { get; private set; }
    public float MoveDeceleration { get; private set; }
    public bool RotateToLookAtPlayer { get; private set; }
    public float DelayBeforeLookingAtPlayer { get; private set; }
    public float LookAtPlayerDuration { get; private set; }
    
    // random move when looking ai settings
    public float RandomlyMoveWhenLooking_WaitTimeMin { get; private set; }
    public float RandomlyMoveWhenLooking_WaitTimeMax { get; private set; }
    public float RandomlyMoveWhenLooking_RandomMoveTimeMin { get; private set; }
    public float RandomlyMoveWhenLooking_RandomMoveTimeMax { get; private set; }

    // looking too long keeps agro ai settings
    public float LookingTooLongKeepsAgro_LookTimeBeforeAgro { get; private set; }

    // once seen agro after timer ai settings
    public float OnceSeenAgroAfterTimer_WaitTimeMin { get; private set; }
    public float OnceSeenAgroAfterTimer_WaitTimeMax { get; private set; }

    // binds
    public static ConfigEntry<float> GiantScaleInsideMinEntry { get; private set; }
    public static ConfigEntry<float> GiantScaleInsideMaxEntry { get; private set; }
    public static ConfigEntry<float> GiantScaleOutsideMinEntry { get; private set; }
    public static ConfigEntry<float> GiantScaleOutsideMaxEntry { get; private set; }

    public static ConfigEntry<RollingGiantAiType> AiTypeEntry { get; private set; }
    public static ConfigEntry<float> MoveSpeedEntry { get; private set; }
    public static ConfigEntry<float> MoveAccelerationEntry { get; private set; }
    public static ConfigEntry<float> MoveDecelerationEntry { get; private set; }
    public static ConfigEntry<bool> RotateToLookAtPlayerEntry { get; private set; }
    public static ConfigEntry<float> DelayBeforeLookingAtPlayerEntry { get; private set; }
    public static ConfigEntry<float> LookAtPlayerDurationEntry { get; private set; }
    
    public static ConfigEntry<float> RandomlyMoveWhenLooking_WaitTimeMinEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_WaitTimeMaxEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_RandomMoveTimeMinEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry { get; private set; }

    public static ConfigEntry<float> LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry { get; private set; }

    public static ConfigEntry<float> OnceSeenAgroAfterTimer_WaitTimeMinEntry { get; private set; }
    public static ConfigEntry<float> OnceSeenAgroAfterTimer_WaitTimeMaxEntry { get; private set; }

    public CustomConfig(ConfigFile config) {
        _config = config;
        InitInstance(this);
        Reload();
    }

    public void Save() {
        if (_config == null) {
            throw new NullReferenceException("Config is null.");
        }
        _config.Save();
    }

    public void AssignFromSaved() {
        GiantScaleInsideMinEntry.Value = GiantScaleInsideMin;
        GiantScaleInsideMaxEntry.Value = GiantScaleInsideMax;
        GiantScaleOutsideMinEntry.Value = GiantScaleOutsideMin;
        GiantScaleOutsideMaxEntry.Value = GiantScaleOutsideMax;
        SpawnInEntry.Value = SpawnIn;
        SpawnInOutsideChanceEntry.Value = SpawnInAnyOutsideChance;
        SpawnInAnyEntry.Value = SpawnInAny;
        SpawnInAnyChanceEntry.Value = SpawnInAnyChance;
        SpawnInAnyOutsideChanceEntry.Value = SpawnInAnyOutsideChance;
        CanSpawnInsideEntry.Value = CanSpawnInside;
        CanSpawnOutsideEntry.Value = CanSpawnOutside;
        DisableOutsideAtNightEntry.Value = DisableOutsideAtNight;
        MaxPerLevelEntry.Value = MaxPerLevel;
        SpawnPosterInEntry.Value = SpawnPosterIn;

        AiTypeEntry.Value = AiType;
        AiTypeChangeOnHourInterval = AiTypeChangeOnHourInterval;
        MoveSpeedEntry.Value = MoveSpeed;
        MoveAccelerationEntry.Value = MoveAcceleration;
        MoveDecelerationEntry.Value = MoveDeceleration;
        RotateToLookAtPlayerEntry.Value = RotateToLookAtPlayer;
        DelayBeforeLookingAtPlayerEntry.Value = DelayBeforeLookingAtPlayer;
        LookAtPlayerDurationEntry.Value = LookAtPlayerDuration;
        
        RandomlyMoveWhenLooking_WaitTimeMinEntry.Value = RandomlyMoveWhenLooking_WaitTimeMin;
        RandomlyMoveWhenLooking_WaitTimeMaxEntry.Value = RandomlyMoveWhenLooking_WaitTimeMax;
        RandomlyMoveWhenLooking_RandomMoveTimeMinEntry.Value = RandomlyMoveWhenLooking_RandomMoveTimeMin;
        RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry.Value = RandomlyMoveWhenLooking_RandomMoveTimeMax;

        LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry.Value = LookingTooLongKeepsAgro_LookTimeBeforeAgro;

        OnceSeenAgroAfterTimer_WaitTimeMinEntry.Value = OnceSeenAgroAfterTimer_WaitTimeMin;
        OnceSeenAgroAfterTimer_WaitTimeMaxEntry.Value = OnceSeenAgroAfterTimer_WaitTimeMax;
    }

    public void Reload(bool setValues = true) {
        // general settings
        // _config.Reload();
        GiantScaleInsideMinEntry = _config.Bind(Name1, nameof(GiantScaleInsideMin), 0.9f, "The min scale of the Rolling Giant inside.\nThis is a multiplier, so 0.5 is half as large.");
        GiantScaleInsideMaxEntry = _config.Bind(Name1, nameof(GiantScaleInsideMax), 1.1f, "The max scale of the Rolling Giant inside.\nThis is a multiplier, so 2 is twice as large.");
        GiantScaleOutsideMinEntry = _config.Bind(Name1, nameof(GiantScaleOutsideMin), 0.9f, "The min scale of the Rolling Giant outside.\nThis is a multiplier, so 0.5 is half as large.");
        GiantScaleOutsideMaxEntry = _config.Bind(Name1, nameof(GiantScaleOutsideMax), 1.1f, "The max scale of the Rolling Giant outside.\nThis is a multiplier, so 2 is twice as large.");

        SpawnInEntry = _config.Bind(Name1,
            nameof(SpawnIn),
            "Vow:45,March:45,Rend:54,Dine:65,Offense:45,Titan:65",
            "Where the Rolling Giant can spawn.\nSeparate each level with a comma, and put a chance (no decimals) separated by a colon.\nVanilla caps at 100, but you can go farther.\nThis chance is also a weight, not a percentage.\nHigher chance = higher chance to get picked\nThe names are what you see in the terminal\nExample: Vow:6,March:10");
        SpawnInOutsideChanceEntry = _config.Bind(Name1,
            nameof(SpawnInOutsideChance),
            45,
            "The chance for the Rolling Giant to spawn outside.\nIs used alongside SpawnIn.\nThis is a weight, not a percentage.\nHigher chance = higher chance to get picked");
        SpawnInAnyEntry = _config.Bind(Name1,
            nameof(SpawnInAny),
            false,
            "If the Rolling Giant can spawn in any level.");
        SpawnInAnyChanceEntry = _config.Bind(Name1,
            nameof(SpawnInAnyChance),
            45,
            "The chance for the Rolling Giant to spawn in any level.\nRequires SpawnInAny to be enabled!\nThis is a weight, not a percentage.\nHigher chance = higher chance to get picked");
        SpawnInAnyOutsideChanceEntry = _config.Bind(Name1,
            nameof(SpawnInAnyOutsideChance),
            45,
            "The chance for the Rolling Giant to spawn outside when spawning in any level.\nRequires SpawnInAny to be enabled!\nThis is a weight, not a percentage.\nHigher chance = higher chance to get picked");
        CanSpawnInsideEntry = _config.Bind(Name1,
            nameof(CanSpawnInside),
            true,
            "If the Rolling Giant can spawn inside.");
        CanSpawnOutsideEntry = _config.Bind(Name1,
            nameof(CanSpawnOutside),
            false,
            "If the Rolling Giant can spawn outside.");
        DisableOutsideAtNightEntry = _config.Bind(Name1,
            nameof(DisableOutsideAtNight),
            false,
            "If the Rolling Giant will turn off if it is outside at night.");
        MaxPerLevelEntry = _config.Bind(Name1,
            nameof(MaxPerLevel),
            3,
            "The maximum amount of Rolling Giants that can spawn in a level.");
        SpawnPosterInEntry = _config.Bind(Name1,
            nameof(SpawnPosterIn),
            "Vow:12,March:12,Rend:12,Dine:12,Offense:12,Titan:12",
            "Where the Rolling Giant poster scrap can spawn.\nSeparate each level with a comma, and put a chance separated by a colon.\nVanilla caps at 100, but you can go farther.\nThis chance is also a weight, not a percentage.\nHigher chance = higher chance to get picked\nThe names are what you see in the terminal\nExample: Vow:12,March:12,Rend:12,Dine:12,Offense:12,Titan:12");

        GotoPreviousAiTypeKey ??= _config.Bind("Host",
            nameof(GotoPreviousAiTypeKey),
            "<Keyboard>/numpad7",
            "The key to go to the previous AI type. This uses Unity's New Input System's key-bind names.\nhttps://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Controls.html#control-paths");
        GotoNextAiTypeKey ??= _config.Bind("Host",
            nameof(GotoNextAiTypeKey),
            "<Keyboard>/numpad8",
            "The key to go to the next AI type. This uses Unity's New Input System's key-bind names.\nhttps://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Controls.html#control-paths");
        ReloadConfigKey ??= _config.Bind("Host",
            nameof(ReloadConfigKey),
            "<Keyboard>/numpad9",
            "The key to reload the config. Does not update spawn conditions. This uses Unity's New Input System's key-bind names.\nhttps://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Controls.html#control-paths");

        // ai settings
        var aiDescription = AiTypeDescription;
        foreach (var value in Enum.GetValues(typeof(RollingGiantAiType))) {
            var attribute = GetAttributeOfType<DescriptionAttribute>((Enum)value);
            var description = attribute?.Description ?? "No description found.";
            aiDescription += $"\n{value}: {description}";
        }
        AiTypeEntry = _config.Bind(Name2, nameof(AiType), RollingGiantAiType.RandomlyMoveWhileLooking, aiDescription);
        AiTypeChangeOnHourInterval = _config.Bind(Name2, nameof(AiTypeChangeOnHourInterval), 0, AiTypeChangeOnHourIntervalDescription).Value;
        MoveSpeedEntry = _config.Bind(Name2, nameof(MoveSpeed), 6f, MoveSpeedDescription);
        MoveAccelerationEntry = _config.Bind(Name2, nameof(MoveAcceleration), 2f, MoveAccelerationDescription);
        MoveDecelerationEntry = _config.Bind(Name2, nameof(MoveDeceleration), 0.5f, MoveDecelerationDescription);
        RotateToLookAtPlayerEntry = _config.Bind(Name2, nameof(RotateToLookAtPlayer), true, RotateToLookAtPlayerDescription);
        DelayBeforeLookingAtPlayerEntry = _config.Bind(Name2, nameof(DelayBeforeLookingAtPlayer), 2f, DelayBeforeLookingAtPlayerDescription);
        LookAtPlayerDurationEntry = _config.Bind(Name2, nameof(LookAtPlayerDuration), 3f, LookAtPlayerDurationDescription);
        
        RandomlyMoveWhenLooking_WaitTimeMinEntry = _config.Bind("AI.RandomlyMoveWhenLooking",
            "WaitTimeMin",
            1f,
            "The minimum duration in seconds that the Rolling Giant waits before moving again.");
        RandomlyMoveWhenLooking_WaitTimeMaxEntry = _config.Bind("AI.RandomlyMoveWhenLooking",
            "WaitTimeMax",
            3f,
            "The maximum duration in seconds that the Rolling Giant waits before moving again.");
        RandomlyMoveWhenLooking_RandomMoveTimeMinEntry =
            _config.Bind("AI.RandomlyMoveWhenLooking", "RandomMoveTimeMin", 1f, "The minimum duration in seconds that the Rolling Giant moves for.");
        RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry =
            _config.Bind("AI.RandomlyMoveWhenLooking", "RandomMoveTimeMax", 3f, "The maximum duration in seconds that the Rolling Giant moves for.");

        // looking too long keeps agro ai settings
        LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry = _config.Bind("AI.LookingTooLongKeepsAgro",
            "LookTimeBeforeAgro",
            12f,
            "How long the player can look at the Rolling Giant before it starts chasing in seconds.");
        
        // once seen agro after timer ai settings
        OnceSeenAgroAfterTimer_WaitTimeMinEntry = _config.Bind("AI.OnceSeenAgroAfterTimer",
            "WaitTimeMin",
            15f,
            "The minimum duration in seconds the Rolling Giant waits before chasing the player.");
        OnceSeenAgroAfterTimer_WaitTimeMaxEntry = _config.Bind("AI.OnceSeenAgroAfterTimer",
            "WaitTimeMax",
            30f,
            "The minimum duration in seconds the Rolling Giant waits before chasing the player.");

        if (setValues) {
            GiantScaleInsideMin = GiantScaleInsideMinEntry.Value;
            GiantScaleInsideMax = GiantScaleInsideMaxEntry.Value;
            GiantScaleOutsideMin = GiantScaleOutsideMinEntry.Value;
            GiantScaleOutsideMax = GiantScaleOutsideMaxEntry.Value;
            SpawnIn = SpawnInEntry.Value;
            SpawnInOutsideChance = SpawnInOutsideChanceEntry.Value;
            SpawnInAny = SpawnInAnyEntry.Value;
            SpawnInAnyChance = SpawnInAnyChanceEntry.Value;
            SpawnInAnyOutsideChance = SpawnInAnyOutsideChanceEntry.Value;
            CanSpawnInside = CanSpawnInsideEntry.Value;
            CanSpawnOutside = CanSpawnOutsideEntry.Value;
            DisableOutsideAtNight = DisableOutsideAtNightEntry.Value;
            MaxPerLevel = MaxPerLevelEntry.Value;
            SpawnPosterIn = SpawnPosterInEntry.Value;

            AiType = AiTypeEntry.Value;
            AiTypeChangeOnHourInterval = AiTypeChangeOnHourInterval;
            MoveSpeed = MoveSpeedEntry.Value;
            MoveAcceleration = MoveAccelerationEntry.Value;
            MoveDeceleration = MoveDecelerationEntry.Value;
            RotateToLookAtPlayer = RotateToLookAtPlayerEntry.Value;
            DelayBeforeLookingAtPlayer = DelayBeforeLookingAtPlayerEntry.Value;
            LookAtPlayerDuration = LookAtPlayerDurationEntry.Value;
            
            RandomlyMoveWhenLooking_WaitTimeMin = RandomlyMoveWhenLooking_WaitTimeMinEntry.Value;
            RandomlyMoveWhenLooking_WaitTimeMax = RandomlyMoveWhenLooking_WaitTimeMaxEntry.Value;
            RandomlyMoveWhenLooking_RandomMoveTimeMin = RandomlyMoveWhenLooking_RandomMoveTimeMinEntry.Value;
            RandomlyMoveWhenLooking_RandomMoveTimeMax = RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry.Value;

            LookingTooLongKeepsAgro_LookTimeBeforeAgro = LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry.Value;

            OnceSeenAgroAfterTimer_WaitTimeMin = OnceSeenAgroAfterTimer_WaitTimeMinEntry.Value;
            OnceSeenAgroAfterTimer_WaitTimeMax = OnceSeenAgroAfterTimer_WaitTimeMaxEntry.Value;
            SetCurrentAi();
        }

        Plugin.Log.LogInfo("Config reloaded.");
    }

    private static T GetAttributeOfType<T>(Enum enumVal) where T : Attribute {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T)attributes[0] : null;
    }

    public static SharedAiSettings GetSharedAiSettings() {
        return new SharedAiSettings {
            // aiType = Instance.AiType,
            moveSpeed = Instance.MoveSpeed,
            moveAcceleration = Instance.MoveAcceleration,
            moveDeceleration = Instance.MoveDeceleration,
            rotateToLookAtPlayer = Instance.RotateToLookAtPlayer,
            delayBeforeLookingAtPlayer = Instance.DelayBeforeLookingAtPlayer,
            lookAtPlayerDuration = Instance.LookAtPlayerDuration
        };
    }

    public static void RequestSync() {
        if (!IsClient) {
            Plugin.Log.LogError("Config sync error: Not a client.");
            return;
        }

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage(ROLLINGGIANT_ONREQUESTCONFIGSYNC, 0uL, stream);
        Plugin.Log.LogInfo("Config sync request sent.");
    }

    public static void OnRequestSync(ulong clientId, FastBufferReader _) {
        if (!IsHost) {
            Plugin.Log.LogError("Config sync error: Not a host.");
            return;
        }

        Plugin.Log.LogInfo($"Config sync request received from client: {clientId}");

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try {
            stream.WriteValueSafe(in value);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage(ROLLINGGIANT_ONRECEIVECONFIGSYNC, clientId, stream, NetworkDelivery.ReliableFragmentedSequenced);
            Plugin.Log.LogInfo($"Config sync sent to client: {clientId}");
        }
        catch (Exception e) {
            Plugin.Log.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }

    public static void OnReceiveSync(ulong _, FastBufferReader reader) {
        if (!reader.TryBeginRead(IntSize)) {
            Plugin.Log.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }

        reader.ReadValueSafe(out int val, default);
        if (!reader.TryBeginRead(val)) {
            Plugin.Log.LogError("Config sync error: Host could not sync.");
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);
        SetCurrentAi();
        Plugin.Log.LogInfo("Successfully synced config with host.");
    }

    public static void SetCurrentAi() {
        if (!NetworkHandler.Instance) return;
        var aiType = NetworkHandler.AiType;
        SharedAiSettings = aiType switch {
            RollingGiantAiType.Coilhead => GetSharedAiSettings() with {
                // canWander = Instance.Coilhead_CanWander,
                // chaseMaxDistance = Instance.Coilhead_ChaseMaxDistance
            },
            RollingGiantAiType.InverseCoilhead => GetSharedAiSettings() with {
                // canWander = Instance.InverseCoilhead_CanWander,
                // chaseMaxDistance = Instance.InverseCoilhead_ChaseMaxDistance
            },
            RollingGiantAiType.RandomlyMoveWhileLooking => GetSharedAiSettings() with {
                // canWander = Instance.RandomlyMoveWhenLooking_CanWander,
                // chaseMaxDistance = Instance.RandomlyMoveWhenLooking_ChaseMaxDistance,
                waitTimeMin = Instance.RandomlyMoveWhenLooking_WaitTimeMin,
                waitTimeMax = Instance.RandomlyMoveWhenLooking_WaitTimeMax,
                randomMoveTimeMin = Instance.RandomlyMoveWhenLooking_RandomMoveTimeMin,
                randomMoveTimeMax = Instance.RandomlyMoveWhenLooking_RandomMoveTimeMax
            },
            RollingGiantAiType.LookingTooLongKeepsAgro => GetSharedAiSettings() with {
                lookTimeBeforeAgro = Instance.LookingTooLongKeepsAgro_LookTimeBeforeAgro
            },
            RollingGiantAiType.FollowOnceAgro => GetSharedAiSettings() with {
                // canWander = Instance.FollowOnceAgro_CanWander,
                // chaseMaxDistance = Instance.FollowOnceAgro_ChaseMaxDistance
            },
            RollingGiantAiType.OnceSeenAgroAfterTimer => GetSharedAiSettings() with {
                waitTimeMin = Instance.OnceSeenAgroAfterTimer_WaitTimeMin,
                waitTimeMax = Instance.OnceSeenAgroAfterTimer_WaitTimeMax
            },
            _ => GetSharedAiSettings() with {
                // canWander = Instance.Coilhead_CanWander,
                // chaseMaxDistance = Instance.Coilhead_ChaseMaxDistance
            },
            // _ => throw new ArgumentOutOfRangeException($"I don't support the \"{aiType}\" type?")
        };

        Plugin.Log.LogInfo($"[{aiType}]: {SharedAiSettings}");
        
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            if (RoundManager.Instance) {
                var allSpawnedGiants = RoundManager.Instance.SpawnedEnemies;
                if (allSpawnedGiants == null) return;
                
                Plugin.Log.LogInfo("Resetting all rolling giants!");
                foreach (var enemy in allSpawnedGiants) {
                    if (enemy is RollingGiantAI rollingGiant) {
                        rollingGiant.ResetValues();
                    }
                }
            }
        }
    }
}