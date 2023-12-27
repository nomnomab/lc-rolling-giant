using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;

namespace RollingGiant.Patches;

[HarmonyPatch]
public class CoilheadPatch : MonoBehaviour {
   private static EnemyType _rollingGiantEnemyType;
   private static TerminalNode _giantInfoNode;
   
   private readonly static Dictionary<SpringManAI, RollingGiantData> _rollingGiantDatas = new();
   private static float _roamingAudioPercent = 0.5f;

   private struct RollingGiantData {
      public float waitTime;
      public float moveTimer;
      public float lookTimer;
      public float stoppedTimer;
      public bool isMoving;
      public bool isAgro;
      public bool isLookWaiting;
   }

   private static void InitData(Terminal __instance) {
      if (_giantInfoNode == null) {
         const string giantBestiary = """
                                      ROLLING-GIANTS

                                      Rolling Giant's danger level: 80%

                                      We're back.

                                      Created by La Reunion TX artists and volunteers who care about West Dallas, the oversized puppets of Bridge-o-Rama's Parade of Giants nearly stole the show during the opening weekend of the Margaret Hunt Hill Bridge.

                                      See them again. Learn their stories.

                                      Bridge-o-Rama's Parade of Giants at Dallas City Hall.
                                      Opening reception 6 p.m., Tuesday, June 19, 1500 Marilla St.


                                      """;

         _giantInfoNode = TerminalApi.TerminalApi.CreateTerminalNode(giantBestiary, true);
         _giantInfoNode.displayVideo = Plugin.MovieTexture;
         _giantInfoNode.loadImageSlowly = true;
         _giantInfoNode.creatureName = "Rolling Giant";
         _giantInfoNode.creatureFileID = 100;
      }

      while (__instance.enemyFiles.Count <= 100) {
         __instance.enemyFiles.Add(null);
      }

      __instance.enemyFiles[100] = _giantInfoNode;
   }

   [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
   [HarmonyPrefix]
   public static void InitDataIfNeeded(Terminal __instance) {
      InitData(__instance);
   }

   [HarmonyPatch(typeof(Terminal), "Start")]
   [HarmonyPostfix]
   public static void InitTerminal(Terminal __instance) {
      InitData(__instance);

      var terminalNodes = __instance.terminalNodes;
      if (terminalNodes.terminalNodes.Any(x => x.creatureFileID == 100)) {
         return;
      }

      terminalNodes.terminalNodes.Add(_giantInfoNode);

      var infoKeywords = terminalNodes.allKeywords.First(x => x.word == "info" && x.isVerb);
      var baseKeyword = TerminalApi.TerminalApi.CreateTerminalKeyword("rolling giant", false, _giantInfoNode);
      var rollingKeyword = TerminalApi.TerminalApi.CreateTerminalKeyword("rolling", false, _giantInfoNode);
      var giantKeyword = TerminalApi.TerminalApi.CreateTerminalKeyword("giant", false, _giantInfoNode);

      var newNouns = infoKeywords.compatibleNouns.ToList();
      newNouns.Add(new CompatibleNoun {
         noun = baseKeyword,
         result = _giantInfoNode
      });
      newNouns.Add(new CompatibleNoun {
         noun = rollingKeyword,
         result = _giantInfoNode
      });
      newNouns.Add(new CompatibleNoun {
         noun = giantKeyword,
         result = _giantInfoNode
      });
      infoKeywords.compatibleNouns = newNouns.ToArray();

      var newAllKeywords = terminalNodes.allKeywords.ToList();
      newAllKeywords.Add(baseKeyword);
      newAllKeywords.Add(rollingKeyword);
      newAllKeywords.Add(giantKeyword);
      terminalNodes.allKeywords = newAllKeywords.ToArray();
   }

   [HarmonyPatch(typeof(EnemyAI), "Start")]
   [HarmonyPostfix]
   public static void SummonRollingGiant(EnemyAI __instance) {
      if (__instance is not SpringManAI springManAI) {
         return;
      }
      
      // var chance = Random.value;
      // Plugin.Log.LogInfo($"[test] {__instance.NetworkObjectId}, {__instance.NetworkObject.OwnerClientId}, {__instance.IsServer}, {__instance.IsOwnedByServer}, {__instance.IsClient}");
      var mapSeed = StartOfRound.Instance.randomMapSeed;
      var id = (int)__instance.NetworkObjectId + mapSeed;
      var rng = new System.Random(id);
      // var chance = (double)__instance.NetworkObjectId / 32.3f % 1f;
      var chance = rng.NextDouble();
      var totalChance = Plugin.GeneralSettings.ChanceForGiant.Value;
      Plugin.Log.LogInfo($"Rolling Giant [{id}] {springManAI} rolled {chance} ({totalChance})");
      if (chance > totalChance) {
         Plugin.Log.LogInfo($"Rolling Giant [{id}] {springManAI} failed to spawn ({chance} > {totalChance})");
         return;
      }

      var model = springManAI.transform.Find("SpringManModel");
      Destroy(model.Find("Body").gameObject.GetComponent<SkinnedMeshRenderer>());
      Destroy(model.Find("Head").gameObject.GetComponent<MeshRenderer>());

      var footsteps = model.Find("FoostepSFX").GetComponent<AudioSource>();
      // footsteps.enabled = false;
      footsteps.volume = 0;
      footsteps.mute = true;
      
      SpawnRollingGiant(springManAI, Plugin.GeneralSettings.GetRandomScale(rng));
      ChangeSpringNoises(springManAI);

      Plugin.Log.LogInfo($"Summoned Rolling Giant [{id}]! ({chance} <= {totalChance})");
   }

   private static void SpawnRollingGiant(SpringManAI parent, float scale) {
      if (!parent) return;

      var instance = Instantiate(Plugin.RollingGiantModel).transform;
      instance.SetParent(parent.transform.Find("SpringManModel"));
      instance.localPosition = Vector3.zero;
      instance.localRotation = Quaternion.identity;
      instance.localScale = Vector3.one * scale;
      // parent.creatureAnimator.enabled = false;

      if (!_rollingGiantEnemyType) {
         var enemyType = parent.enemyType;
         enemyType = Instantiate(enemyType);
         enemyType.enemyName = "Rolling Giant";
         _rollingGiantEnemyType = enemyType;
      }

      parent.enemyType = _rollingGiantEnemyType;

      var scanNode = parent.GetComponentInChildren<ScanNodeProperties>();
      scanNode.creatureScanID = 100;
      scanNode.headerText = "Rolling Giant";

      var rollingAudioSourceObj = new GameObject("RollingSFX");
      var rollingAudioSource = rollingAudioSourceObj.AddComponent<AudioSource>();
      rollingAudioSourceObj.transform.SetParent(parent.transform, false);
      
      // copy all of the data from the original audio source
      var original = parent.creatureSFX;
      rollingAudioSource.pitch = original.pitch;
      rollingAudioSource.playOnAwake = original.playOnAwake;
      rollingAudioSource.spatialBlend = original.spatialBlend;
      rollingAudioSource.dopplerLevel = original.dopplerLevel;
      rollingAudioSource.rolloffMode = original.rolloffMode;
      rollingAudioSource.minDistance = original.minDistance;
      rollingAudioSource.maxDistance = original.maxDistance;
      rollingAudioSource.bypassEffects = original.bypassEffects;
      rollingAudioSource.bypassListenerEffects = original.bypassListenerEffects;
      rollingAudioSource.bypassReverbZones = original.bypassReverbZones;
      rollingAudioSource.priority = original.priority;
      rollingAudioSource.outputAudioMixerGroup = original.outputAudioMixerGroup;
      
      rollingAudioSource.clip = Plugin.WalkSound;
      rollingAudioSource.volume = _roamingAudioPercent;
      rollingAudioSource.loop = true;
      rollingAudioSource.time = Random.Range(0f, rollingAudioSource.clip.length);
      rollingAudioSource.pitch = Random.Range(0.96f, 1.05f);
      
      rollingAudioSource.Play();

      // Traverse.Create(parent).Field("currentChaseSpeed").SetValue(3f);
      // SetAudioClip(parent);
   }

   private static void ChangeSpringNoises(SpringManAI parent) {
      parent.springNoises = Plugin.QuickWalkSounds;
   }

   // private static void SetAudioClip(SpringManAI parent) {
   //    if (!Plugin.WalkSound) return;
   //    parent.creatureSFX.clip = Plugin.WalkSound;
   //    parent.creatureSFX.Play();
   //    _springMenPlaying[parent] = true;
   //
   //    Plugin.Log.LogInfo($"Rolling Giant {parent} set long sound");
   // }

   [HarmonyPatch(typeof(SpringManAI), "SetAnimationGoClientRpc")]
   [HarmonyPostfix]
   [ClientRpc]
   public static void PlayWalkSounds(SpringManAI __instance) {
      if (!Plugin.WalkSound/* || __instance.creatureSFX.isPlaying*/) return;
      if (__instance.enemyType != _rollingGiantEnemyType) return;
   
      var sounds = Plugin.QuickWalkSounds;
      var randomSound = sounds[Random.Range(0, sounds.Length)];
      // __instance.creatureSFX.clip = Plugin.WalkSound;
      // __instance.creatureSFX.Play();
      
      // __instance.creatureSFX.PlayOneShot(Plugin.WalkSound, 0.5f);
      WalkieTalkie.TransmitOneShotAudio(__instance.creatureSFX, randomSound, 0.4f);
      
      var rollingAudioSource = __instance.transform.Find("RollingSFX").GetComponent<AudioSource>();
      rollingAudioSource.volume = _roamingAudioPercent;
   }
   
   [HarmonyPatch(typeof(SpringManAI), "SetAnimationStopClientRpc")]
   [HarmonyPostfix]
   [ClientRpc]
   public static void StopWalkSounds(SpringManAI __instance) {
      if (!Plugin.WalkSound || !__instance.creatureSFX.isPlaying) return;
      if (__instance.enemyType != _rollingGiantEnemyType) return;
      
      var rollingAudioSource = __instance.transform.Find("RollingSFX").GetComponent<AudioSource>();
      rollingAudioSource.volume = 0;
      // Plugin.Log.LogInfo($"Rolling Giant {__instance} stopped playing long sound");
   }

   // [HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
   // [HarmonyPrefix]
   // public static void CustomAIInterval(SpringManAI __instance) {
   //    var walkingState = __instance.enemyBehaviourStates[1];
   //    walkingState.SFXClip = Plugin.WalkSound;
   //    walkingState.playOneShotSFX = false;
   //    
   //    Plugin.Log.LogInfo($"calling SpringManAI.DoAIInterval() for {__instance}");
   // }

   [HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
   [HarmonyPrefix]
   public static bool CustomAIInterval(SpringManAI __instance) {
      if (!__instance) return true;
      if (__instance.enemyType != _rollingGiantEnemyType) return true;
      if (StartOfRound.Instance.allPlayersDead || __instance.isEnemyDead) {
         return true;
      }
      
      typeof(EnemyAI).GetMethod("DoAIInterval").InvokeNotOverride(__instance, null);
   
      var rollingAudioSource = __instance.transform.Find("RollingSFX").GetComponent<AudioSource>();
      switch (__instance.currentBehaviourStateIndex) {
         case 0:
            if (!__instance.IsServer) {
               __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               break;
            }
   
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
               if (__instance.PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) &&
                   !Physics.Linecast(__instance.transform.position + Vector3.up * 0.5f,
                      StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position,
                      StartOfRound.Instance.collidersAndRoomMaskAndDefault) &&
                   Vector3.Distance(__instance.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < 30.0) {
                  __instance.SwitchToBehaviourState(1);
                  return false;
               }
            }
   
            var currentAiTypeSettings = Plugin.CurrentAiTypeSettings;
            var rampUpSpeed = Time.deltaTime / currentAiTypeSettings.MoveBuildUpDuration.Value;
            rollingAudioSource.volume = __instance.searchForPlayers.inProgress ? Mathf.Lerp(rollingAudioSource.volume, _roamingAudioPercent, rampUpSpeed) : 0;
            __instance.agent.speed = Mathf.Lerp(__instance.agent.speed, Plugin.CurrentAiTypeSettings.MoveSpeed.Value, rampUpSpeed);
            
            if (__instance.searchForPlayers.inProgress) break;
            __instance.movingTowardsTargetPlayer = false;
            rollingAudioSource.volume = 0;
            __instance.StartSearch(__instance.transform.position, __instance.searchForPlayers);
            break;
   
         case 1:
            if (__instance.searchForPlayers.inProgress) {
               __instance.StopSearch(__instance.searchForPlayers);
            }

            var (canWander, wanderDistance) = Plugin.CurrentAiTypeSettings is WanderAi wander ? wander.GetWanderSettings() : (false, 0);
            if (Plugin.CurrentAiTypeSettings is LookingTooLongKeepsAgroAiTypeSettings && _rollingGiantDatas.TryGetValue(__instance, out var data) && data.isAgro) {
               canWander = false;
            }
            
            if (__instance.TargetClosestPlayer() && (!canWander || Vector3.Distance(__instance.transform.position, __instance.targetPlayer.transform.position) < wanderDistance)) {
               var previousTarget = GetPreviousTarget(__instance);
               if (previousTarget != __instance.targetPlayer) {
                  SetPreviousTarget(__instance, __instance.targetPlayer);
                  __instance.ChangeOwnershipOfEnemy(__instance.targetPlayer.actualClientId);
               }
   
               __instance.movingTowardsTargetPlayer = true;
               break;
            }
   
            __instance.SwitchToBehaviourState(0);
            __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            // rollingAudioSource.volume = _roamingAudioPercent;
            break;
      }
   
      return false;
   }

   [HarmonyPatch(typeof(SpringManAI), "Update")]
   [HarmonyPrefix]
   public static bool CustomAIUpdate(SpringManAI __instance) {
      if (!__instance) return true;
      if (__instance.enemyType != _rollingGiantEnemyType) return true;
      if (__instance.isEnemyDead) return true;
      
      var currentAiTypeSettings = Plugin.CurrentAiTypeSettings;
      
      // call base Update on __instance
      // Plugin.Log.LogInfo($"calling SpringManAI.Update() for {__instance}");
      typeof(EnemyAI).GetMethod("Update").InvokeNotOverride(__instance, null);
      
      var timeSinceHittingPlayer = GetTimeSinceHittingPlayer(__instance);
      if (timeSinceHittingPlayer >= 0) {
         timeSinceHittingPlayer -= Time.deltaTime;
         SetTimeSinceHittingPlayer(__instance, timeSinceHittingPlayer);
      }

      // if (Plugin.WalkSound) {
      //    var walkingState = __instance.enemyBehaviourStates[1];
      //    walkingState.VoiceClip = Plugin.WalkSound;
      //    walkingState.playOneShotVoice = false;
      // }

      var rollingAudioSource = __instance.transform.Find("RollingSFX").GetComponent<AudioSource>();
      switch (__instance.currentBehaviourStateIndex) {
         // case 0:
         //    rollingAudioSource.volume = __instance.creatureSFX.volume;
         //    break;
         case 1:
            if (__instance.IsOwner) {
               var stopAndGoMinimumInterval = GetStopAndGoMinimumInterval(__instance);
               if (stopAndGoMinimumInterval > 0) {
                  stopAndGoMinimumInterval -= Time.deltaTime;
                  SetStopAndGoMinimumInterval(__instance, stopAndGoMinimumInterval);
               }
               
               var wasOwnerLastFrame = GetWasOwnerLastFrame(__instance);
               if (!wasOwnerLastFrame) {
                  wasOwnerLastFrame = true;
                  SetWasOwnerLastFrame(__instance, true);
                  if (!GetStoppingMovement(__instance) && GetTimeSinceHittingPlayer(__instance) < 0.11999999731779099) {
                     __instance.agent.speed = currentAiTypeSettings.MoveSpeed.Value;
                  } else {
                     __instance.agent.speed = 0;
                     rollingAudioSource.volume = 0;
                  }
               }
               
               if (!_rollingGiantDatas.TryGetValue(__instance, out var data)) {
                  _rollingGiantDatas[__instance] = new RollingGiantData {
                     // waitTime = Random.Range(2f, 10f)
                     waitTime = Random.Range(1, 3)
                  };
               }

               var isStopped = false;
               PlayerControllerB player = null;
               switch (Plugin.AiSettings.AiType.Value) {
                  case RollingGiantAiType.Coilhead:
                     HandleNormalAI(__instance, ref isStopped, ref data, out player);
                     break;
                  case RollingGiantAiType.MoveWhenLooking:
                     HandleMoveWhenLookingAI(__instance, ref isStopped, ref data, out player);
                     break;
                  case RollingGiantAiType.RandomlyMoveWhileLooking:
                     HandleRandomMovementsWhileLookingAI(__instance, ref isStopped, ref data, out player);
                     break;
                  case RollingGiantAiType.LookingTooLongKeepsAgro:
                     HandleMoveWhenLookingTooLongAI(__instance, ref isStopped, ref data, out player);
                     break;
                  case RollingGiantAiType.FollowOnceAgro:
                     HandleFollowOnceAgro(__instance, ref isStopped, ref data, out player);
                     break;
                  case RollingGiantAiType.OnceSeenAgroAfterTimer:
                     HandleOnceSeenAgroAfterTimer(__instance, ref isStopped, ref data, out player);
                     break;
                  default:
                     HandleNormalAI(__instance, ref isStopped, ref data, out player);
                     break;
               }

               if (isStopped) {
                  if (player && currentAiTypeSettings.RotateToLookAtPlayer.Value) {
                     data.stoppedTimer += Time.deltaTime;
                     if (data.stoppedTimer > currentAiTypeSettings.DelayBeforeLookingAtPlayer.Value) {
                        var t = __instance.transform;
                        var dirToPlayerEyes = (player.gameplayCamera.transform.position - t.position).normalized;
                        var rotation = Quaternion.LookRotation(dirToPlayerEyes);
                        
                        // only keep the y rotation
                        var euler = rotation.eulerAngles;
                        euler.x = 0;
                        euler.z = 0;
                        rotation = Quaternion.Euler(euler);
                        rotation = Quaternion.Slerp(t.rotation, rotation, Time.deltaTime / currentAiTypeSettings.LookAtPlayerDuration.Value);
                        __instance.transform.rotation = rotation;
                     }
                  }
               } else {
                  data.stoppedTimer = 0;
               }
               
               _rollingGiantDatas[__instance] = data;

               if (__instance.stunNormalizedTimer > 0) {
                  isStopped = true;
               }

               if (isStopped != GetStoppingMovement(__instance) && GetStopAndGoMinimumInterval(__instance) <= 0) {
                  SetStopAndGoMinimumInterval(__instance, 0.15f);

                  if (isStopped) {
                     __instance.SetAnimationStopServerRpc();
                  } else {
                     __instance.SetAnimationGoServerRpc();
                  }

                  SetStoppingMovement(__instance, isStopped);
               }
            }

            if (GetStoppingMovement(__instance)) {
               if (!__instance.animStopPoints.canAnimationStop) break;
               if (!GetHasStopped(__instance)) {
                  SetHasStopped(__instance, true);
                  if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(__instance.transform.position, 70f, 25)) {
                     float num = Vector3.Distance(__instance.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
                     if (num < 4.0) {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
                     } else if (num < 9.0) {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
                     }
                  }
                  if (GetCurrentAnimSpeed(__instance) > 2.0) {
                     RoundManager.PlayRandomClip(__instance.creatureVoice, __instance.springNoises, false);
                     if (__instance.animStopPoints.animationPosition == 1) {
                        __instance.creatureAnimator.SetTrigger("springBoing");
                     } else {
                        __instance.creatureAnimator.SetTrigger("springBoingPosition2");
                     }
                  }
               }
               if (__instance.mainCollider.isTrigger &&
                   Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, __instance.transform.position) > 0.25)
                  __instance.mainCollider.isTrigger = false;
               __instance.creatureAnimator.SetFloat("walkSpeed", 0.0f);
               SetCurrentAnimSpeed(__instance, 0.0f);
               rollingAudioSource.volume = 0;
               if (!__instance.IsOwner) break;
               __instance.agent.speed = 0.0f;
               break;
            }

            if (GetHasStopped(__instance)) {
               SetHasStopped(__instance, false);
               __instance.mainCollider.isTrigger = true;
            }
            
            SetCurrentAnimSpeed(__instance, Mathf.Lerp(GetCurrentAnimSpeed(__instance), currentAiTypeSettings.MoveSpeed.Value, 5f * Time.deltaTime));
            var speed = GetCurrentAnimSpeed(__instance);
            __instance.creatureAnimator.SetFloat("walkSpeed", GetCurrentAnimSpeed(__instance));
            if (speed <= 0.1) {
               rollingAudioSource.volume = 0;
            }
            
            if (!__instance.IsOwner) break;

            // var rampUpDuration = 1f / Plugin.AiWaitTimeMin.Value;
            var rampUpSpeed = Time.deltaTime / currentAiTypeSettings.MoveBuildUpDuration.Value;
            __instance.agent.speed = Mathf.Lerp(__instance.agent.speed, currentAiTypeSettings.MoveSpeed.Value, rampUpSpeed);
            rollingAudioSource.volume = Mathf.Lerp(rollingAudioSource.volume, _roamingAudioPercent, rampUpSpeed);
            break;
      }

      return false;
   }
   
   private static void HandleNormalAI(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      var isLookingAt = IsPlayerLooking(__instance, out player);
      if (isLookingAt) {
         isStopped = true;
      }
   }

   private static void HandleRandomMovementsWhileLookingAI(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      if (Plugin.CurrentAiTypeSettings is not RandomlyMoveWhileLookingAiTypeSettings aiSettings) {
         HandleNormalAI(__instance, ref isStopped, ref data, out player);
         return;
      }
      
      var isLookingAt = IsPlayerLooking(__instance, out player);
      if (isLookingAt) {
         isStopped = true;
         
         if (data.waitTime > 0) {
            data.waitTime -= Time.deltaTime;
         } else if (data.waitTime <= 0 && !data.isMoving) {
            // data.moveTimer = Random.Range(1f, 3f);
            data.moveTimer = aiSettings.GetRandomMoveTime(RoundManager.Instance.LevelRandom);
            data.isMoving = true;
         }

         if (data.isMoving && data.moveTimer > 0) {
            data.moveTimer -= Time.deltaTime;
            isStopped = false;
         } else if (data.isMoving && data.moveTimer <= 0) {
            // data.waitTime = Random.Range(2f, 5f);
            data.waitTime = aiSettings.GetRandomWaitTime(RoundManager.Instance.LevelRandom);
            data.isMoving = false;
            isStopped = true;
         }
      }
   }

   private static void HandleMoveWhenLookingAI(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      var isLookingAt = IsPlayerLooking(__instance, out player);
      if (isLookingAt) {
         isStopped = false;
      } else {
         isStopped = true;
      }
   }

   private static void HandleMoveWhenLookingTooLongAI(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      if (Plugin.CurrentAiTypeSettings is not LookingTooLongKeepsAgroAiTypeSettings aiSettings) {
         HandleNormalAI(__instance, ref isStopped, ref data, out player);
         return;
      }
      
      if (data.isAgro) {
         isStopped = false;
         player = null;
         return;
      }
      
      var isLookingAt = IsPlayerLooking(__instance, out player);
      if (isLookingAt) {
         isStopped = true;
         
         data.lookTimer += Time.deltaTime;
         
         if (!data.isAgro && data.lookTimer >= aiSettings.LookTimeBeforeAgro.Value) {
            isStopped = false;
            data.isAgro = true;
            return;
         }
      }
      
      HandleNormalAI(__instance, ref isStopped, ref data, out player);
   }

   private static void HandleFollowOnceAgro(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      isStopped = false;
      player = null;
   }
   
   private static void HandleOnceSeenAgroAfterTimer(SpringManAI __instance, ref bool isStopped, ref RollingGiantData data, out PlayerControllerB player) {
      if (Plugin.CurrentAiTypeSettings is not OnceSeenAgroAfterTimerAiTypeSettings aiSettings) {
         HandleNormalAI(__instance, ref isStopped, ref data, out player);
         return;
      }
      
      if (data.isAgro) {
         isStopped = false;
         player = null;
         return;
      }
      
      var isLookingAt = IsPlayerLooking(__instance, out player);
      if (isLookingAt) {
         isStopped = true;

         if (!data.isLookWaiting) {
            data.isLookWaiting = true;
            data.lookTimer = aiSettings.GetRandomWaitTime(RoundManager.Instance.LevelRandom);
         }
         
         data.lookTimer -= Time.deltaTime;
         
         if (!data.isAgro && data.lookTimer <= 0) {
            isStopped = false;
            data.isAgro = true;
         }
      }
   }

   private static bool IsPlayerLooking(SpringManAI __instance, out PlayerControllerB player) {
      var flag = false;
      player = null;
      
      for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; ++i) {
         var p = StartOfRound.Instance.allPlayerScripts[i];
         if (__instance.PlayerIsTargetable(p) &&
             StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(__instance.transform.position + Vector3.up * 1.6f, 68f) &&
             Vector3.Distance(p.gameplayCamera.transform.position, __instance.eye.position) > 0.30000001192092896) {
            flag = true;
            player = p;
         }
      }

      return flag;
   }
   
   private static PlayerControllerB GetPreviousTarget(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("previousTarget").GetValue<PlayerControllerB>();
   }
   
   private static void SetPreviousTarget(SpringManAI __instance, PlayerControllerB value) {
      Traverse.Create(__instance).Field("previousTarget").SetValue(value);
   }
   
   private static bool GetWasOwnerLastFrame(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("wasOwnerLastFrame").GetValue<bool>();
   }
   
   private static void SetWasOwnerLastFrame(SpringManAI __instance, bool value) {
      Traverse.Create(__instance).Field("wasOwnerLastFrame").SetValue(value);
   }
   
   private static float GetStopAndGoMinimumInterval(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("stopAndGoMinimumInterval").GetValue<float>();
   }
   
   private static void SetStopAndGoMinimumInterval(SpringManAI __instance, float value) {
      Traverse.Create(__instance).Field("stopAndGoMinimumInterval").SetValue(value);
   }
   
   private static float GetTimeSinceHittingPlayer(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("timeSinceHittingPlayer").GetValue<float>();
   }
   
   private static void SetTimeSinceHittingPlayer(SpringManAI __instance, float value) {
      Traverse.Create(__instance).Field("timeSinceHittingPlayer").SetValue(value);
   }
   
   private static bool GetStoppingMovement(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("stoppingMovement").GetValue<bool>();
   }
   
   private static void SetStoppingMovement(SpringManAI __instance, bool value) {
      Traverse.Create(__instance).Field("stoppingMovement").SetValue(value);
   }
   
   private static bool GetHasStopped(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("hasStopped").GetValue<bool>();
   }
   
   private static void SetHasStopped(SpringManAI __instance, bool value) {
      Traverse.Create(__instance).Field("hasStopped").SetValue(value);
   }
   
   private static float GetCurrentAnimSpeed(SpringManAI __instance) {
      return Traverse.Create(__instance).Field("currentAnimSpeed").GetValue<float>();
   }
   
   private static void SetCurrentAnimSpeed(SpringManAI __instance, float value) {
      Traverse.Create(__instance).Field("currentAnimSpeed").SetValue(value);
   }
}