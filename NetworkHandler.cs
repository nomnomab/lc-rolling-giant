using System;
using System.Collections.Generic;
using System.Linq;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace RollingGiant;

public class NetworkHandler : NetworkBehaviour {
    public static NetworkHandler Instance { get; private set; }
    public static RollingGiantAiType AiType => Instance._aiType.Value;
    private readonly static List<RollingGiantAiType> _aiTypes = Enum.GetValues(typeof(RollingGiantAiType)).Cast<RollingGiantAiType>().ToList();
    
    private NetworkVariable<RollingGiantAiType> _aiType = new();

    private static InputAction _gotoPreviousAiType;
    private static InputAction _gotoNextAiType;
    private static InputAction _reloadConfig;
    
#if DEBUG
    private static InputAction _tpAllToEntrance;
    private static InputAction _tpPlayersToMe;
    private static InputAction _killAllEnemies;
    private static InputAction _spawnGiant;
#endif
    
    public override void OnNetworkSpawn() {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            if (Instance) {
                Instance.gameObject.GetComponent<NetworkObject>().Despawn();
            }
            
            _aiType.Value = CustomConfig.AiType.GetFirst();
        }
        Instance = this;
        
        _gotoPreviousAiType?.Disable();
        _gotoPreviousAiType?.Dispose();
        
        _gotoPreviousAiType = new InputAction("gotoPreviousAiType", InputActionType.Button, CustomConfig.GotoPreviousAiTypeKey.Value);
        _gotoPreviousAiType.Enable();
        
        _gotoNextAiType?.Disable();
        _gotoNextAiType?.Dispose();
        
        _gotoNextAiType = new InputAction("gotoNextAiType", InputActionType.Button, CustomConfig.GotoNextAiTypeKey.Value);
        _gotoNextAiType.Enable();
        
        _reloadConfig?.Disable();
        _reloadConfig?.Dispose();
        
        _reloadConfig = new InputAction("reloadConfig", InputActionType.Button, CustomConfig.ReloadConfigKey.Value);
        _reloadConfig.Enable();
        
#if DEBUG
        _tpAllToEntrance?.Disable();
        _tpAllToEntrance?.Dispose();
        
        _tpAllToEntrance = new InputAction("tpAllToEntrance", InputActionType.Button, "<Keyboard>/numpad1");
        _tpAllToEntrance.Enable();
        
        _tpPlayersToMe?.Disable();
        _tpPlayersToMe?.Dispose();
        
        _tpPlayersToMe = new InputAction("tpPlayersToMe", InputActionType.Button, "<Keyboard>/numpad2");
        _tpPlayersToMe.Enable();
        
        _killAllEnemies?.Disable();
        _killAllEnemies?.Dispose();
        
        _killAllEnemies = new InputAction("killAllEnemies", InputActionType.Button, "<Keyboard>/numpad3");
        _killAllEnemies.Enable();
        
        _spawnGiant?.Disable();
        _spawnGiant?.Dispose();
        
        _spawnGiant = new InputAction("spawnEnemy", InputActionType.Button, "<Keyboard>/numpad5");
        _spawnGiant.Enable();
#endif

        base.OnNetworkSpawn();
    }
    
    public void SetAiType(RollingGiantAiType aiType) {
        if (!(IsServer || IsHost)) return;
        SetNewAiType(aiType, showTip: false);
    }

    private void Update() {
        if (!(IsServer || IsHost)) return;

        if (_gotoPreviousAiType.WasPressedThisFrame()) {
            // previous ai
            var newAi = _aiTypes.IndexOf(_aiType.Value) - 1;
            if (newAi < 0) {
                newAi = Enum.GetValues(typeof(RollingGiantAiType)).Length - 1;
            }

            SetNewAiType(_aiTypes[newAi]);
        } else if (_gotoNextAiType.WasPressedThisFrame()) {
            // next ai
            var newAi = _aiTypes.IndexOf(_aiType.Value) + 1;
            if (newAi >= _aiTypes.Count) {
                newAi = 0;
            }

            SetNewAiType(_aiTypes[newAi]);
        } else if (_reloadConfig.WasPressedThisFrame()) {
            // reload config
            Plugin.Config.Reload();
            CustomConfig.Instance.Reload();
            
            _aiType.Value = CustomConfig.AiType.GetFirst();
            SetNewAiType(_aiType.Value);
            HUDManager.Instance.DisplayTip("Config reloaded", $"Ai defaulted to {_aiType.Value}");
        } 
#if DEBUG
        else if(_spawnGiant.WasPressedThisFrame()) {
            // spawn custom enemy type
            var currentRound = RoundManager.Instance;
            var localPlayer = currentRound.playersManager.localPlayerController;
            var currentLevel = currentRound.currentLevel;
            var isInside = localPlayer.isInsideFactory;
            
            foreach (var enemy in currentLevel.Enemies) {
                if (enemy.enemyType != Plugin.EnemyTypeInside) continue;
                // if (enemy.enemyType.name != "SpringMan") continue;
                if (isInside) {
                    var closestVent = currentRound.allEnemyVents.OrderBy(x => Vector3.Distance(x.floorNode.position, localPlayer.transform.position)).FirstOrDefault();
                    var ventPosition = closestVent.floorNode.position;
                    currentRound.SpawnEnemyOnServer(ventPosition, currentRound.allEnemyVents[0].floorNode.eulerAngles.y, currentLevel.Enemies.IndexOf(enemy));
                } else {
                    var outsideNodesWithTag = GameObject.FindGameObjectsWithTag("OutsideAINode");
                    var outsideNode = outsideNodesWithTag[Random.Range(0, outsideNodesWithTag.Length - 1)]
                        .transform.position;
                    var prefab = currentLevel.OutsideEnemies[currentLevel.OutsideEnemies.IndexOf(enemy)].enemyType.enemyPrefab;
                    var instance = Instantiate(prefab, outsideNode, Quaternion.Euler(Vector3.zero));
                    instance.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                }
                
                HUDManager.Instance.DisplayTip("Spawned enemy", $"{enemy.enemyType.name}\n{_aiType.Value}");
                return;
            }
            
            HUDManager.Instance.DisplayTip("Failed", "No enemy found", isWarning: true);
        }
        else if (_tpAllToEntrance.WasPressedThisFrame()) {
            // tp players to entrance
            var activeAndInactive = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var entrance = activeAndInactive.FirstOrDefault(x => x && x.name.ToLower().Contains("teleports"));
            if (!entrance) {
                Plugin.Log.LogInfo($"No entrance found");
                return;
            }
            
            for (int i = 0; i < entrance.childCount; i++) {
                var child = entrance.GetChild(i);
                if (child.name != "EntranceTeleportA") continue;
                TeleportToClientRpc(child.position, RoundManager.Instance.playersManager.localPlayerController.isInsideFactory);
                RoundManager.Instance.playersManager.localPlayerController.TeleportPlayer(child.position);
            }
            
            HUDManager.Instance.DisplayTip("Teleport", "Teleported players to entrance");
        } 
        else if (_tpPlayersToMe.WasPressedThisFrame()) {
            // tp players to me
            var player = RoundManager.Instance.playersManager.localPlayerController;
            TeleportToClientRpc(player.transform.position, player.isInsideFactory);
            HUDManager.Instance.DisplayTip("Teleport", "Teleported players to you");
        } else if (_killAllEnemies.WasPressedThisFrame()) {
            // kill all enemies
            var enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies) {
                enemy.KillEnemyOnOwnerClient();
            }
            HUDManager.Instance.DisplayTip("Kill all enemies", "Killed all enemies");
        }
#endif
    }

    private void SetNewAiType(RollingGiantAiType aiType, bool showTip = true) {
        var lastAiType = _aiType.Value;
        _aiType.Value = aiType;
        CustomConfig.SetCurrentAi();
        EmitSharedServerSettingsClientRpc();
        if (HUDManager.Instance && lastAiType != aiType && showTip) {
            HUDManager.Instance.DisplayTip("Rolling Giant AI changed", aiType.ToString());
        }
        
        Plugin.Log.LogMessage($"Rolling Giant AI changed: {aiType}");
    }
    
    [ClientRpc]
    private void EmitSharedServerSettingsClientRpc() {
        CustomConfig.RequestSync();
    }
    
#if DEBUG
    [ClientRpc]
    private void TeleportToClientRpc(Vector3 position, bool insideFactory) {
        if (IsServer || IsHost) return;
        var player = RoundManager.Instance.playersManager.localPlayerController;
        if (player.isPlayerDead || !player.IsSpawned) return;
        player.TeleportPlayer(position);
        player.isInsideFactory = insideFactory;
    }
#endif
}