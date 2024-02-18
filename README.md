# Rolling Giant

> Made by Andrew Burke

Adds the Rolling Giant as a new enemy type into Lethal Company. Sounds are included.

Features:

- Adds the Rolling Giant as a new enemy
- Adds a custom scrap poster for the Rolling Giant
- Rolling Giants can be scanned to read their own unique bestiary entry
- Multiple AI behaviours to choose from
- Can change the scale of the Rolling Giant between two values
- Can change the Rolling Giant's movement speed, wait durations, move durations, and more
- Rolling Giants have the option to rotate to face the player if they have been still for some time
- Can change the AI type of all Rolling Giants on the fly:
  - A hot key to reload the entire config file
  - Hotkeys to cycle between AI types

![There should be the picture... Something went wrong.](https://github.com/nomnomab/lc-rolling-giant/blob/b7ec500fe67ce588190dbb03c3a2a0baad42bfda/Images/promo.png?raw=true)

## Installation

Put the `/BepInEx/` folder inside your `/steamapps/common/Lethal Company/` folder after installing all the dependencies.

## Config

Generated after launching the game for the first time.

### General

- `GiantScaleInsideMin` - The minimum scale of the Rolling Giant's model inside
  - This changes how small the Giant can be
- `GiantScaleInsideMax` - The maximum scale of the Rolling Giant's model inside
  - This changes how big the Giant can be
- `GiantScaleOutsideMin` - The minimum scale of the Rolling Giant's model outside
  - This changes how small the Giant can be
- `GiantScaleOutsideMax` - The maximum scale of the Rolling Giant's model outside
  - This changes how big the Giant can be

### Spawn Conditions

These do not update when reloading the config in-game!

- `SpawnIn` - Levels that the Rolling Giant can spawn in, separated by their chances of spawning
  - Vanilla caps at 100, but you can go farther.
  - This chance is also a weight, not a percentage
  - Higher chance = higher chance to get picked
  - The names are what you see in the terminal
  - `Vow:45,March:45,Rend:54,Dine:65,Offense:45,Titan:65`
- `SpawnInOutsideChance` - The chance for the Rolling Giant to spawn outside in the levels from `SpawnIn`
- `SpawnInAny` - If the Rolling Giant can spawn on any level
- `SpawnInAnyChance` - The chance for the Rolling Giant to spawn in any level
- `SpawnInAnyOutsideChance` - The chance for the Rolling Giant to spawn outside in any level
- `CanSpawnInside` - If the Rolling Giant should spawn inside the dungeon
- `CanSpawnOutside` - If the Rolling Giant should spawn outside
- `DisableOutsideAtNight` - If the Rolling Giant will turn off if it is outside at night.
- `MaxPerLevel` - The maximum amount of Rolling Giant's that can spawn in a level
- `SpawnPosterIn` - Where the Rolling Giant poster scrap can spawn, separated by their chances of spawning
  - Vanilla caps at 100, but you can go farther.
  - This chance is also a weight, not a percentage
  - Higher chance = higher chance to get picked
  - The names are what you see in the terminal
  - `Vow:12,March:12,Rend:12,Dine:12,Offense:12,Titan:12`

### AI

- `AiType` - Type of AI the Rolling Giant uses
  - Putting multiple will randomly choose between them each time you land on a moon 
  - Coilhead - Move when the player is not looking at it
  - MoveWhenLooking - Move when the player is looking at it
  - RandomlyMoveWhileLooking - Randomly move while the player is looking at it
    - `WaitTimeMin` - The minimum duration in seconds that the Rolling Giant waits before moving again
    - `WaitTimeMax` - The maximum duration in seconds that the Rolling Giant waits before moving again
    - `RandomMoveTimeMin` - The minimum duration in seconds that the Rolling Giant moves toward the player
    - `RandomMoveTimeMax` - The maximum duration in seconds that the Rolling Giant moves toward the player
  - LookingTooLongKeepsAgro - If the player looks at it for too long it doesn't stop chasing
    - `LookTimeBeforeAgro` - How long the player can look at the Rolling Giant before it starts chasing.
  - FollowOnceAgro - Once the player is noticed, the Rolling Giant will follow the player constantly
  - OnceSeenAgroAfterTimer - Once the player sees the Rolling Giant, it will agro after a timer
    - `WaitTimeMin` - The minimum duration in seconds that the Rolling Giant waits before chasing the player
    - `WaitTimeMax` - The minimum duration in seconds that the Rolling Giant waits before chasing the player
  - All - Will select all of the ai types
- `AiTypeChangeOnHourInterval` - If the AI type should change every X hours. Set to 0 to disable
- `MoveSpeed` - Speed of the Rolling Giant's movement in m/s²
- `MoveAcceleration` - How long it takes the Rolling Giant to get to its movement speed in seconds
- `MoveDeceleration` - How long it takes the Rolling Giant to stop moving in seconds
- `RotateToLookAtPlayer` - If the Rolling Giant should rotate to face the player if it has been still for some time
- `DelayBeforeLookingAtPlayer` - The delay before the Rolling Giant looks at the player
- `LookAtPlayerDuration` - The duration the Rolling Giant takes to look at the player

### Host

These do not update when reloading the config in-game!

- `GotoPreviousAiTypeKey` - The key to go to the previous AI type
  - This uses Unity's New Input System's key-bind names
  - Defaults to `Keypad 7`
- `GotoNextAiTypeKey` - The key to go to the next AI type
  - This uses Unity's New Input System's key-bind names
  - Defaults to `Keypad 8`
- `ReloadConfigKey` - The key to reload the config. Does not update spawn conditions
  - This uses Unity's New Input System's key-bind names
  - Defaults to `Keypad 9`

### Building the project

#### Removing the local plugin package step

Remove the `PreBuild` step in the csproj, and replace the `PostBuild` step with

```cs
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="cd $(NetcodePatcherDir)&#xA;NetcodePatcher.dll $(TargetDir) deps/&#xA;xcopy /y /d &quot;$(TargetDir)$(TargetName).dll&quot; &quot;$(GameDir)\BepInEx\plugins\RollingGiant\&quot;&#xA;" />
</Target>
```

Also remove:

```cs
<ItemGroup>
  <Folder Include="plugin\BepInEx\plugins\RollingGiant\" />
</ItemGroup>
```

#### Initial steps

1. Open the .csproj
2. Change `<GameDir>` to where your game is installed
3. Change `<NetcodePatcherDir>` to where you have the [Unity Netcode Patcher](https://github.com/EvaisaDev/UnityNetcodePatcher) extracted

> Can also remove the enter netcode patcher step if you want to use the nuget version instead, but I haven't updated to that yet, so I haven't looked into it

4. Build

When built, it will patch the dll and then copy the dll to the game's plugin folder.

If you don't want the auto-copying to the game directory, then remove this from the `PostBuild` step:

```cs
xcopy /y /d &quot;$(TargetDir)$(TargetName).dll&quot; &quot;$(GameDir)\BepInEx\plugins\RollingGiant\&quot;&#xA;
```

## Changelog

## 2.5.2

- Fixed LLL spawning
- Fixed daytime variant's shader being missing (somehow)
- Fixed daytime variant, when disabled, not disabling audio

## 2.5.1

- Fixed timers that didn't reset when ai types changed hourly
- Fixed some ai types slowing down dramatically due to timers not resetting

## 2.5.0

- Added `AiTypeChangeOnHourInterval` to the config so the ai type can change every X amount of hours
- The `InverseCoilhead` ai type will now move towards a player if it can't see any of them, until it gets back into line of sight

## 2.4.4

- Added`@Headcraps` to the acknowledgments for making the original Rolling Giant model
  - I was given this by a friend so I wasn't aware that Headcraps initially made it, sorry about that!

## 2.4.3

- Fixes outside ai!

## 2.4.2

- Clients properly get targetted during the roaming phase

## 2.4.1

- Fixed an ownership issue that caused the Rolling Giant to spam errors
- Fixed an ownership issue that caused the Rolling Giant to not untarget from someone
- Fixed an ownership issue that caused the Rolling Giant to target people outside of the factory
- Removed capsule collider and added a box collider as it was causing issues with the Rolling Giant's attack
- Rolling Giants outside still don't work properly, but is getting worked on 🫡

## 2.4.0

- Fixed the Rolling Giant's bestiary entry breaking when scanning the outside versions of it
- Fixed the Rolling Giant not being able to get on the ship
- Fixed the latency teleportation of the Rolling Giant on clients; now moves smoothly across all clients
- Fixed ai path issues if the player jumped or got onto a high place. Now the Rolling Giant paths to the closest point to the player if they are inaccessable
- Fixed the Rolling Giant damaging the player when clipping into the ship and the player is in the ship. Now it won't damage the player in this case
- Fixed ai type syncing between clients
- Fixed various ai types by syncing shared variables between clients
- Fixed `LookingTooLongKeepsAgro` where the Rolling Giant wouldn't move while the timer is partially completed
- Fixed `LookingTooLongKeepsAgro` where the time would restart when it switched players, or lost the player
- Fixed `OnceSeenAgroAfterTimer` where the time would restart when it switched players, or lost the player 
- Fixed ai type timers resetting when the player jumped or went out of range
- Fixed the Rolling Giant's audio stuttering if it makes quick small movements to reach a location
- Improved the wandering speed so it will speed up to their max speed like their chasing state
- Rolling Giants past 1.2 size can no longer get onto the ship
- Rolling Giant scale now slightly affects their rolling audio pitch

## 2.3.0

- Added a config option to change the max amount of Rolling Giant's that can spawn in a level
- Changed the config option for AiType to accept multiple values
  - This allows for a random AI type to be picked at the start of each moon
  - AI type is now synced across clients with a NetworkVariable instead of with the config sync
- Added a new Rolling Giant model that is more optimized than the previous one
  - Thanks to `Krampus` for the help on that!
- The Rolling Giant now properly checks line of sight to start chasing the player
- Fixed a client bug that would complain about an invalid navmesh agent

## 2.2.1

- Fixed dead body shader being applied to all dead bodies and not only to the Rolling Giant player death type

## 2.2.0

- Added a config option to spawn the Rolling Giant on any level and a weight for that option

## 2.1.4

- Fixed client audio not playing due to agent speed mismatch

## 2.1.3

- Slightly increases the now too low default poster spawn weight

## 2.1.2

- Removed a multipler that was applied to the poster rarity for some reason

## 2.1.1

- Removed the soft dependency to LethalSettings, as it somehow broke through a try catch which broke the mod loading.
- Increased the default spawn weights slightly.
- Added a slight probability curve to the giant's spawning inside, outside, and outside during the day.
  - Inside will slightly more common at the start
  - Outside will be slightly more common at the start and a bit more common near the end
  - Outside during the day will be slightly more common at the start

## 2.1.0

- Renamed `SpawnInside` to `CanSpawnInside`, and is now just a toggle
- Renamed `SpawnOutside` to `CanSpawnOutside`, and is now just a toggle
- Renamed `SpawnDaytime` to `DisableOutsideAtNight` as it was too confusing for many people, and is now just a toggle
- Added extra notes to various config options to make them more clear
- Added the Rolling Giant to the in-game dev enemy spawn list
- Fixed the Rolling Giant's outside AI
- Gave the Rolling Giant, when it is a daytime type, a disabled state

## 2.0.1

- Removed logs from ai modes that spammed the console :(

## 2.0.0

- Removed Terminal API dependency
- Converted the Rolling Giant into a completely custom enemy that can be added to the spawn pools 
- Overhauled all AI behaviors
- Fixed many audio and networking sync issues
- All AI types will wander by default if all players get out of range
- Added a new player death type for when the Rolling Giant kills the player
- Removed wander settings
- Added config options:
  - `GotoPreviousAiTypeKey` - The key to go to the previous AI type.
    - This uses Unity's New Input System's key-bind names
  - `GotoNextAiTypeKey` - The key to go to the next AI type.
    - This uses Unity's New Input System's key-bind names
  - `ReloadConfigKey` - The key to reload the config. Does not update spawn conditions.
    - This uses Unity's New Input System's key-bind names
  - `SpawnIn` - Levels that the Rolling Giant can spawn in, separated by their chances of spawning
  - `SpawnInside` - If the Rolling Giant should spawn inside the dungeon
  - `SpawnDaytime` - If the Rolling Giant should spawn during the day
  - `SpawnOutside` - If the Rolling Giant should spawn outside
  - `MoveDeceleration` - How long it takes the Rolling Giant to stop moving in seconds
  - `SpawnPosterIn` - Where the Rolling Giant poster scrap can spawn, separated by their chances of spawning

## 1.2.0

- Removed unused LC_API dependency
- Added a config option to tell the Rolling Giant to wander again if the player goes past a certain distance
- Added a config option to change that distance between the player and the Rolling Giant
- Added a config option to change how long it takes the Rolling Giant to get to its movement speed
- Added a config option to change the Rolling Giant's visual scale between two values
- Added a config option to change the duration the player has to look at the Rolling Giant before agro for the LookingTooLongKeepsAgro AI
- Audio volume scales based on the Rolling Giant's speed up to a cap of 1.0
- Added new AI types for the Rolling Giant:
  - FollowOnceAgro = Once provoked, the Rolling Giant will follow the player constantly
  - OnceSeenAgroAfterTimer = Once the player sees the Rolling Giant, it will agro after a timer
- Fixed RandomlyMoveWhileLooking AI not taking into account player viewing for timers
- Fixed movement speed not applying to AI tick loop
- Overhauled all settings to allow for per-AI type settings
  - AI types are now grouped with the data they need
  - Previous settings are removed automatically

## 1.1.1

- Made the Rolling Giant rng utilize the map seed to make results less samey

## 1.1.0

- Multiple AI types for the Rolling Giant:
  - Coilhead = Coilhead AI
  - MoveWhenLooking = Move when player is looking at it
  - RandomlyMoveWhileLooking = Randomly move while the player is looking at it
  - LookingTooLongKeepsAgro = If the player looks at it for too long it doesn't stop chasing
- Can change the scale of the Rolling Giant's model
- Can change the Rolling Giant's movement speed and wait/move random durations
- Rolling Giants have the option to rotate to face the player if they have been still for some time
- Rolling Giant variant is now synced visually across clients

## 1.0.0

- Initial release

## Acknowledgments

- `Ayyobee` for a bunch of online testing and suggestions.
- `Krampus` for the help with the new Rolling Giant model.
- `@Headcraps` for the [original Rolling Giant model](https://rigmodels.com/model.php?view=The_Rolling_Giant_[Tov3_Accurate]_3d_model__50038756bcfa4e3082bee7211dbd22a0)!

<br/>

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/B0B6R2Z9U)