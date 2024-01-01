using System.Linq;
using HarmonyLib;

namespace RollingGiant.Patches; 

[HarmonyPatch]
public static class TerminalPatches {
    [HarmonyPatch(typeof(Terminal), "Start")]
    [HarmonyPrefix]
    private static void RegisterTerminal(Terminal __instance) {
        var terminalNode = Plugin.EnemyTerminalNode;
        if (__instance.enemyFiles.Any(x => x == terminalNode || x.creatureName == terminalNode.creatureName)) {
            return;
        }

        var infoKeyword = __instance.terminalNodes.allKeywords.First(x => x.word == "info");
        var keyword = Plugin.EnemyTerminalKeyword;
        keyword.defaultVerb = infoKeyword;
        
        var allKeywords = __instance.terminalNodes.allKeywords.ToList();
        if (allKeywords.All(x => x.word != keyword.word)) {
            allKeywords.Add(keyword);
            __instance.terminalNodes.allKeywords = allKeywords.ToArray();
        }

        var itemInfoNouns = infoKeyword.compatibleNouns.ToList();
        if (itemInfoNouns.All(x => x.noun.word != keyword.word)) {
            itemInfoNouns.Add(new CompatibleNoun {
                noun = keyword,
                result = terminalNode
            });
        }
        infoKeyword.compatibleNouns = itemInfoNouns.ToArray();
        
        var newId = __instance.enemyFiles.Count;
        terminalNode.creatureFileID = newId;
        Plugin.EnemyTypeInside.enemyPrefab.GetComponentInChildren<ScanNodeProperties>().creatureScanID = newId;
        __instance.enemyFiles.Add(terminalNode);
    }

    // ? cleans up ids that are no longer valid
    [HarmonyPatch(typeof(Terminal), "Start")]
    [HarmonyPostfix]
    private static void ValidateIds(Terminal __instance) {
        var enemyFiles = __instance.enemyFiles;
        var scannedEnemyIds = __instance.scannedEnemyIDs;
        for (int i = 0; i < scannedEnemyIds.Count; i++) {
            var id = scannedEnemyIds[i];
            if (id < 0 || id >= enemyFiles.Count || !enemyFiles[id]) {
                scannedEnemyIds.RemoveAt(i--);
            }
        }
    }
}