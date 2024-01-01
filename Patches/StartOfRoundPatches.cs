using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

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