﻿using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RollingGiant.Patches;
using RollingGiant.Settings;
using UnityEngine;

#if DEBUG
using System.Diagnostics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
#endif

namespace RollingGiant;

public enum RollingGiantAiType {
    Coilhead,
    InverseCoilhead,
    RandomlyMoveWhileLooking,
    LookingTooLongKeepsAgro,
    FollowOnceAgro,
    OnceSeenAgroAfterTimer
}

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
// [BepInDependency("com.willis.lc.lethalsettings", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    public const string PluginGuid = "nomnomab.rollinggiant";
    public const string PluginName = "Rolling Giant";
    public const string PluginVersion = "2.0.1";
    
    private const int SaveFileVersion = 1;

    public static string PluginDirectory;
    public static CustomConfig CustomConfig { get; private set; }
    public static ConfigFile Config { get; private set; }

    public static AssetBundle Bundle;
    public static EnemyType EnemyType;
    public static TerminalNode EnemyTerminalNode;
    public static TerminalKeyword EnemyTerminalKeyword;
    public static AudioClip WalkSound;
    public static AudioClip[] StopSounds;
    public static GameObject PlayerRagdoll;
    public static Item PosterItem;

    internal static ManualLogSource Log;

    private void Awake() {
        Config = base.Config;
        Log = Logger;
        PluginDirectory = Info.Location;
        LoadSettings();
        RemoveOldSettings();
        // LethalSettingsAccessor.GenerateUi();
#if DEBUG
        new ILHook(typeof(StackTrace).GetMethod("AddFrames", BindingFlags.Instance | BindingFlags.NonPublic), IlHook);
#endif
        LoadAssets();
        LoadNetWeaver();
        
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void LoadNetWeaver() {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types) {
            // ? prevents the compatibility layer from crashing the plugin loading
            try {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) {
                        method.Invoke(null, null);
                    }
                }
            } catch {
                Log.LogWarning($"NetWeaver is skipping {type.FullName}");
            }
        }
    }

    private void LoadSettings() {
        CustomConfig = new CustomConfig(base.Config);
    }

    private void RemoveOldSettings() {
        var version = base.Config.Bind("z_Ignore", "__version", 0, "The version of this config file. Do not change this.").Value;
        if (version != SaveFileVersion) {
            Log.LogMessage($"Removing old settings... ({version} != {SaveFileVersion})");

            // back up config file and nuke the old one
            var configFile = base.Config.ConfigFilePath;
            var backupFile = configFile + ".bak";
            File.Copy(configFile, backupFile, true);
            File.WriteAllText(configFile, "");

            // reload from disk the empty file
            base.Config.Reload();

            // reload the bindings, but don't get the values from them
            CustomConfig.Reload(setValues: false);

            base.Config.Bind("z_Ignore", "__version", SaveFileVersion).Value = SaveFileVersion;

            // copy the values from the backup into the new config bindings
            CustomConfig.AssignFromSaved();
            base.Config.Save();
        } else {
            Log.LogMessage($"Settings version is up to date ({version} == {SaveFileVersion})");
        }
    }

    private void LoadAssets() {
        try {
            var dirName = Path.GetDirectoryName(PluginDirectory);
            if (dirName == null) {
                throw new System.Exception("Failed to get directory name!");
            }
            Bundle = AssetBundle.LoadFromFile(Path.Combine(dirName, "rollinggiant"));

            EnemyType = Bundle.LoadAsset<EnemyType>("Assets/RollingGiant/Data/RollingGiant_EnemyType.asset");
            NetworkPatches.RegisterPrefab(EnemyType.enemyPrefab);

            EnemyTerminalNode = Bundle.LoadAsset<TerminalNode>("Assets/RollingGiant/Data/RollingGiant_TerminalNode.asset");
            EnemyTerminalKeyword = Bundle.LoadAsset<TerminalKeyword>("Assets/RollingGiant/Data/RollingGiant_TerminalKeyword.asset");
        }
        catch (System.Exception e) {
            Log.LogError($"Failed to load asset bundle! {e}");
        }

        try {
            WalkSound = Bundle.LoadAsset<AudioClip>("Assets/RollingGiant/Audio/MovingLoop.wav");
            StopSounds = new AudioClip[5];
            for (int i = 0; i < 5; i++) {
                StopSounds[i] = Bundle.LoadAsset<AudioClip>($"Assets/RollingGiant/Audio/Stopped{i + 1}.wav");
            }
            PlayerRagdoll = Bundle.LoadAsset<GameObject>("Assets/RollingGiant/PlayerRagdollRollingGiant Variant.prefab");
            PlayerRagdoll.AddComponent<RollingGiantDeadBody>();
            
            PosterItem = Bundle.LoadAsset<Item>("Assets/RollingGiant/Data/RollingGiant_PosterItem.asset");
            PosterItem.rotationOffset += new Vector3(45, 0, 0);
            PosterItem.positionOffset += new Vector3(-0.1f, -0.12f, 0.15f);
            Destroy(PosterItem.spawnPrefab.GetComponent<PhysicsProp>());
            PosterItem.spawnPrefab.AddComponent<Poster>().Init();
            NetworkPatches.RegisterPrefab(PosterItem.spawnPrefab);
        }
        catch (System.Exception e) {
            Log.LogError($"Failed to load assets! {e}");
        }
    }

#if DEBUG
    private void IlHook(ILContext il) {
        var cursor = new ILCursor(il);
        cursor.GotoNext(
            x => x.MatchCallvirt(typeof(StackFrame).GetMethod("GetFileLineNumber", BindingFlags.Instance | BindingFlags.Public))
        );
        cursor.RemoveRange(2);
        cursor.EmitDelegate(GetLineOrIL);
    }

    private static string GetLineOrIL(StackFrame instance) {
        var line = instance.GetFileLineNumber();
        if (line == StackFrame.OFFSET_UNKNOWN || line == 0) {
            return "IL_" + instance.GetILOffset().ToString("X4");
        }

        return line.ToString();
    }
#endif
}

#if DEBUG
[HarmonyPatch]
public class DebugEditorPatch {
    [HarmonyPatch(typeof(Application), "isEditor", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ForceIsEditor(ref bool __result) {
        __result = true;
    }
}
#endif