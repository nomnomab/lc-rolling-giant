using GameNetcodeStuff;
using UnityEngine;

namespace RollingGiant; 

public class RollingGiantAI {
    public PlayerControllerB PreviousTarget;
    public float CurrentChaseSpeed = 14.5f;
    public float CurrentAnimSpeed = 1f;
    public bool WasOwnerLastFrame;
    public float StopAndGoMinimumInterval;
    public float TimeSinceHittingPlayer;
    public float CheckLineOfSightInterval;
    public bool HasEnteredChaseMode;
    public bool StoppingMovement;
    public bool HasStopped;
    public bool IsPlayingSound;
}