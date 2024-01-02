using System.Linq;
using HarmonyLib;
using RollingGiant.Settings;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public static class EnemyPatches {
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void RegisterEnemy(StartOfRound __instance) {
        // if (!(NetworkHandler.Instance.IsHost || NetworkHandler.Instance.IsServer)) {
        //     return;
        // }
        
        var canSpawnIn = GetLevelChances(CustomConfig.SpawnIn);
        var scrapCanSpawnIn = GetLevelChances(CustomConfig.SpawnPosterIn);

        if (!__instance.allItemsList.itemsList.Contains(Plugin.PosterItem)) {
            __instance.allItemsList.itemsList.Add(Plugin.PosterItem);
        }

        var spawnInAny = CustomConfig.SpawnInAny;
        foreach (var level in __instance.levels) {
            try {
                var levelName = level.PlanetName.ToLower().Replace(" ", string.Empty);
                foreach (var (name, chance) in canSpawnIn) {
                    var finalChance = chance;
                    if (!spawnInAny && !levelName.Contains(name)) {
                        continue;
                    }

                    if (spawnInAny) {
                        finalChance = CustomConfig.SpawnInAnyChance;
                    }

                    if (!level.spawnableScrap.Any(x => x.spawnableItem == Plugin.PosterItem)) {
                        var scrap = scrapCanSpawnIn.FirstOrDefault(x => levelName.Contains(x.Item1));
                        if (!string.IsNullOrEmpty(scrap.Item1)) {
                            level.spawnableScrap.Add(new SpawnableItemWithRarity {
                                spawnableItem = Plugin.PosterItem,
                                rarity = scrap.Item2
                            });

                            Plugin.Log.LogMessage($"Added {Plugin.PosterItem.itemName} to {level.PlanetName} with chance of {scrap.Item2}");
                        }
                    }

                    if (CustomConfig.CanSpawnInside && level.Enemies.All(x => x.enemyType != Plugin.EnemyTypeInside)) {
                        level.Enemies.Add(new SpawnableEnemyWithRarity {
                            enemyType = Plugin.EnemyTypeInside,
                            rarity = finalChance
                        });
                        Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeOutside.enemyName} to {level.PlanetName} with chance of {finalChance} (inside)");
                    }

                    if (!CustomConfig.DisableOutsideAtNight && CustomConfig.CanSpawnOutside && level.OutsideEnemies.All(x => x.enemyType != Plugin.EnemyTypeOutside)) {
                        level.OutsideEnemies.Add(new SpawnableEnemyWithRarity {
                            enemyType = Plugin.EnemyTypeOutside,
                            rarity = finalChance
                        });
                        Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeOutside.enemyName} to {level.PlanetName} with chance of {finalChance} (outside)");
                    }

                    if (CustomConfig.DisableOutsideAtNight && CustomConfig.CanSpawnOutside && level.DaytimeEnemies.All(x => x.enemyType != Plugin.EnemyTypeOutsideDaytime)) {
                        level.DaytimeEnemies.Add(new SpawnableEnemyWithRarity {
                            enemyType = Plugin.EnemyTypeOutsideDaytime,
                            rarity = finalChance
                        });
                        Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeOutsideDaytime.enemyName} to {level.PlanetName} with chance of {finalChance} (daytime)");
                    }
                }
            }
            catch (System.Exception e) {
                Plugin.Log.LogError($"Failed to add enemy to {level.PlanetName}!\n{e}");
            }
        }
    }

    private static (string, int)[] GetLevelChances(string str) {
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

    [HarmonyPatch(typeof(QuickMenuManager), "Debug_SetEnemyDropdownOptions")]
    [HarmonyPrefix]
    private static void AddGiantToDebugList(QuickMenuManager __instance) {
        // ? isn't in the main level list so have to add here
        // ? adding in Start didn't seem to work
        var testLevel = __instance.testAllEnemiesLevel;
        var firstEnemy = testLevel.Enemies.FirstOrDefault();
        if (firstEnemy == null) {
            Plugin.Log.LogError("Failed to get first enemy for debug list!");
            return;
        }
        
        var enemies = testLevel.Enemies;
        var outsideEnemies = testLevel.OutsideEnemies;
        var daytimeEnemies = testLevel.DaytimeEnemies;
        
        if (enemies.All(x => x.enemyType != Plugin.EnemyTypeInside)) {
            enemies.Add(new SpawnableEnemyWithRarity {
                enemyType = Plugin.EnemyTypeInside,
                rarity = firstEnemy.rarity
            });
            
            Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeInside.enemyName} to debug list");
        }
        
        if (outsideEnemies.All(x => x.enemyType != Plugin.EnemyTypeOutside)) {
            outsideEnemies.Add(new SpawnableEnemyWithRarity {
                enemyType = Plugin.EnemyTypeOutside,
                rarity = firstEnemy.rarity
            });
            
            Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeOutside.enemyName} to debug list");
        }
        
        if (daytimeEnemies.All(x => x.enemyType != Plugin.EnemyTypeOutsideDaytime)) {
            daytimeEnemies.Add(new SpawnableEnemyWithRarity {
                enemyType = Plugin.EnemyTypeOutsideDaytime,
                rarity = firstEnemy.rarity
            });
            
            Plugin.Log.LogMessage($"Added {Plugin.EnemyTypeOutsideDaytime.enemyName} to debug list");
        }
    }
}