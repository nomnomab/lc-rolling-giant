using BepInEx.Configuration;

namespace RollingGiant.Settings; 

public class AiSettings {
    public const string AiTypeDescription = "The AI type of the Rolling Giant.\nCoilhead = Coilhead AI\nMoveWhenLooking = Move when player is looking at it\nRandomlyMoveWhileLooking = Randomly move while the player is looking at it\nLookingTooLongKeepsAgro = If the player looks at it for too long it doesn't stop chasing\nFollowOnceAgro = Once provoked, the Rolling Giant will follow the player constantly";
    
    public ConfigEntry<RollingGiantAiType> AiType;
    
    public AiSettings(ConfigFile configFile) {
        AiType = configFile.Bind("AI", "AiType", RollingGiantAiType.RandomlyMoveWhileLooking, AiTypeDescription);
    }
}