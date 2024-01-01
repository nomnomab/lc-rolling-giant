using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public static class NetworkPatches {
    private readonly static List<GameObject> _prefabs = new();
    private static GameObject _networkPrefab;

    public static void RegisterPrefab(GameObject prefab) {
        _prefabs.Add(prefab);
    }
    
    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    [HarmonyPostfix]
    private static void RegisterPrefabs() {
        foreach (var prefab in _prefabs) {
            NetworkManager.Singleton.AddNetworkPrefab(prefab);
        }
        
        var networkPrefab = (GameObject)Plugin.Bundle.LoadAsset("Assets/RollingGiant/NetworkObjectRoot.prefab");
        networkPrefab.AddComponent<NetworkHandler>();
        _networkPrefab = networkPrefab;
    
        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab); 
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
    static void SpawnNetworkHandler() {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            var networkHandlerHost = Object.Instantiate(_networkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn();
        }
    }

    // [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
    // [HarmonyPostfix]
    // private static void EmitSharedServerSettings() {
    //     NetworkHandler.Instance.EmitSharedServerSettingsServerRpc();
    // }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    // [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyMemberJoined")]
    public static void InitializeLocalPlayer() {
        if (CustomConfig.IsHost) {
            try {
                CustomConfig.MessageManager.RegisterNamedMessageHandler(CustomConfig.ROLLINGGIANT_ONREQUESTCONFIGSYNC, CustomConfig.OnRequestSync);
                CustomConfig.Synced = true;
            }
            catch (Exception e) {
                Plugin.Log.LogError(e);
            }
            return;
        }

        CustomConfig.Synced = false;
        CustomConfig.MessageManager.RegisterNamedMessageHandler(CustomConfig.ROLLINGGIANT_ONRECEIVECONFIGSYNC, CustomConfig.OnReceiveSync);
        CustomConfig.RequestSync();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    public static void PlayerLeave() {
        CustomConfig.RevertSync();
        CustomConfig.SetCurrentAi();
        Plugin.Log.LogMessage("Reverting sync");
    }
}