# Rolling Giant

> Made by Andrew Burke

Adds the Rolling Giant as a Coilhead variant into Lethal Company. Sounds are included.

Features:

- Adds a new Coilhead variant into the game with a configurable chance of spawning
- Coilheads and Rolling Giants can co-exist
- Rolling Giants can be scanned to read their own unique bestiary entry
- Rolling Giants have their own scan id so it doesn't conflict with the existing Coilheads
- Multiple AI behaviours to choose from
- Can change the scale of the Rolling Giant's model
- Can change the Rolling Giant's movement speed and wait/move random durations
- Rolling Giants have the option to rotate to face the player if they have been still for some time

![There should be the picture... Something went wrong.](./Images/image0.png)
![There should be the picture... Something went wrong.](./Images/image1.png)

## Installation

Put the `/BepInEx/` folder inside your `/steamapps/common/Lethal Company/` folder after installing all the dependencies.

## Config

Generated after launching the game for the first time.

### General

- `ChanceForGiant` = 0.0-1.0 - Chance for a Rolling Giant to spawn. Higher means more chances for a Rolling Giant
- `GiantScaleMin` - The minimum scale of the Rolling Giant's model
- `GiantScaleMax` - The maximum scale of the Rolling Giant's model

### AI

- `AiType` - Type of AI the Rolling Giant uses
  - Coilhead - Coilhead AI
  - MoveWhenLooking - Move when player is looking at it
  - RandomlyMoveWhileLooking - Randomly move while the player is looking at it
    - `WaitTimeMin` - The minimum duration the Rolling Giant waits before moving again
    - `WaitTimeMax` - The maximum duration the Rolling Giant waits before moving again
    - `RandomMoveTimeMin` - The minimum duration the Rolling Giant moves toward the player
    - `RandomMoveTimeMax` - The maximum duration the Rolling Giant moves toward the player
  - LookingTooLongKeepsAgro - If the player looks at it for too long it doesn't stop chasing
    - `LookTimeBeforeAgro` - How long the player can look at the Rolling Giant before it starts chasing.
  - FollowOnceAgro - Once provoked, the Rolling Giant will follow the player constantly
  - OnceSeenAgroAfterTimer - Once the player sees the Rolling Giant, it will agro after a timer
    - `WaitTimeMin` - The minimum duration the Rolling Giant waits before chasing the player
    - `WaitTimeMax` - The minimum duration the Rolling Giant waits before chasing the player

- Shared settings
  - `AiMoveSpeed` - Speed of the Rolling Giant's movement
  - `AiStartMoveDuration` - How long it takes the Rolling Giant to get to its movement speed
  - `RotateToLookAtPlayer` - If the Rolling Giant should rotate to face the player if it has been still for some time
  - `DelayBeforeLookingAtPlayer` - The delay before the Rolling Giant looks at the player
  - `LookAtPlayerDuration` - The duration the Rolling Giant takes to look at the player

- Wandering settings
  - `CanWander` - If the Rolling Giant goes back to wandering after the player gets far enough away from it
  - `ChaseMaxDistance` - The distance between the Rolling Giant and the player before it stops chasing and goes back to wandering

## Changelog

## 1.2.0

- Removed unused LC_API dependency
- Added a config option to tell the Rolling Giant to wander again if the player goes past a certain distance
- Added a config option to change that distance between the player and the Rolling Giant
- Added a config option to change how long it takes the Rolling Giant to get to its movement speed
- Added a config option to change the Rolling Giant's visual scale between two values
- Added a config option to change the duration the player has to look at the Rolling Giant before agro for the LookingTooLongKeepsAgro AI
- Added new AI types for the Rolling Giant:
  - FollowOnceAgro = Once provoked, the Rolling Giant will follow the player constantly
  - OnceSeenAgroAfterTimer = Once the player sees the Rolling Giant, it will agro after a timer
- Fixed RandomlyMoveWhileLooking AI not taking into account player viewing for timers
- Fixed movement speed not applying to AI tick loop
- Overhauled all settings to allow for per-AI type settings
  - AI types are now grouped with the data they need

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

Initial release

## Send me a Coffee!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/B0B6R2Z9U)