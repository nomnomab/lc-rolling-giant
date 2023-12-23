using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public class CoilheadPatch: MonoBehaviour {
   private static EnemyType _rollingGiantEnemyType;
   private static TerminalNode _giantInfoNode;
   private static Dictionary<SpringManAI, bool> _springMenPlaying = new();
   
   private void FixedUpdate() {
      using var _ = HashSetPool<SpringManAI>.Get(out var dead);
      using var __ = HashSetPool<SpringManAI>.Get(out var setCanPlay);
      foreach (var (springMan, isPlaying) in _springMenPlaying) {
         if (!springMan || !springMan.creatureSFX) {
            dead.Add(springMan);
            continue;
         }
         
         if (!springMan.creatureSFX.isPlaying && isPlaying) {
            setCanPlay.Add(springMan);
         }
      }
      
      foreach (var springMan in dead) {
         _springMenPlaying.Remove(springMan);
      }

      foreach (var springMan in setCanPlay) {
         _springMenPlaying[springMan] = false;
      }
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
      newNouns.Add( new CompatibleNoun {
         noun = baseKeyword,
         result = _giantInfoNode
      });
      newNouns.Add( new CompatibleNoun {
         noun = rollingKeyword,
         result = _giantInfoNode
      });
      newNouns.Add( new CompatibleNoun {
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

      var chance = Random.value;
      if (chance > Plugin.ChanceForGiant.Value) {
         Plugin.Log.LogInfo($"Rolling Giant {springManAI} failed to spawn ({chance} > {Plugin.ChanceForGiant.Value})");
         return;
      }

      var model = springManAI.transform.Find("SpringManModel");
      Destroy(model.Find("Body").gameObject.GetComponent<SkinnedMeshRenderer>());
      Destroy(model.Find("Head").gameObject.GetComponent<MeshRenderer>());
      SpawnRollingGiant(springManAI);
      ChangeSpringNoises(springManAI);
      
      Plugin.Log.LogInfo($"Summoned Rolling Giant! ({chance} <= {Plugin.ChanceForGiant.Value})");
   }

   private static void SpawnRollingGiant(SpringManAI parent) {
      if (!parent) return;
      
      var instance = Instantiate(Plugin.RollingGiantModel).transform;
      instance.SetParent(parent.transform.Find("SpringManModel"));
      instance.localPosition = Vector3.zero;
      instance.localRotation = Quaternion.identity;
      instance.localScale = Vector3.one;

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

      // Traverse.Create(parent).Field("currentChaseSpeed").SetValue(3f);
      SetAudioClip(parent);
   }

   private static void ChangeSpringNoises(SpringManAI parent) {
      parent.springNoises = Plugin.QuickWalkSounds;
   }

   private static void SetAudioClip(SpringManAI parent) {
      if (!Plugin.WalkSound) return;
      parent.creatureSFX.clip = Plugin.WalkSound;
      parent.creatureSFX.Play();
      _springMenPlaying[parent] = true;
      
      Plugin.Log.LogInfo($"Rolling Giant {parent} started playing long sound");
   }
   
   [HarmonyPatch(typeof(SpringManAI), "SetAnimationGoClientRpc")]
   [HarmonyPostfix]
   [ClientRpc]
   public static void PlayWalkSounds(SpringManAI __instance) {
      if (!Plugin.WalkSound || __instance.creatureSFX.isPlaying) return;
      if (__instance.enemyType != _rollingGiantEnemyType) return;
     
      var canPlay = !_springMenPlaying.TryGetValue(__instance, out var isPlaying) || !isPlaying;
      if (__instance.searchForPlayers.inProgress && canPlay) {
         SetAudioClip(__instance);
         return;
      }

      var sounds = Plugin.QuickWalkSounds;
      var randomSound = sounds[Random.Range(0, sounds.Length)];
      // __instance.creatureSFX.PlayOneShot(randomSound, 0.75f);
      WalkieTalkie.TransmitOneShotAudio(__instance.creatureSFX, randomSound, 0.4f);
   }
   
   [HarmonyPatch(typeof(SpringManAI), "SetAnimationStopClientRpc")]
   [HarmonyPostfix]
   [ClientRpc]
   public static void StopWalkSounds(SpringManAI __instance) {
      if (!Plugin.WalkSound || !__instance.creatureSFX.isPlaying) return;
      if (__instance.enemyType != _rollingGiantEnemyType) return;
      __instance.creatureSFX.Stop();
   }
}