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

   private static SharedAiSettings _sharedAiSettings => CustomConfig.SharedAiSettings;
   
   private AudioSource _rollingSFX;
   private NetworkVariable<float> _velocity = new();

   private float _timeSinceHittingPlayer;
   private bool _wantsToChaseThisClient;
   private bool _hasEnteredChaseState;
   private bool _wasStopped;
   private bool _wasFeared;
   private bool _isAgro;
   private float _lastSpeed;
   private float _audioFade;

   private float _waitTimer;
   private float _moveTimer;
   private float _lookTimer;
   private float _agroTimer;
   private float _springVelocity;

   private static float NextDouble() {
      if (!RoundManager.Instance || RoundManager.Instance.LevelRandom == null) {
         // Plugin.Log.LogWarning("Missing RoundManager or LevelRandom, in dev level?");
         return Random.value;
      }
      
      return (float)RoundManager.Instance.LevelRandom.NextDouble();
   }

   public override void Start() {
      base.Start();
      
      Init();
      
      if (IsHost || IsOwner) {
         AssignInitData_LocalClient();
      }
      
      Plugin.Log.LogInfo($"Rolling giant spawned with ai type: {NetworkHandler.AiType}, owner? {IsOwner}");
   }

   private void Init() {
      agent = gameObject.GetComponentInChildren<NavMeshAgent>();
      _rollingSFX = transform.Find("RollingSFX").GetComponent<AudioSource>();
      
      var mixer = SoundManager.Instance.diageticMixer.outputAudioMixerGroup;
      _rollingSFX.outputAudioMixerGroup = mixer;
      creatureVoice.outputAudioMixerGroup = mixer;
      creatureSFX.outputAudioMixerGroup = mixer;
      
      _rollingSFX.loop = true;
      _rollingSFX.clip = Plugin.WalkSound;
      var time = NextDouble() * Plugin.WalkSound.length;
      var pitch = Mathf.Lerp(0.96f, 1.05f, NextDouble());
      _rollingSFX.time = time;
      _rollingSFX.pitch = pitch;
      _rollingSFX.volume = 0;
      _rollingSFX.Play();
      
      // Plugin.Log.LogInfo($"[Init::{_sharedAiSettings.aiType}] _rollingSFX.time: {_rollingSFX.time}, _rollingSFX.pitch: {_rollingSFX.pitch}, _rollingSFX.volume: {_rollingSFX.volume}");
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
   
      switch (currentBehaviourStateIndex) {
         // searching
         case 0:
            if (!IsServer) {
               ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               break;
            }

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
               var player = StartOfRound.Instance.allPlayerScripts[i];
               if (PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside) &&
                   !Physics.Linecast(transform.position + Vector3.up * 0.5f,
                      player.gameplayCamera.transform.position,
                      StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(transform.position, player.transform.position) < 30.0) {
                  SwitchToBehaviourState(1);
                  // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] SwitchToBehaviourState(1)");
                  return;
               }
            }
   
            if (_searchForPlayers.inProgress) {
               break;
            }
   
            // start a search to find a player...
            StartSearch(transform.position, _searchForPlayers);
            // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StartSearch({transform.position}, _searchForPlayers)");
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
               ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] lost player; StartSearch({transform.position}, _searchForPlayers)");
               break;
            }
   
            if (!_searchForPlayers.inProgress) {
               break;
            }
   
            // stop the current search as we found a player!
            StopSearch(_searchForPlayers);
            movingTowardsTargetPlayer = true;
            // Plugin.Log.LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StopSearch(_searchForPlayers)");
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
   
      _mainCollider.isTrigger = !_wasStopped;
   
      // var speed = !(IsOwner || IsHost) ? _velocity.Value : agent.velocity.magnitude;
      var speed = _velocity.Value;
      // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] speed: {speed}/{_sharedAiSettings.moveSpeed}, moveTowardsDestination: {moveTowardsDestination}: movingTowardsTargetPlayer: {movingTowardsTargetPlayer}, onNavmesh: {agent.isOnNavMesh}, isActiveAndEnabled: {agent.isActiveAndEnabled}");
      // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] speed: {speed}/{_sharedAiSettings.moveSpeed}, _rollingSFX.volume: {_rollingSFX.volume}");
      _rollingSFX.volume = Mathf.Lerp(0, Mathf.Clamp01(ROAMING_AUDIO_PERCENT * speed + 0.05f), speed / _sharedAiSettings.moveSpeed);
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
               _isAgro = false;
               _agroTimer = 0;
               _waitTimer = 0;
               _moveTimer = 0;
               _lookTimer = 0;
               _springVelocity = 0;
               agent.stoppingDistance = 0;
               agent.speed = _sharedAiSettings.moveSpeed;
               agent.acceleration = 200;
            }
            
            if (IsOwner) {
               //if (AmIInRangeOfAPlayer(out var closestPlayer) && closestPlayer == localPlayer) {
               if (TargetClosestPlayer(requireLineOfSight: true) && targetPlayer == localPlayer) {
                  if (_wantsToChaseThisClient) {
                     break;
                  }

                  _wantsToChaseThisClient = true;
                  BeginChasingPlayer_ServerRpc((int)targetPlayer.playerClientId);
                  ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
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
               _isAgro = false;
               _agroTimer = 0;
               _waitTimer = 0;
               _moveTimer = 0;
               _lookTimer = 0;
               _springVelocity = 0;
               agent.stoppingDistance = 0;
               agent.speed = 0;
               agent.acceleration = 200;
            }
   
            if (stunNormalizedTimer > 0) {
               break;
            }

            var lastPlayer = targetPlayer;
            if (IsOwner) {
               // nobody is in range so go back to searching
               if (!TargetClosestPlayer()) {
                  // SwitchToBehaviourState(0);
                  EndChasingPlayer_ServerRpc();
                  // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] not in range; SwitchToBehaviourState(0)");
                  break;
               }
               
               if (_wasStopped && _sharedAiSettings.rotateToLookAtPlayer) {
                  if (_lookTimer >= _sharedAiSettings.delayBeforeLookingAtPlayer) {
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
                  if (AmIBeingLookedAt(out _) && _moveTimer <= 0) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.LookingTooLongKeepsAgro:
                  if (AmIBeingLookedAt(out _) && _agroTimer < _sharedAiSettings.lookTimeBeforeAgro) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.FollowOnceAgro:
                  // ?
                  break;
               case RollingGiantAiType.OnceSeenAgroAfterTimer:
                  if (_isAgro && _waitTimer >= 0) {
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
   
            if (!IsOwner || lastPlayer == targetPlayer || targetPlayer != localPlayer) {
               return;
            }
   
            SetMovingTowardsTargetPlayer(targetPlayer);
            ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            // Plugin.Log.LogInfo($"[Update::{NetworkHandler.AiType}] SetMovingTowardsTargetPlayer({targetPlayer?.playerUsername})");
         }
            break;
      }
   }

   // ? needed to insert the overrideInsideFactoryCheck override
   public new bool TargetClosestPlayer(float bufferDistance = 1.5f, bool requireLineOfSight = false, float viewWidth = 70f) {
      mostOptimalDistance = 2000f;
      var targetPlayer = this.targetPlayer;
      this.targetPlayer = null;
      for (int index = 0; index < StartOfRound.Instance.connectedPlayersAmount + 1; ++index) {
         var player = StartOfRound.Instance.allPlayerScripts[index];
         if (PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside) &&
             !PathIsIntersectedByLineOfSight(player.transform.position, avoidLineOfSight: false) && (!requireLineOfSight || HasLineOfSightToPosition(player.gameplayCamera.transform.position, viewWidth, 40))) {
            tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[index].transform.position);
            if (tempDist < (double)mostOptimalDistance) {
               mostOptimalDistance = tempDist;
               this.targetPlayer = StartOfRound.Instance.allPlayerScripts[index];
            }
         }
      }

      if (this.targetPlayer && bufferDistance > 0.0 && targetPlayer && Mathf.Abs(mostOptimalDistance - Vector3.Distance(transform.position, targetPlayer.transform.position)) < (double)bufferDistance) {
         this.targetPlayer = targetPlayer;
      }

      return this.targetPlayer;
   }

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
         agent.speed = _sharedAiSettings.moveAcceleration == 0
            ? _sharedAiSettings.moveSpeed
            : Mathf.Lerp(agent.speed, _sharedAiSettings.moveSpeed, Time.deltaTime / _sharedAiSettings.moveAcceleration);
         agent.acceleration = 200;
      }
      // chasing
      else if (currentBehaviourStateIndex == 1) {
         // if not on the ground, accelerate to reach it
         if (!IsAgentOnNavMesh(agent.gameObject)) {
            MoveAccelerate();
            // Plugin.Log.LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] not on navmesh");
            return;
         }

         var isLookedAt = AmIBeingLookedAt(out _);
         if (isLookedAt) {
            _lookTimer += Time.deltaTime;
         } else {
            _lookTimer = 0;
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
                  if (_waitTimer <= 0 && _moveTimer <= 0) {
                     GenerateWaitTime_LocalClient();
                  }

                  if (_waitTimer > 0 && _moveTimer <= 0) {
                     MoveDecelerate();

                     _waitTimer -= Time.deltaTime;
                     if (_waitTimer <= 0) {
                        GenerateMoveTime_LocalClient();
                     }
                     return;
                  }
               }

               MoveAccelerate();

               if (_moveTimer > 0) {
                  _moveTimer -= Time.deltaTime;
                  if (_moveTimer <= 0) {
                     GenerateWaitTime_LocalClient();
                  }
               }
               break;
            case RollingGiantAiType.LookingTooLongKeepsAgro:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _agroTimer = Mathf.SmoothDamp(_agroTimer, _sharedAiSettings.lookTimeBeforeAgro + 0.5f, ref _springVelocity, 0.5f);
                     // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] _lookTimer: {_agroTimer}");
                     if (_agroTimer >= _sharedAiSettings.lookTimeBeforeAgro) {
                        _isAgro = true;
                     }

                     MoveDecelerate();
                  } else {
                     _agroTimer = Mathf.Lerp(_agroTimer, 0, Time.deltaTime / (_sharedAiSettings.lookTimeBeforeAgro / 2f));
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
                     // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] got agro");
                     return;
                  }
               }

               MoveAccelerate();
               break;
            case RollingGiantAiType.OnceSeenAgroAfterTimer:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _isAgro = true;
                     // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] got agro");
                     GenerateWaitTime_LocalClient();
                     MoveDecelerate();
                  }
                  return;
               }

               if (_waitTimer >= 0) {
                  // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] _waitTimer: {_waitTimer}");
                  _waitTimer -= Time.deltaTime;

                  if (_waitTimer < 0) {
                     // Plugin.Log.LogInfo($"[Update::{_sharedAiSettings.aiType}] chasing time");
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
         if (!PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside)) {
            // Plugin.Log.LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] !PlayerIsTargetable({player?.playerUsername})");
            continue;
         }
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

   [ServerRpc(RequireOwnership = false)]
   private void GenerateWaitTime_ServerRpc(float waitTime) {
      _waitTimer = waitTime;
      // Plugin.Log.LogInfo($"[GenerateWaitTime_ServerRpc::{_sharedAiSettings.aiType}] _waitTimer: {_waitTimer}");
   }
   
   private void GenerateWaitTime_LocalClient() {
      var waitTime = Mathf.Lerp(_sharedAiSettings.waitTimeMin, _sharedAiSettings.waitTimeMax, NextDouble());
      _waitTimer = waitTime;
      GenerateWaitTime_ServerRpc(waitTime);
      // Plugin.Log.LogInfo($"[GenerateWaitTime_LocalClient::{_sharedAiSettings.aiType}] _waitTimer: {_waitTimer}");
   }

   [ServerRpc(RequireOwnership = false)]
   private void GenerateMoveTime_ServerRpc(float moveTime) {
      _moveTimer = moveTime;
      GenerateMoveTime_ClientRpc(moveTime);
      // Plugin.Log.LogInfo($"[GenerateMoveTime_ServerRpc::{_sharedAiSettings.aiType}] _moveTimer: {_moveTimer}");
   }
   
   private void GenerateMoveTime_LocalClient() {
      var moveTime = Mathf.Lerp(_sharedAiSettings.randomMoveTimeMin, _sharedAiSettings.randomMoveTimeMax, NextDouble());
      _moveTimer = moveTime;
      GenerateMoveTime_ServerRpc(moveTime);
      // Plugin.Log.LogInfo($"[GenerateMoveTime_LocalClient::{_sharedAiSettings.aiType}] _moveTimer: {_moveTimer}");
   }
   
   [ClientRpc]
   private void GenerateMoveTime_ClientRpc(float moveTime) {
      _moveTimer = moveTime;
      // Plugin.Log.LogInfo($"[GenerateMoveTime_ClientRpc::{_sharedAiSettings.aiType}] _moveTimer: {_moveTimer}");
   }

   [ServerRpc(RequireOwnership = false)]
   private void AssignInitData_ServerRpc(float scale) {
      agent.transform.localScale = Vector3.one * scale;
      AssignInitData_ClientRpc(scale);
      // Plugin.Log.LogInfo($"[AssignInitData_ServerRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }
   
   private void AssignInitData_LocalClient() {
      var config = CustomConfig.Instance;
      var modelScale = Mathf.Lerp(config.GiantScaleMin, config.GiantScaleMax, NextDouble());
      agent.transform.localScale = Vector3.one * modelScale;
      AssignInitData_ServerRpc(modelScale);
      // Plugin.Log.LogInfo($"[AssignInitData_LocalClient::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }

   [ClientRpc]
   private void AssignInitData_ClientRpc(float scale) {
      Init();
      agent.transform.localScale = Vector3.one * scale;
      // Plugin.Log.LogInfo($"[AssignInitData_ClientRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }
}