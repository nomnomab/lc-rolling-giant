using System.Linq;
using HarmonyLib;
using RollingGiant.Settings;
using UnityEngine;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public static class EnemyPatches {
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void RegisterEnemy(StartOfRound __instance) {
        var config = CustomConfig.Instance;
        var canSpawnIn = getLevelChances(config.SpawnIn);
        var canSpawnOutside = config.SpawnOutside
            .ToLower()
            .Replace(" ", string.Empty)
            .Split(",");
        var canSpawnDaytime = config.SpawnDaytime
            .ToLower()
            .Replace(" ", string.Empty)
            .Split(",");
        var canSpawnInside = config.SpawnInside
            .ToLower()
            .Replace(" ", string.Empty)
            .Split(",");
        var scrapCanSpawnIn = getLevelChances(config.SpawnPosterIn);
        
        (string, int)[] getLevelChances(string str) {
            if (string.IsNullOrEmpty(str)) {
                return new (string, int)[0];
            }
            return str.Replace(" ", string.Empty)
                .Split(",")
                .Select(x =>
                {
                    if (!x.Contains(":")) {
                        return (x.ToLower().Replace(" ", string.Empty), 0);
                    }

                    var split = x.Split(":");
                    if (!int.TryParse(split[1], out var chance)) {
                        chance = 0;
                    }

                    return (split[0].ToLower().Replace(" ", string.Empty), chance);
                }).ToArray();
        }
        
        if (!__instance.allItemsList.itemsList.Contains(Plugin.PosterItem)) {
            __instance.allItemsList.itemsList.Add(Plugin.PosterItem);
        }
        
        foreach (var level in __instance.levels) {
            var levelName = level.PlanetName.ToLower().Replace(" ", string.Empty);
            foreach (var (name, chance) in canSpawnIn) {
                if (!levelName.Contains(name)) {
                    continue;
                }
                
                var rarity = new SpawnableEnemyWithRarity {
                    enemyType = Plugin.EnemyType,
                    rarity = chance
                };

                if (!level.spawnableScrap.Any(x => x.spawnableItem == Plugin.PosterItem)) {
                    var scrap = scrapCanSpawnIn.FirstOrDefault(x => levelName.Contains(x.Item1));
                    if (!string.IsNullOrEmpty(scrap.Item1)) {
                        level.spawnableScrap.Add(new SpawnableItemWithRarity {
                            spawnableItem = Plugin.PosterItem,
                            rarity = scrap.Item2 * 50
                        });

                        Plugin.Log.LogMessage($"Added {Plugin.PosterItem.itemName} to {level.PlanetName} with chance of {scrap.Item2}");
                    }
                }
                
                if (canSpawnInside.Contains(name) && level.Enemies.All(x => x.enemyType != Plugin.EnemyType)) {
                    level.Enemies.Add(rarity);
                    Plugin.Log.LogMessage($"Added {Plugin.EnemyType.enemyName} to {level.PlanetName} with chance of {chance} (inside)");
                }
                
                if (canSpawnOutside.Contains(name) && level.OutsideEnemies.All(x => x.enemyType != Plugin.EnemyType)) {
                    level.OutsideEnemies.Add(rarity);
                    Plugin.Log.LogMessage($"Added {Plugin.EnemyType.enemyName} to {level.PlanetName} with chance of {chance} (outside)");
                }
                
                if (canSpawnDaytime.Contains(name) && level.DaytimeEnemies.All(x => x.enemyType != Plugin.EnemyType)) {
                    level.DaytimeEnemies.Add(rarity);
                    Plugin.Log.LogMessage($"Added {Plugin.EnemyType.enemyName} to {level.PlanetName} with chance of {chance} (daytime)");
                }
            }
        }
    }
}