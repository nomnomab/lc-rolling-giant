using GameNetcodeStuff;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant;

public class RollingGiantAI : EnemyAI {
   private const float ROAMING_AUDIO_PERCENT = 0.4f;

#pragma warning disable 649
   [SerializeField] private AISearchRoutine _searchForPlayers;
   [SerializeField] private Collider _mainCollider;
   [SerializeField] private AudioClip[] _stopNoises;
#pragma warning restore 649

   private static RollingGiantAiType _aiType => NetworkHandler.AiType;
   private static SharedAiSettings _sharedAiSettings => CustomConfig.SharedAiSettings;
   
   private AudioSource _rollingSFX;
   private NetworkVariable<float> _velocity = new();
   private NetworkVariable<float> _waitTimer = new();
   private NetworkVariable<float> _moveTimer = new();
   private NetworkVariable<float> _lookTimer = new();
   private NetworkVariable<float> _agroTimer = new();

   private float _timeSinceHittingPlayer;
   private bool _wantsToChaseThisClient;
   private bool _hasEnteredChaseState;
   private bool _wasStopped;
   private bool _wasFeared;
   private bool _isAgro;
   private float _lastSpeed;
   private bool _tooBig;

   private static float NextDouble() {
      if (!RoundManager.Instance || RoundManager.Instance.LevelRandom == null) {
         return Random.value;
      }
      
      return (float)RoundManager.Instance.LevelRandom.NextDouble();
   }

   public override void Start() {
      base.Start();
      
      Init(transform.localScale.x);
      
      if (IsHost || IsOwner) {
         AssignInitData_LocalClient();
      }
      
      Plugin.Log.LogInfo($"Rolling giant spawned with ai type: {NetworkHandler.AiType}, owner? {IsOwner}");
   }

   private void Init(float scale) {
      agent = gameObject.GetComponentInChildren<NavMeshAgent>();
      _rollingSFX = transform.Find("RollingSFX").GetComponent<AudioSource>();
      
      var mixer = SoundManager.Instance.diageticMixer.outputAudioMixerGroup;
      _rollingSFX.outputAudioMixerGroup = mixer;
      creatureVoice.outputAudioMixerGroup = mixer;
      creatureSFX.outputAudioMixerGroup = mixer;
      
      _rollingSFX.loop = true;
      _rollingSFX.clip = Plugin.WalkSound;
      
      var time = NextDouble() * Plugin.WalkSound.length;
      _rollingSFX.time = time;
      _rollingSFX.pitch = Mathf.Lerp(1.1f, 0.8f, Mathf.InverseLerp(0.9f, 1.2f, scale));
      _rollingSFX.volume = 0;
      _rollingSFX.Play();
      
      isOutside = enemyType.isOutsideEnemy || enemyType.isDaytimeEnemy;
      if (isOutside) {
         allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
      }
   }

   public override void DaytimeEnemyLeave() {
      base.DaytimeEnemyLeave();
      
      foreach (var renderer in transform.GetComponentsInChildren<Renderer>()) {
         if (renderer.name == "object_3") continue;
         renderer.sharedMaterial = Plugin.BlackAndWhiteMaterial;
      }

      _mainCollider.isTrigger = true;
   }

   public override void DoAIInterval() {
      if (daytimeEnemyLeaving) {
         _mainCollider.isTrigger = true;
         return;
      }
      
      base.DoAIInterval();
      
      if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead) return;
      
      // Debug.Log($"{agent.speed}, {_velocity.Value}");
   
      switch (currentBehaviourStateIndex) {
         // searching
         case 0:
            if (!IsServer) {
               ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               break;
            }
   
            if (!_searchForPlayers.inProgress) {
               StartSearch(transform.position, _searchForPlayers);
               // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StartSearch({transform.position}, _searchForPlayers)");
               return;
            }

            foreach (var player in StartOfRound.Instance.allPlayerScripts) {
               if (player.isPlayerDead) continue;
               if (!player.IsSpawned) continue;
               if (!isOutside != player.isInsideFactory) continue;
               
               // if (PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside) &&
               if (!isOutside && !PlayerIsTargetable(player)) continue;

               var distance = Vector3.Distance(transform.position, player.transform.position);
               var inRange = distance < (isOutside ? 90 : 30);
               if (Physics.Linecast(transform.position + Vector3.up * 0.5f, player.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && inRange) {
                  SwitchToBehaviourState(1);
                  // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] SwitchToBehaviourState(1), found {player?.playerUsername} at distance {distance}m");
                  return;
               }
            }
            
            break;
         // chasing
         case 1:
            // not in range of any player, so go back to wandering
            // if (!AmIInRangeOfAPlayer(out _)) {
            if (!TargetClosestPlayer()) {
               movingTowardsTargetPlayer = false;
               if (_searchForPlayers.inProgress) {
                  break;
               }
               // _searchForPlayers.searchWidth = 30f;
               // StartSearch(transform.position, _searchForPlayers);
               SwitchToBehaviourState(0);
               // ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] lost player; StartSearch({transform.position}, _searchForPlayers)");
               break;
            }
   
            if (!_searchForPlayers.inProgress) {
               break;
            }
   
            // stop the current search as we found a player!
            StopSearch(_searchForPlayers);
            movingTowardsTargetPlayer = true;
            // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StopSearch(_searchForPlayers), found {targetPlayer?.playerUsername}");
            break;
      }
   }
   
   public override void Update() {
      if (daytimeEnemyLeaving) {
         _mainCollider.isTrigger = true;
         return;
      }
   
      if (IsHost || IsServer) {
         _velocity.Value = agent.velocity.magnitude;
      }
   
      base.Update();
   
      if (isEnemyDead) return;
   
      _lastSpeed = agent.velocity.magnitude;
      CalculateAgentSpeed();
      _timeSinceHittingPlayer += Time.deltaTime;
   
      // if (!IsOwner) {
      //    Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] !IsOwner, {agent.isOnNavMesh}, {agent.isActiveAndEnabled}");
      // }
   
      // _mainCollider.isTrigger = !_wasStopped;
      _mainCollider.isTrigger = _velocity.Value > 0.01f;
   
      // var speed = !(IsOwner || IsHost) ? _velocity.Value : agent.velocity.magnitude;
      var speed = _velocity.Value;
      // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] speed: {speed}/{_sharedAiSettings.moveSpeed}, moveTowardsDestination: {moveTowardsDestination}: movingTowardsTargetPlayer: {movingTowardsTargetPlayer}, onNavmesh: {agent.isOnNavMesh}, isActiveAndEnabled: {agent.isActiveAndEnabled}");
      // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] speed: {speed}/{_sharedAiSettings.moveSpeed}, _rollingSFX.volume: {_rollingSFX.volume}");
      if (speed > 0.1f) {
         _rollingSFX.volume = Mathf.Lerp(0, Mathf.Clamp01(ROAMING_AUDIO_PERCENT * speed + 0.05f), speed / _sharedAiSettings.moveSpeed);
      } else {
         _rollingSFX.volume = Mathf.Lerp(_rollingSFX.volume, 0, Time.deltaTime);
      }

      // if (_wasStopped) {
      //    _audioFade -= Time.deltaTime;
      //    _audioFade = Mathf.Clamp01(_audioFade);
      //
      //    _rollingSFX.volume = SmoothLerp(0, _rollingSFX.volume, _audioFade);
      //    // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] stopped; agent.speed: {agent.speed}, velocity: {agent.velocity.magnitude}, other: {_velocity.Value}, _rollingSFX.volume: {_rollingSFX.volume}");
      // } else {
      //    _audioFade += Time.deltaTime;
      //    _audioFade = Mathf.Clamp01(_audioFade);
      //
      //    _rollingSFX.volume = SmoothLerp(_rollingSFX.volume, Mathf.Clamp01(ROAMING_AUDIO_PERCENT * speed + 0.05f), _audioFade);
      //    // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] rolling; agent.speed: {agent.speed}, velocity: {agent.velocity.magnitude}, other: {_velocity.Value}, _rollingSFX.volume: {_rollingSFX.volume}");
      // }
   
      var gameNetworkManager = GameNetworkManager.Instance;
      var localPlayer = gameNetworkManager.localPlayerController;
      if (_wasStopped && !_wasFeared) {
         if (localPlayer.HasLineOfSightToPosition(eye.position, 70, 25)) {
            _wasFeared = true;
            // Plugin.Log.LogInfo($"[Update] feared");
   
            var distance = Vector3.Distance(transform.position, localPlayer.transform.position);
            if (distance < 4) {
               gameNetworkManager.localPlayerController.JumpToFearLevel(0.9f);
            } else if (distance < 9) {
               gameNetworkManager.localPlayerController.JumpToFearLevel(0.4f);
            }
   
            if (_lastSpeed > 1) {
               RoundManager.PlayRandomClip(creatureVoice, _stopNoises, false);
               // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] _lastSpeed: {_lastSpeed}");
            }
   
            // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] _wasStopped: {_wasStopped}, _wasFeared: {_wasFeared}, _lastSpeed: {_lastSpeed}");
         }
      }
   
      switch (currentBehaviourStateIndex) {
         // searching
         case 0:
         {
            if (_hasEnteredChaseState) {
               _hasEnteredChaseState = false;
               _wantsToChaseThisClient = false;
               _wasStopped = false;
               _wasFeared = false;
               agent.speed = 0;
               if (_aiType is not RollingGiantAiType.OnceSeenAgroAfterTimer) {
                  _isAgro = false;
                  if (IsOwner) {
                     _agroTimer.Value = 0;
                  }
               }
               if (IsOwner) {
                  _waitTimer.Value = 0;
                  _moveTimer.Value = 0;
                  _lookTimer.Value = 0;
               }
               // _springVelocity = 0;
               // agent.stoppingDistance = 0;
               // agent.speed = _sharedAiSettings.moveSpeed;
               // agent.acceleration = 200;
            }
            
            if (IsOwner) {
               //if (AmIInRangeOfAPlayer(out var closestPlayer) && closestPlayer == localPlayer) {
               if (TargetClosestPlayer(requireLineOfSight: true)) {
                  if (_wantsToChaseThisClient) {
                     break;
                  }

                  _wantsToChaseThisClient = true;
                  BeginChasingPlayer_ServerRpc((int)targetPlayer.playerClientId);
                  // ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] began chasing local player {targetPlayer?.playerUsername}");
               } else {
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] not in range; SwitchToBehaviourState(0); {targetPlayer}");
               }
            }
         }
            break;
         // chasing
         case 1:
         {
            if (!_hasEnteredChaseState) {
               _hasEnteredChaseState = true;
               _wantsToChaseThisClient = false;
               _wasStopped = false;
               _wasFeared = false;
               if (_aiType is not RollingGiantAiType.OnceSeenAgroAfterTimer) {
                  _isAgro = false;
                  if (IsOwner) {
                     _agroTimer.Value = 0;
                  }
               }
               if (IsOwner) {
                  _waitTimer.Value = 0;
                  _moveTimer.Value = 0;
                  _lookTimer.Value = 0;
               }
               // _springVelocity = 0;
               // agent.stoppingDistance = 0;
               // agent.speed = 0;
               // agent.acceleration = 200;
            }
   
            if (stunNormalizedTimer > 0) {
               break;
            }

            var lastPlayer = targetPlayer;
            if (IsOwner) {
               // nobody is in range so go back to searching
               if (!TargetClosestPlayer()) {
                  SwitchToBehaviourState(0);
                  EndChasingPlayer_ServerRpc();
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] not in range; SwitchToBehaviourState(0)");
                  break;
               }
               
               if (_wasStopped && _sharedAiSettings.rotateToLookAtPlayer) {
                  if (_lookTimer.Value >= _sharedAiSettings.delayBeforeLookingAtPlayer) {
                     // rotate visuals to look at player
                     var lookAt = targetPlayer.transform.position;
                     var position = transform.position;
                     var dir = lookAt - position;
                     dir.y = 0;
                     dir.Normalize();
   
                     var quaternion = Quaternion.LookRotation(dir);
                     // _visuals.rotation = Quaternion.Lerp(_visuals.rotation, quaternion, Time.deltaTime * 5f);
                     transform.rotation = Quaternion.Lerp(transform.rotation, quaternion, Time.deltaTime / _sharedAiSettings.lookAtPlayerDuration);
                  }
               } else if (!_wasStopped && _sharedAiSettings.rotateToLookAtPlayer) {
                  // _visuals.localRotation = Quaternion.Lerp(_visuals.localRotation, Quaternion.identity, Time.deltaTime * 5f);
               }
            }
   
            var aiType = NetworkHandler.AiType;
            switch (aiType) {
               case RollingGiantAiType.Coilhead:
                  if (AmIBeingLookedAt(out _)) {
                     _wasStopped = true;
                     // Plugin.Log.LogInfo($"[Update::{aiType}] _wasStopped");
                     return;
                  }
                  break;
               case RollingGiantAiType.InverseCoilhead:
                  if (!AmIBeingLookedAt(out _) && _isAgro) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.RandomlyMoveWhileLooking:
                  if (AmIBeingLookedAt(out _) && _moveTimer.Value <= 0) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.LookingTooLongKeepsAgro:
                  if (AmIBeingLookedAt(out _) && _agroTimer.Value < 1f) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.FollowOnceAgro:
                  // ?
                  break;
               case RollingGiantAiType.OnceSeenAgroAfterTimer:
                  if (_isAgro && _agroTimer.Value <= 0) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               default:
                  Plugin.Log.LogWarning($"Unknown ai type: {aiType}");
                  break;
            }
   
            _wasStopped = false;
            _wasFeared = false;
            // Plugin.Log.LogInfo($"[Update::{aiType}] _wasStopped: {_wasStopped}, _wasFeared: {_wasFeared}, _lastSpeed: {_lastSpeed}");

            // if (IsOwner) {
            //    if (lastPlayer != targetPlayer) {
            //       SetMovingTowardsTargetPlayer(targetPlayer);
            //       // ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            //       Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] SetMovingTowardsTargetPlayer, player {targetPlayer?.playerUsername}");
            //    }
            // }

            if (IsOwner) {
               if (lastPlayer != targetPlayer) {
                  SetMovingTowardsTargetPlayer(targetPlayer);
                  // ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] SetMovingTowardsTargetPlayer, player {targetPlayer?.playerUsername}");
               }
            }

            // if (!IsOwner || lastPlayer == targetPlayer || targetPlayer != localPlayer) {
            //    return;
            // }
         }
            break;
      }
   }

   // public bool GetClosestPlayer() {
   //    this.targetPlayer = null;
   //    
   //    // did the player see the giant
   //    if (AmIBeingLookedAt(out var closestPlayer)) {
   //       targetPlayer = closestPlayer;
   //       return closestPlayer;
   //    }
   //    
   //    var closestDistance = 2000f;
   //    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; ++i) {
   //       var player = StartOfRound.Instance.allPlayerScripts[i];
   //       if (player.isPlayerDead) continue;
   //       if (!player.IsSpawned) continue;
   //       
   //       var position = player.transform.position;
   //       
   //       // did the giant see this player
   //       var pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       // if (!pathIsIntersectedByLineOfSight) {
   //       //    var closestNode = ChooseClosestNodeToPosition(player.transform.position, avoidLineOfSight: false);
   //       //    if (closestNode) {
   //       //       position = closestNode.position;
   //       //       pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       //    }
   //       // }
   //       
   //       if (!pathIsIntersectedByLineOfSight) {
   //          continue;
   //       }
   //       
   //       if (isOutside == player.isInsideFactory) {
   //          continue;
   //       }
   //       
   //       var distance = Vector3.Distance(transform.position, position);
   //       if (distance < closestDistance) {
   //          closestDistance = distance;
   //          targetPlayer = player;
   //       }
   //    }
   //
   //    return targetPlayer;
   // }
   
   // public new bool TargetClosestPlayer(float bufferDistance = 1.5f, bool requireLineOfSight = false, float viewWidth = 70f) {
   //    mostOptimalDistance = 2000;
   //    var targetPlayer = this.targetPlayer;
   //    this.targetPlayer = null;
   //    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; ++i) {
   //       var player = StartOfRound.Instance.allPlayerScripts[i];
   //       if (player.isPlayerDead) continue;
   //       if (!player.IsSpawned) continue;
   //       // if (isOutside && player.isInsideFactory) continue;
   //
   //       var position = player.transform.position;
   //       var pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       // if (isOutside) {
   //       //    if (pathIsIntersectedByLineOfSight) {
   //       //       // get closest navmesh point
   //       //       var closestNode = ChooseClosestNodeToPosition(player.transform.position, avoidLineOfSight: false);
   //       //       if (closestNode) {
   //       //          position = closestNode.position;
   //       //          pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       //       }
   //       //       
   //       //       // ? this gets modified above so this is resetting it otherwise it is too low
   //       //       mostOptimalDistance += 10;
   //       //    }
   //       // }
   //       
   //       var hasLineOfSightToPosition = HasLineOfSightToPosition(player.gameplayCamera.transform.position, viewWidth, 40);
   //       if (!pathIsIntersectedByLineOfSight && (!requireLineOfSight || hasLineOfSightToPosition)) {
   //          tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
   //          if (tempDist < (double)mostOptimalDistance) {
   //             // Plugin.Log.LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) tempDist: {tempDist}, mostOptimalDistance: {mostOptimalDistance}");
   //             mostOptimalDistance = tempDist;
   //             this.targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
   //          } else {
   //             // Plugin.Log.LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) tempDist: {tempDist}, mostOptimalDistance: {mostOptimalDistance}");
   //          }
   //       } else {
   //          // Plugin.Log.LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) pathIsIntersectedByLineOfSight: {pathIsIntersectedByLineOfSight}, hasLineOfSightToPosition: {hasLineOfSightToPosition}");
   //       }
   //    }
   //
   //    if (this.targetPlayer && this.targetPlayer != targetPlayer && bufferDistance > 0.0 && targetPlayer && Mathf.Abs(mostOptimalDistance - Vector3.Distance(transform.position, targetPlayer.transform.position)) < (double)bufferDistance) {
   //       this.targetPlayer = targetPlayer;
   //    } else if (this.targetPlayer != targetPlayer) {
   //       // Plugin.Log.LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] this.targetPlayer: {this.targetPlayer?.playerUsername}, targetPlayer: {targetPlayer?.playerUsername}, mostOptimalDistance: {mostOptimalDistance}, bufferDistance: {bufferDistance}, distance: {(targetPlayer ? Vector3.Distance(transform.position, targetPlayer.transform.position) : -1)}");
   //    }
   //
   //    return this.targetPlayer;
   // }

   private static float SmoothLerp(float a, float b, float t) {
      return a + (t * t) * (b - a);
   }

   public override void OnCollideWithPlayer(Collider other) {
      if (daytimeEnemyLeaving) {
         return;
      }
      
      base.OnCollideWithPlayer(other);
      if (_timeSinceHittingPlayer < 0.6f) {
         return;
      }

      var player = MeetsStandardPlayerCollisionConditions(other);
      if (!player) return;

      if (_tooBig && player.isInHangarShipRoom) {
         return;
      }

      _timeSinceHittingPlayer = 0.2f;
      var index = StartOfRound.Instance.playerRagdolls.IndexOf(Plugin.PlayerRagdoll);
      player.DamagePlayer(90, causeOfDeath: CauseOfDeath.Strangulation, deathAnimation: index);
      agent.speed = 0;

      GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
   }

   private void CalculateAgentSpeed() {
      if (stunNormalizedTimer >= 0) {
         agent.speed = 0;
         agent.acceleration = 200;
         return;
      }

      // searching
      if (currentBehaviourStateIndex == 0) {
         MoveAccelerate();
      }
      // chasing
      else if (currentBehaviourStateIndex == 1) {
         // if not on the ground, accelerate to reach it
         if (IsOwner && !IsAgentOnNavMesh(agent.gameObject)) {
            MoveAccelerate();
            // Plugin.Log.LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] not on navmesh");
            return;
         }

         var isLookedAt = AmIBeingLookedAt(out _);
         if (IsOwner) {
            if (isLookedAt) {
               _lookTimer.Value += Time.deltaTime;
            } else {
               _lookTimer.Value = 0;
            }
         }

         var aiType = NetworkHandler.AiType;
         switch (aiType) {
            case RollingGiantAiType.Coilhead:
               if (isLookedAt) {
                  MoveDecelerate();
                  // Plugin.Log.LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] {player?.playerUsername} is lookin at, {_lookTimer}sec");
                  return;
               }

               MoveAccelerate();
               // Plugin.Log.LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] not looking at, {_lookTimer}sec");
               break;
            case RollingGiantAiType.InverseCoilhead:
               if (!isLookedAt && _isAgro) {
                  MoveDecelerate();
                  return;
               }

               MoveAccelerate();
               _isAgro = true;
               break;
            case RollingGiantAiType.RandomlyMoveWhileLooking:
               if (isLookedAt) {
                  if (_waitTimer.Value <= 0 && _moveTimer.Value <= 0) {
                     GenerateWaitTime();
                  }

                  if (_waitTimer.Value > 0 && _moveTimer.Value <= 0) {
                     MoveDecelerate();

                     if (IsOwner) {
                        _waitTimer.Value -= Time.deltaTime;
                        if (_waitTimer.Value <= 0) {
                           GenerateMoveTime();
                        }
                     }
                     return;
                  }
               }

               MoveAccelerate();

               if (_moveTimer.Value > 0) {
                  if (IsOwner) {
                     _moveTimer.Value -= Time.deltaTime;
                     if (_moveTimer.Value <= 0) {
                        GenerateWaitTime();
                     }
                  }
               }
               break;
            case RollingGiantAiType.LookingTooLongKeepsAgro:
               if (!_isAgro) {
                  if (isLookedAt) {
                     if (IsOwner) {
                        _agroTimer.Value += Time.deltaTime / _sharedAiSettings.lookTimeBeforeAgro;
                     }
                     // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] _lookTimer: {_agroTimer.Value}");
                     if (_agroTimer.Value >= 1f) {
                        _isAgro = true;
                        // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     }

                     MoveDecelerate();
                  } else {
                     if (IsOwner) {
                        _agroTimer.Value = Mathf.Lerp(_agroTimer.Value, 0, Time.deltaTime / (_sharedAiSettings.lookTimeBeforeAgro * 1.5f));
                        // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] _lookTimer: {_agroTimer.Value}");
                     }

                     MoveAccelerate();
                  }
                  return;
               }

               MoveAccelerate();
               break;
            case RollingGiantAiType.FollowOnceAgro:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _isAgro = true;
                     MoveDecelerate();
                     // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     return;
                  }
               }

               MoveAccelerate();
               break;
            case RollingGiantAiType.OnceSeenAgroAfterTimer:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _isAgro = true;
                     // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     if (IsOwner) {
                        _agroTimer.Value = Mathf.Lerp(_sharedAiSettings.waitTimeMin, _sharedAiSettings.waitTimeMax, NextDouble());
                     }
                     MoveDecelerate();
                  }
                  return;
               }

               if (_agroTimer.Value >= 0) {
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] _agroTimer: {_agroTimer.Value}");
                  if (IsOwner) {
                     _agroTimer.Value -= Time.deltaTime;
                  }

                  if (_agroTimer.Value < 0) {
                     // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] chasing time");
                  }
                  MoveDecelerate();
                  return;
               }

               MoveAccelerate();
               break;
            default:
               Plugin.Log.LogWarning($"Unknown ai type: {aiType}");
               break;
         }
      }
   }

   private void MoveAccelerate() {
      agent.speed = _sharedAiSettings.moveAcceleration == 0
         ? _sharedAiSettings.moveSpeed
         : Mathf.Lerp(agent.speed, _sharedAiSettings.moveSpeed, Time.deltaTime / _sharedAiSettings.moveAcceleration);
      agent.acceleration = Mathf.Lerp(agent.acceleration, 200, Time.deltaTime);
   }

   private void MoveDecelerate() {
      agent.speed = _sharedAiSettings.moveDeceleration == 0 ? 0 : Mathf.Lerp(agent.speed, 0, Time.deltaTime / _sharedAiSettings.moveDeceleration);
      agent.acceleration = 200;
   }


   private bool AmIBeingLookedAt(out PlayerControllerB closestPlayer) {
      var players = StartOfRound.Instance.allPlayerScripts;
      var closestDistance = float.MaxValue;
      closestPlayer = null;

      foreach (var player in players) {
         if (!isOutside && !PlayerIsTargetable(player)) continue;
         if (player.isPlayerDead) continue;
         if (!player.IsSpawned) continue;
         // if (!PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside)) {
         //    // Plugin.Log.LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] !PlayerIsTargetable({player?.playerUsername})");
         //    continue;
         // }
         // transform.position + Vector3.up * 1.6f
         if (player.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f)) {
            var distance = Vector3.Distance(transform.position, player.transform.position);
            // Plugin.Log.LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] HasLineOfSightToPosition({player?.playerUsername}), distance: {distance}");
            if (distance < closestDistance) {
               closestDistance = distance;
               closestPlayer = player;
            }
         } else {
            // Plugin.Log.LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] !HasLineOfSightToPosition({player?.playerUsername})");
         }
      }

      // Plugin.Log.LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] closestPlayer: {closestPlayer?.playerUsername}");
      return closestPlayer;
   }
   
   // public bool HasLineOfSightToPosition(
   //    PlayerControllerB playerControllerB,
   //    Vector3 pos,
   //    float width = 45f,
   //    int range = 60,
   //    float proximityAwareness = -1f)
   // {
   //    float num = Vector3.Distance(playerControllerB.transform.position, pos);
   //    var b0 = num < (double)range;
   //    var b1 = (Vector3.Angle(playerControllerB.playerEye.transform.forward, pos - playerControllerB.gameplayCamera.transform.position) < (double)width ||
   //              num < (double)proximityAwareness);
   //    var b2 = !Physics.Linecast(playerControllerB.playerEye.transform.position,
   //       pos,
   //       out var hit,
   //       StartOfRound.Instance.collidersRoomDefaultAndFoliage,
   //       QueryTriggerInteraction.Ignore);
   //    Plugin.Log.LogInfo($"[HasLineOfSightToPosition::{NetworkHandler.AiType}] b0: {b0}, b1: {b1}, b2: {b2} ({hit.collider?.name})");
   //    return b0 && b1 && b2;
   // }

   private bool IsAgentOnNavMesh(GameObject agentObject) {
      var agentPosition = agentObject.transform.position;

      // Check for nearest point on navmesh to agent, within onMeshThreshold
      if (NavMesh.SamplePosition(agentPosition, out var hit, 3, NavMesh.AllAreas)) {
         // Check if the positions are vertically aligned
         if (Mathf.Approximately(agentPosition.x, hit.position.x)
             && Mathf.Approximately(agentPosition.z, hit.position.z)) {
            // Lastly, check if object is below navmesh
            return agentPosition.y >= hit.position.y;
         }
      }

      return false;
   }

   [ServerRpc(RequireOwnership = false)]
   private void BeginChasingPlayer_ServerRpc(int playerId) {
      BeginChasingPlayer_ClientRpc(playerId);
      // Plugin.Log.LogInfo($"[BeginChasingPlayer_ServerRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(1)");
   }

   [ClientRpc]
   private void BeginChasingPlayer_ClientRpc(int playerId) {
      SwitchToBehaviourStateOnLocalClient(1);
      var player = StartOfRound.Instance.allPlayerScripts[playerId];
      SetMovingTowardsTargetPlayer(player);
      // Plugin.Log.LogInfo($"[BeginChasingPlayer_ClientRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(1)");
   }
   
   [ServerRpc(RequireOwnership = false)]
   private void EndChasingPlayer_ServerRpc() {
      EndChasingPlayer_ClientRpc();
      // Plugin.Log.LogInfo($"[EndChasingPlayer_ServerRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(0)");
   }
   
   [ClientRpc]
   private void EndChasingPlayer_ClientRpc() {
      moveTowardsDestination = false;
      movingTowardsTargetPlayer = false;
      SwitchToBehaviourStateOnLocalClient(0);
      // Plugin.Log.LogInfo($"[EndChasingPlayer_ClientRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(0)");
   }
   
   private void GenerateWaitTime() {
      if (!IsOwner) return;
      var waitTime = Mathf.Lerp(_sharedAiSettings.waitTimeMin, _sharedAiSettings.waitTimeMax, NextDouble());
      _waitTimer.Value = waitTime;
   }
   
   private void GenerateAgroTime() {
      if (!IsOwner) return;
      _agroTimer.Value = _sharedAiSettings.lookTimeBeforeAgro;
   }
   
   private void GenerateMoveTime() {
      if (!IsOwner) return;
      var moveTime = Mathf.Lerp(_sharedAiSettings.randomMoveTimeMin, _sharedAiSettings.randomMoveTimeMax, NextDouble());
      _moveTimer.Value = moveTime;
   }

   [ServerRpc(RequireOwnership = false)]
   private void AssignInitData_ServerRpc(float scale) {
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      AssignInitData_ClientRpc(scale);
      // Plugin.Log.LogInfo($"[AssignInitData_ServerRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }
   
   private void AssignInitData_LocalClient() {
      var config = CustomConfig.Instance;
      var sizeRange = isOutside ? new Vector2(config.GiantScaleOutsideMin, config.GiantScaleOutsideMax) : new Vector2(config.GiantScaleInsideMin, config.GiantScaleInsideMax);
      var scale = Mathf.Lerp(sizeRange.x, sizeRange.y, NextDouble());
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      AssignInitData_ServerRpc(scale);
      // Plugin.Log.LogInfo($"[AssignInitData_LocalClient::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }

   [ClientRpc]
   private void AssignInitData_ClientRpc(float scale) {
      // agent.height = 5f;
      updatePositionThreshold = float.MaxValue;
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      
      // Plugin.Log.LogInfo($"[AssignInitData_ClientRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }

   private void AssignAgentData(float scale) {
      Init(scale);
      if (scale >= 1.2f) {
         var areas = agent.areaMask;
         // var exclude = 1 << NavMesh.GetAreaFromName("SmallSpace") | 1 << NavMesh.GetAreaFromName("MediumSpace") | 1 << NavMesh.GetAreaFromName("Climb");
         areas &= ~(1 << NavMesh.GetAreaFromName("SmallSpace"));
         areas &= ~(1 << NavMesh.GetAreaFromName("MediumSpace"));
         areas &= ~(1 << NavMesh.GetAreaFromName("Climb"));
         areas &= ~(1 << NavMesh.GetAreaFromName("PlayerShip"));
         agent.areaMask = areas;
         _tooBig = true;
      }
   }
}