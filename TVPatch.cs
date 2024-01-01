// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using HarmonyLib;
// using TVLoader;
//
// namespace RollingGiant; 
//
// [HarmonyPatch]
// class TVPatch {
//     private static Type _tvManagerType;
//     
//     public static MethodBase TargetMethod() {
//         var tvLoaderPlugin = typeof(TVLoaderPlugin);
//         var assembly = tvLoaderPlugin.Assembly;
//         var types = assembly.GetTypes();
//         var tvManagerType = types.First(t => t.Name == "VideoManager");
//         _tvManagerType = tvManagerType;
//         return AccessTools.FirstMethod(tvManagerType, x => x.Name.Contains("Load"));
//     }
//
//     public static void Postfix() {
//         var videosField = AccessTools.Field(_tvManagerType, "Videos");
//         var videos = (List<string>) videosField.GetValue(null);
//
//         if (videos.Count == 0) {
//             Plugin.Log.LogInfo("No videos found");
//             return;
//         }
//         
//         // shuffle
//         var random = new Random();
//         videos = videos.OrderBy(x => random.Next()).ToList();
//         videosField.SetValue(null, videos);
//         Plugin.Log.LogInfo($"Shuffled {videos.Count} videos");
//     }
// }