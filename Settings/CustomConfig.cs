using System;
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
    public static ConfigEntry<bool> SpawnInAnyEntry { get; private set; }
    public static ConfigEntry<int> SpawnInAnyChanceEntry { get; private set; }
    public static ConfigEntry<bool> CanSpawnInsideEntry { get; private set; }
    public static ConfigEntry<bool> CanSpawnOutsideEntry { get; private set; }
    public static ConfigEntry<bool> DisableOutsideAtNightEntry { get; private set; }
    public static ConfigEntry<string> SpawnPosterInEntry { get; private set; }

    // general settings
    public const string Name1 = "1. General Settings";
    public float GiantScaleMin { get; private set; }
    public float GiantScaleMax { get; private set; }
    public static string SpawnIn { get; private set; }
    public static bool SpawnInAny { get; private set; }
    public static int SpawnInAnyChance { get; private set; }
    public static bool CanSpawnInside { get; private set; }
    public static bool CanSpawnOutside { get; private set; }
    public static bool DisableOutsideAtNight { get; private set; }
    public static string SpawnPosterIn { get; private set; }

    // ai settings
    public const string Name2 = "2. AI Settings";
    public const string AiTypeDescription =
        "The AI type of the Rolling Giant.\nCoilhead = Coilhead AI\nInverseCoilhead = Move when player is looking at it\nRandomlyMoveWhileLooking = Randomly move while the player is looking at it\nLookingTooLongKeepsAgro = If the player looks at it for too long it doesn't stop chasing\nFollowOnceAgro = Once the player is noticed, the Rolling Giant will follow the player constantly\nOnceSeenAgroAfterTimer = Once the player sees the Rolling Giant, it will chase the player after a timer";

    public RollingGiantAiType AiType { get; internal set; }

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

    // coilhead ai settings
    // public bool Coilhead_CanWander { get; private set; }
    // public float Coilhead_ChaseMaxDistance { get; private set; }

    // inverse coilhead ai settings
    // public bool InverseCoilhead_CanWander { get; private set; }
    // public float InverseCoilhead_ChaseMaxDistance { get; private set; }

    // randomly move while looking ai settings
    // public bool RandomlyMoveWhenLooking_CanWander { get; private set; }
    // public float RandomlyMoveWhenLooking_ChaseMaxDistance { get; private set; }
    public float RandomlyMoveWhenLooking_WaitTimeMin { get; private set; }
    public float RandomlyMoveWhenLooking_WaitTimeMax { get; private set; }
    public float RandomlyMoveWhenLooking_RandomMoveTimeMin { get; private set; }
    public float RandomlyMoveWhenLooking_RandomMoveTimeMax { get; private set; }

    // looking too long keeps agro ai settings
    public float LookingTooLongKeepsAgro_LookTimeBeforeAgro { get; private set; }

    // follow once agro ai settings
    // public bool FollowOnceAgro_CanWander { get; private set; }
    // public float FollowOnceAgro_ChaseMaxDistance { get; private set; }

    // once seen agro after timer ai settings
    public float OnceSeenAgroAfterTimer_WaitTimeMin { get; private set; }
    public float OnceSeenAgroAfterTimer_WaitTimeMax { get; private set; }

    // binds
    public static ConfigEntry<float> GiantScaleMinEntry { get; private set; }
    public static ConfigEntry<float> GiantScaleMaxEntry { get; private set; }

    public static ConfigEntry<RollingGiantAiType> AiTypeEntry { get; private set; }
    public static ConfigEntry<float> MoveSpeedEntry { get; private set; }
    public static ConfigEntry<float> MoveAccelerationEntry { get; private set; }
    public static ConfigEntry<float> MoveDecelerationEntry { get; private set; }
    public static ConfigEntry<bool> RotateToLookAtPlayerEntry { get; private set; }
    public static ConfigEntry<float> DelayBeforeLookingAtPlayerEntry { get; private set; }
    public static ConfigEntry<float> LookAtPlayerDurationEntry { get; private set; }

    // public static ConfigEntry<bool> Coilhead_CanWanderEntry { get; private set; }
    // public static ConfigEntry<float> Coilhead_ChaseMaxDistanceEntry { get; private set; }

    // public static ConfigEntry<bool> InverseCoilhead_CanWanderEntry { get; private set; }
    // public static ConfigEntry<float> InverseCoilhead_ChaseMaxDistanceEntry { get; private set; }

    // public static ConfigEntry<bool> RandomlyMoveWhenLooking_CanWanderEntry { get; private set; }
    // public static ConfigEntry<float> RandomlyMoveWhenLooking_ChaseMaxDistanceEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_WaitTimeMinEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_WaitTimeMaxEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_RandomMoveTimeMinEntry { get; private set; }
    public static ConfigEntry<float> RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry { get; private set; }

    public static ConfigEntry<float> LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry { get; private set; }

    // public static ConfigEntry<bool> FollowOnceAgro_CanWanderEntry { get; private set; }
    // public static ConfigEntry<float> FollowOnceAgro_ChaseMaxDistanceEntry { get; private set; }

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
        GiantScaleMinEntry.Value = GiantScaleMin;
        GiantScaleMaxEntry.Value = GiantScaleMax;
        SpawnInEntry.Value = SpawnIn;
        SpawnInAnyEntry.Value = SpawnInAny;
        SpawnInAnyChanceEntry.Value = SpawnInAnyChance;
        CanSpawnInsideEntry.Value = CanSpawnInside;
        CanSpawnOutsideEntry.Value = CanSpawnOutside;
        DisableOutsideAtNightEntry.Value = DisableOutsideAtNight;
        SpawnPosterInEntry.Value = SpawnPosterIn;

        AiTypeEntry.Value = AiType;
        MoveSpeedEntry.Value = MoveSpeed;
        MoveAccelerationEntry.Value = MoveAcceleration;
        MoveDecelerationEntry.Value = MoveDeceleration;
        RotateToLookAtPlayerEntry.Value = RotateToLookAtPlayer;
        DelayBeforeLookingAtPlayerEntry.Value = DelayBeforeLookingAtPlayer;
        LookAtPlayerDurationEntry.Value = LookAtPlayerDuration;

        // Coilhead_CanWanderEntry.Value = Coilhead_CanWander;
        // Coilhead_ChaseMaxDistanceEntry.Value = Coilhead_ChaseMaxDistance;

        // InverseCoilhead_CanWanderEntry.Value = InverseCoilhead_CanWander;
        // InverseCoilhead_ChaseMaxDistanceEntry.Value = InverseCoilhead_ChaseMaxDistance;

        // RandomlyMoveWhenLooking_CanWanderEntry.Value = RandomlyMoveWhenLooking_CanWander;
        // RandomlyMoveWhenLooking_ChaseMaxDistanceEntry.Value = RandomlyMoveWhenLooking_ChaseMaxDistance;
        RandomlyMoveWhenLooking_WaitTimeMinEntry.Value = RandomlyMoveWhenLooking_WaitTimeMin;
        RandomlyMoveWhenLooking_WaitTimeMaxEntry.Value = RandomlyMoveWhenLooking_WaitTimeMax;
        RandomlyMoveWhenLooking_RandomMoveTimeMinEntry.Value = RandomlyMoveWhenLooking_RandomMoveTimeMin;
        RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry.Value = RandomlyMoveWhenLooking_RandomMoveTimeMax;

        LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry.Value = LookingTooLongKeepsAgro_LookTimeBeforeAgro;

        // FollowOnceAgro_CanWanderEntry.Value = FollowOnceAgro_CanWander;
        // FollowOnceAgro_ChaseMaxDistanceEntry.Value = FollowOnceAgro_ChaseMaxDistance;

        OnceSeenAgroAfterTimer_WaitTimeMinEntry.Value = OnceSeenAgroAfterTimer_WaitTimeMin;
        OnceSeenAgroAfterTimer_WaitTimeMaxEntry.Value = OnceSeenAgroAfterTimer_WaitTimeMax;
    }

    public void Reload(bool setValues = true) {
        // general settings
        // ChanceForGiantEntry =_config.Bind(Name1, nameof(ChanceForGiant), 0.4f, "0.0-1.0: Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant.");
        GiantScaleMinEntry = _config.Bind(Name1, nameof(GiantScaleMin), 0.9f, "The minimum scale of the Rolling Giant.\nThis changes how small the Giant can be.\nThis is a multiplier, so 0.5 is half as large.");
        GiantScaleMaxEntry = _config.Bind(Name1, nameof(GiantScaleMax), 1.1f, "The maximum scale of the Rolling Giant.\nThis changes how big the Giant can be.\nThis is a multiplier, so 2 is twice as large.");

        SpawnInEntry = _config.Bind(Name1,
            nameof(SpawnIn),
            "Vow:45,March:45,Rend:54,Dine:65,Offense:45,Titan:65",
            "Where the Rolling Giant can spawn.\nSeparate each level with a comma, and put a chance (no decimals) separated by a colon.\nVanilla caps at 100, but you can go farther.\nThis chance is also a weight, not a percentage.\nHigher chance = higher chance to get picked\nThe names are what you see in the terminal\nExample: Vow:6,March:10");
        SpawnInAnyEntry = _config.Bind(Name1,
            nameof(SpawnInAny),
            false,
            "If the Rolling Giant can spawn in any level.");
        SpawnInAnyChanceEntry = _config.Bind(Name1,
            nameof(SpawnInAnyChance),
            45,
            "The chance for the Rolling Giant to spawn in any level.\nRequires SpawnInAny to be enabled!\nThis is a weight, not a percentage.\nHigher chance = higher chance to get picked");
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
        AiTypeEntry = _config.Bind(Name2, nameof(AiType), RollingGiantAiType.RandomlyMoveWhileLooking, AiTypeDescription);
        MoveSpeedEntry = _config.Bind(Name2, nameof(MoveSpeed), 6f, MoveSpeedDescription);
        MoveAccelerationEntry = _config.Bind(Name2, nameof(MoveAcceleration), 2f, MoveAccelerationDescription);
        MoveDecelerationEntry = _config.Bind(Name2, nameof(MoveDeceleration), 0.5f, MoveDecelerationDescription);
        RotateToLookAtPlayerEntry = _config.Bind(Name2, nameof(RotateToLookAtPlayer), true, RotateToLookAtPlayerDescription);
        DelayBeforeLookingAtPlayerEntry = _config.Bind(Name2, nameof(DelayBeforeLookingAtPlayer), 2f, DelayBeforeLookingAtPlayerDescription);
        LookAtPlayerDurationEntry = _config.Bind(Name2, nameof(LookAtPlayerDuration), 3f, LookAtPlayerDurationDescription);

        // coilhead ai settings
        // Coilhead_CanWanderEntry =_config.Bind("AI.Coilhead",
        //     "CanWander",
        //     true,
        //     "If the Rolling Giant can go back to wandering after the player gets far enough away from it.");
        // Coilhead_ChaseMaxDistanceEntry =_config.Bind("AI.Coilhead",
        //     "ChaseMaxDistance",
        //     50f,
        //     "The distance in meters between the Rolling Giant and the player before it stops chasing and goes back to wandering.");

        // inverse coilhead ai settings
        // InverseCoilhead_CanWanderEntry =_config.Bind("AI.InverseCoilhead",
        //     "CanWander",
        //     true,
        //     "If the Rolling Giant can go back to wandering after the player gets far enough away from it.");
        // InverseCoilhead_ChaseMaxDistanceEntry =_config.Bind("AI.InverseCoilhead",
        //     "ChaseMaxDistance",
        //     50f,
        //     "The distance in meters between the Rolling Giant and the player before it stops chasing and goes back to wandering.");

        // randomly move while looking ai settings
        // RandomlyMoveWhenLooking_CanWanderEntry =_config.Bind("AI.RandomlyMoveWhenLooking",
        //     "CanWander",
        //     true,
        //     "If the Rolling Giant can go back to wandering after the player gets far enough away from it.");
        // RandomlyMoveWhenLooking_ChaseMaxDistanceEntry =_config.Bind("AI.RandomlyMoveWhenLooking",
        //     "ChaseMaxDistance",
        //     50f,
        //     "The distance in meters between the Rolling Giant and the player before it stops chasing and goes back to wandering.");
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
            10f,
            "How long the player can look at the Rolling Giant before it starts chasing in seconds.");

        // follow once agro ai settings
        // FollowOnceAgro_CanWanderEntry =_config.Bind("AI.FollowOnceAgro",
        //     "CanWander",
        //     true,
        //     "If the Rolling Giant can go back to wandering after the player gets far enough away from it.");
        // FollowOnceAgro_ChaseMaxDistanceEntry =_config.Bind("AI.FollowOnceAgro",
        //     "ChaseMaxDistance",
        //     50f,
        //     "The distance in meters between the Rolling Giant and the player before it stops chasing and goes back to wandering.");

        // once seen agro after timer ai settings
        OnceSeenAgroAfterTimer_WaitTimeMinEntry = _config.Bind("AI.OnceSeenAgroAfterTimer",
            "WaitTimeMin",
            30f,
            "The minimum duration in seconds the Rolling Giant waits before chasing the player.");
        OnceSeenAgroAfterTimer_WaitTimeMaxEntry = _config.Bind("AI.OnceSeenAgroAfterTimer",
            "WaitTimeMax",
            60f,
            "The minimum duration in seconds the Rolling Giant waits before chasing the player.");

        if (setValues) {
            GiantScaleMin = GiantScaleMinEntry.Value;
            GiantScaleMax = GiantScaleMaxEntry.Value;
            SpawnIn = SpawnInEntry.Value;
            SpawnInAny = SpawnInAnyEntry.Value;
            SpawnInAnyChance = SpawnInAnyChanceEntry.Value;
            CanSpawnInside = CanSpawnInsideEntry.Value;
            CanSpawnOutside = CanSpawnOutsideEntry.Value;
            DisableOutsideAtNight = DisableOutsideAtNightEntry.Value;
            SpawnPosterIn = SpawnPosterInEntry.Value;

            AiType = AiTypeEntry.Value;
            MoveSpeed = MoveSpeedEntry.Value;
            MoveAcceleration = MoveAccelerationEntry.Value;
            MoveDeceleration = MoveDecelerationEntry.Value;
            RotateToLookAtPlayer = RotateToLookAtPlayerEntry.Value;
            DelayBeforeLookingAtPlayer = DelayBeforeLookingAtPlayerEntry.Value;
            LookAtPlayerDuration = LookAtPlayerDurationEntry.Value;

            // Coilhead_CanWander = Coilhead_CanWanderEntry.Value;
            // Coilhead_ChaseMaxDistance = Coilhead_ChaseMaxDistanceEntry.Value;
            //
            // InverseCoilhead_CanWander = InverseCoilhead_CanWanderEntry.Value;
            // InverseCoilhead_ChaseMaxDistance = InverseCoilhead_ChaseMaxDistanceEntry.Value;
            //
            // RandomlyMoveWhenLooking_CanWander = RandomlyMoveWhenLooking_CanWanderEntry.Value;
            // RandomlyMoveWhenLooking_ChaseMaxDistance = RandomlyMoveWhenLooking_ChaseMaxDistanceEntry.Value;
            RandomlyMoveWhenLooking_WaitTimeMin = RandomlyMoveWhenLooking_WaitTimeMinEntry.Value;
            RandomlyMoveWhenLooking_WaitTimeMax = RandomlyMoveWhenLooking_WaitTimeMaxEntry.Value;
            RandomlyMoveWhenLooking_RandomMoveTimeMin = RandomlyMoveWhenLooking_RandomMoveTimeMinEntry.Value;
            RandomlyMoveWhenLooking_RandomMoveTimeMax = RandomlyMoveWhenLooking_RandomMoveTimeMaxEntry.Value;

            LookingTooLongKeepsAgro_LookTimeBeforeAgro = LookingTooLongKeepsAgro_LookTimeBeforeAgroEntry.Value;

            // FollowOnceAgro_CanWander = FollowOnceAgro_CanWanderEntry.Value;
            // FollowOnceAgro_ChaseMaxDistance = FollowOnceAgro_ChaseMaxDistanceEntry.Value;

            OnceSeenAgroAfterTimer_WaitTimeMin = OnceSeenAgroAfterTimer_WaitTimeMinEntry.Value;
            OnceSeenAgroAfterTimer_WaitTimeMax = OnceSeenAgroAfterTimer_WaitTimeMaxEntry.Value;
            SetCurrentAi();
        }

        Plugin.Log.LogInfo("Config reloaded.");
    }

    public static SharedAiSettings GetSharedAiSettings() {
        return new SharedAiSettings {
            aiType = Instance.AiType,
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

    // public static void RequestFullSync() {
    //     if (!IsHost) return;
    //     using FastBufferWriter stream = new(IntSize, Allocator.Temp);
    //     foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
    //         MessageManager.SendNamedMessage(ROLLINGGIANT_ONREQUESTCONFIGSYNC, client.ClientId, stream);
    //     }
    //     Plugin.Log.LogInfo("Config sync request sent.");
    // }

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
        SharedAiSettings = Instance.AiType switch {
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
            _ => throw new ArgumentOutOfRangeException()
        };

        Plugin.Log.LogInfo($"Current AI type: {SharedAiSettings}");
    }
}