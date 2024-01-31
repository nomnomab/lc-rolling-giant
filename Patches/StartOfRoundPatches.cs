using System.Linq;
using HarmonyLib;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public class StartOfRoundPatches {
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void AddCustomRagdoll(StartOfRound __instance) {
        var ragdolls = __instance.playerRagdolls;
        var playerRagdoll = Plugin.PlayerRagdoll;
        if (playerRagdoll && !ragdolls.Contains(playerRagdoll)) {
            ragdolls.Add(playerRagdoll);
        }

        RandomizeAiType();
    }

    [HarmonyPatch(typeof(StartOfRound), "StartGame")]
    [HarmonyPostfix]
    private static void RandomizeAiType() {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            // select a random ai type on start
            var aiType = CustomConfig.AiType.GetRandom(seedOffset: 0);
            NetworkHandler.Instance.SetAiType(aiType);

            if (TimeOfDay.Instance) {
                TimeOfDay.Instance.onTimeSync.RemoveListener(OnTimeSync);
                _lastHour = 0;
                if (CustomConfig.AiTypeChangeOnHourInterval != 0) {
                    Plugin.Log.LogMessage($"Setting up time sync for ai type change every {CustomConfig.AiTypeChangeOnHourInterval} hours");
                    TimeOfDay.Instance.onTimeSync.AddListener(OnTimeSync);
                }
            }
        }
    }
    
    private static int _lastHour;
    private static bool InLevel => StartOfRound.Instance && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevelID != 3;
    
    private static void OnTimeSync() {
        if (!InLevel) {
            _lastHour = 0;
            TimeOfDay.Instance.onTimeSync.RemoveListener(OnTimeSync);
            return;
        }
        
        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) {
            return;
        }
        
        var interval = CustomConfig.AiTypeChangeOnHourInterval;
        if (interval == 0) {
            return;
        }

        var time = TimeOfDay.Instance.hour;
        var difference = time - _lastHour;
        if (difference >= interval) {
            _lastHour = time;
            
            var aiType = CustomConfig.AiType.GetRandom(seedOffset: time);
            NetworkHandler.Instance.SetAiType(aiType);
        }
    }
    
    // [HarmonyPatch(typeof(PlayerControllerB), "SpawnDeadBody")]
    // [HarmonyPrefix]
    // private static bool SpawnCustomRagdoll(PlayerControllerB __instance, ref int deathAnimation) {
    //     if (deathAnimation == 2) {
    //         deathAnimation = StartOfRound.Instance.playerRagdolls.IndexOf(Plugin.PlayerRagdoll);
    //     }
    // }
    
    [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
    [HarmonyPostfix]
    private static void SetCustomRagdoll(DeadBodyInfo __instance) {
        if (!__instance.TryGetComponent(out RollingGiantDeadBody _)) return;
        // replace the material because internally it changes it to default orange
        var playerRagdoll = Plugin.PlayerRagdoll;
        var material = playerRagdoll.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
        __instance.GetComponent<SkinnedMeshRenderer>().sharedMaterial = material;
    }

    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    [HarmonyPostfix]
    private static void FixAudioSources() {
        var referenceAudioSource = GameNetworkManager.Instance.GetComponent<NetworkManager>().NetworkConfig.Prefabs.Prefabs
            .Select(p => p.Prefab.GetComponentInChildren<NoisemakerProp>())
            .Where(p => p != null)
            .Select(p => p.GetComponentInChildren<AudioSource>())
            .Where(p => p != null)
            .FirstOrDefault();

        if (!referenceAudioSource) {
            Plugin.Log.LogError("Failed to find reference audio source");
            return;
        }

        var mixerGroup = referenceAudioSource.outputAudioMixerGroup;
        fix(Plugin.EnemyTypeInside.enemyPrefab);
        fix(Plugin.PlayerRagdoll);
        fix(Plugin.PosterItem.spawnPrefab);

        void fix(GameObject target) {
            foreach (var audioSource in target.GetComponentsInChildren<AudioSource>()) {
                audioSource.outputAudioMixerGroup = mixerGroup;
            }
        }
    }
}