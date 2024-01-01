// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using LethalSettings.UI;
// using LethalSettings.UI.Components;
// using TMPro;
//
// namespace RollingGiant.Settings;
//
// public static class LethalSettingsCompatibility {
//     private static bool? _enabled;
//
//     public static bool enabled {
//         get {
//             if (_enabled == null) {
//                 _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.willis.lc.lethalsettings");
//             }
//             return (bool)_enabled;
//         }
//     }
//
//     [MethodImpl(MethodImplOptions.NoInlining)]
//     public static void GenerateUi() {
//         var config = CustomConfig.Instance;
//         ModMenu.RegisterMod(new ModMenu.ModSettingsConfig {
//             Name = Plugin.PluginName,
//             Id = Plugin.PluginGuid,
//             Description = "Adds the Rolling Giant as a new enemy type. Sounds are included.",
//             Version = Plugin.PluginVersion,
//             MenuComponents = GenerateGeneralSettings(config)
//                 .Concat(GenerateAISettings(config))
//                 .Concat(GenerateAITypes(config))
//                 .Concat(GenerateHost(config))
//                 .ToArray()
//         });
//     }
//
//     private static IEnumerable<MenuComponent> GenerateGeneralSettings(CustomConfig config) {
//         yield return GetDefaultLabel("General Settings");
//         yield return new HorizontalComponent {
//             Children = new MenuComponent[] {
//                 new VerticalComponent {
//                     Children = new MenuComponent[] {
//                         GetContentLabel("Giant Scale Min"),
//                         GetFloatField(CustomConfig.GiantScaleMinEntry.Value,
//                             x =>
//                             {
//                                 CustomConfig.GiantScaleMinEntry.Value = x;
//                                 config.AssignFromSaved();
//                                 config.Save();
//                             }),
//                     }
//                 },
//                 new VerticalComponent {
//                     Children = new MenuComponent[] {
//                         GetContentLabel("Giant Scale Max"),
//                         GetFloatField(
//                             CustomConfig.GiantScaleMaxEntry.Value,
//                             x =>
//                             {
//                                 CustomConfig.GiantScaleMaxEntry.Value = x;
//                                 config.AssignFromSaved();
//                                 config.Save();
//                             }),
//                     }
//                 }
//             }
//         };
//
//         // yield return GetContentLabel("Levels to Spawn In");
//         // Plugin.Log.LogInfo($"Levels to Spawn In: {config.SpawnIn}");
//         // yield return GetStringField(
//         //     config.SpawnIn,
//         //     x =>
//         //     {
//         //         CustomConfig.SpawnInEntry.Value = x;
//         //         config.AssignFromSaved();
//         //         config.Save();
//         //     });
//         // yield return GetContentLabel("Levels - Spawn Inside");
//         // yield return GetStringField(
//         //     CustomConfig.SpawnInsideEntry.Value,
//         //     x =>
//         //     {
//         //         CustomConfig.SpawnInsideEntry.Value = x;
//         //         config.AssignFromSaved();
//         //         config.Save();
//         //     });
//         // yield return GetContentLabel("Levels - Spawn Outside");
//         // yield return GetStringField(
//         //     CustomConfig.SpawnOutsideEntry.Value,
//         //     x =>
//         //     {
//         //         CustomConfig.SpawnOutsideEntry.Value = x;
//         //         config.AssignFromSaved();
//         //         config.Save();
//         //     });
//         // yield return GetContentLabel("Levels - Spawn During Daytime");
//         // yield return GetStringField(
//         //     CustomConfig.SpawnDaytimeEntry.Value,
//         //     x =>
//         //     {
//         //         CustomConfig.SpawnDaytimeEntry.Value = x;
//         //         config.AssignFromSaved();
//         //         config.Save();
//         //     });
//     }
//
//     private static IEnumerable<MenuComponent> GenerateAISettings(CustomConfig config) {
//         yield return GetDefaultLabel("AI Settings");
//         yield return new DropdownComponent {
//             Text = "AI Type",
//             Enabled = true,
//             Options = Enum.GetValues(typeof(RollingGiantAiType))
//                 .Cast<RollingGiantAiType>()
//                 .Select(x => new TMP_Dropdown.OptionData(x.ToString()))
//                 .ToList(),
//             Value = new TMP_Dropdown.OptionData(config.AiType.ToString()),
//             OnValueChanged = (component, option) =>
//             {
//                 var index = component.Options.FindIndex(x => x.text == component.Value.text && x.image == component.Value.image);
//                 if (index == -1) {
//                     // display error
//                 }
//                 Plugin.Log.LogInfo($"AI Type: {option.text}");
//                 var value = (RollingGiantAiType)Enum.Parse(typeof(RollingGiantAiType), option.text);
//                 if (config.AiType == value) return;
//                 config.AiType = value;
//                 config.AssignFromSaved();
//                 config.Save();
//             }
//         };
//     }
//
//     private static IEnumerable<MenuComponent> GenerateAITypes(CustomConfig config) {
//         yield break;
//     }
//
//     private static IEnumerable<MenuComponent> GenerateHost(CustomConfig config) {
//         yield break;
//     }
//
//     private static LabelComponent GetDefaultLabel(string text) {
//         return new LabelComponent {
//             Text = text,
//         };
//     }
//     
//     private static LabelComponent GetContentLabel(string text) {
//         return new LabelComponent {
//             Text = text,
//             FontSize = 10
//         };
//     }
//
//     private static InputComponent GetIntField(int initialValue, Action<int> onChanged) {
//         // ? temp fix until initial state is fixed 
//         var firstAssign = false;
//         var input = new InputComponent {
//             Placeholder = "Enter a number",
//             Value = initialValue.ToString(),
//             OnValueChanged = (c, x) =>
//             {
//                 if (!firstAssign) {
//                     firstAssign = true;
//                     c.Value = initialValue.ToString(); // suffering
//                     return;
//                 }
//
//                 if (!int.TryParse(x, out var value)) {
//                     return;
//                 }
//
//                 value = Math.Max(0, value);
//                 onChanged(value);
//             }
//         };
//
//         return input;
//     }
//
//     private static InputComponent GetFloatField(float initialValue, Action<float> onChanged) {
//         var input = new InputComponent {
//             Placeholder = "Enter a number",
//             Value = initialValue.ToString(),
//             OnValueChanged = (c, x) =>
//             {
//                 if (!float.TryParse(x, out var value)) {
//                     return;
//                 }
//
//                 value = Math.Max(0, value);
//                 onChanged(value);
//             }
//         };
//
//         return input;
//     }
//
//     private static InputComponent GetStringField(string initialValue, Action<string> onChanged) {
//         var input = new InputComponent {
//             Placeholder = "Enter a string",
//             Value = initialValue,
//             OnValueChanged = (c, x) =>
//             {
//                 onChanged(x);
//             }
//         };
//
//         return input;
//     }
// }