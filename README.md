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
- `GiantScale` - Scale of the Rolling Giant's model
- `RotateToLookAtPlayer` - If the Rolling Giant should rotate to face the player if it has been still for some time
- `DelayBeforeLookingAtPlayer` - The delay before the Rolling Giant looks at the player
- `LookAtPlayerDuration` - The duration the Rolling Giant takes to look at the player

### AI

- `AiType` - Type of AI the Rolling Giant uses
  - Coilhead - Coilhead AI
  - MoveWhenLooking - Move when player is looking at it
  - RandomlyMoveWhileLooking - Randomly move while the player is looking at it
  - LookingTooLongKeepsAgro - If the player looks at it for too long it doesn't stop chasing
- `AiMoveSpeed` - Speed of the Rolling Giant's movement
- `AiWaitTimeMin` - The minimum time the Rolling Giant waits before moving again
- `AiWaitTimeMax` - The maximum time the Rolling Giant waits before moving again
- `AiRandomMoveTimeMin` - The minimum time the Rolling Giant moves toward the player
- `AiRandomMoveTimeMax` - The maximum time the Rolling Giant moves toward the player

## Changelog

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
