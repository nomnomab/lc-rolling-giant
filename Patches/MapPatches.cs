using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace RollingGiant.Patches; 

public static class MapPatches {
    // [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
    // [HarmonyPrefix]
    // private static void SpawnMapObjects() {
    //     var array = GameObject.FindObjectsOfType<RandomMapObject>();
    //     var posterItem = new SpawnableMapObject {
    //         prefabToSpawn = Plugin.PosterItem.spawnPrefab,
    //         numberToSpawn = AnimationCurve.Constant(0, 1, 1)
    //     };
    //     foreach (var randomMapObject in array) {
    //         if (!randomMapObject.spawnablePrefabs.Any(x => x == posterItem.prefabToSpawn)) {
    //             randomMapObject.spawnablePrefabs.Add(posterItem.prefabToSpawn);
    //         }
    //     }
    // }
}