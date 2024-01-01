using Unity.Netcode;

namespace RollingGiant.Settings;

public struct SharedAiSettings: INetworkSerializable {
    public RollingGiantAiType aiType;
    
    public float moveSpeed;
    public float moveAcceleration;
    public float moveDeceleration;
    public bool rotateToLookAtPlayer;
    public float delayBeforeLookingAtPlayer;
    public float lookAtPlayerDuration;

    // public bool canWander;
    // public float chaseMaxDistance;
    
    public float waitTimeMin;
    public float waitTimeMax;
    public float randomMoveTimeMin;
    public float randomMoveTimeMax;
    
    public float lookTimeBeforeAgro;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref aiType);
        serializer.SerializeValue(ref moveSpeed);
        serializer.SerializeValue(ref moveAcceleration);
        serializer.SerializeValue(ref moveDeceleration);
        serializer.SerializeValue(ref rotateToLookAtPlayer);
        serializer.SerializeValue(ref delayBeforeLookingAtPlayer);
        serializer.SerializeValue(ref lookAtPlayerDuration);
        // serializer.SerializeValue(ref canWander);
        // serializer.SerializeValue(ref chaseMaxDistance);
        serializer.SerializeValue(ref waitTimeMin);
        serializer.SerializeValue(ref waitTimeMax);
        serializer.SerializeValue(ref randomMoveTimeMin);
        serializer.SerializeValue(ref randomMoveTimeMax);
        serializer.SerializeValue(ref lookTimeBeforeAgro);
    }

    public override string ToString() {
        // return $"aiType: {aiType}, moveSpeed: {moveSpeed}, moveAcceleration: {moveAcceleration}, moveDeceleration: {moveDeceleration}, rotateToLookAtPlayer: {rotateToLookAtPlayer}, delayBeforeLookingAtPlayer: {delayBeforeLookingAtPlayer}, lookAtPlayerDuration: {lookAtPlayerDuration}, canWander: {canWander}, chaseMaxDistance: {chaseMaxDistance}, waitTimeMin: {waitTimeMin}, waitTimeMax: {waitTimeMax}, randomMoveTimeMin: {randomMoveTimeMin}, randomMoveTimeMax: {randomMoveTimeMax}, lookTimeBeforeAgro: {lookTimeBeforeAgro}";
        return $"aiType: {aiType}, moveSpeed: {moveSpeed}, moveAcceleration: {moveAcceleration}, moveDeceleration: {moveDeceleration}, rotateToLookAtPlayer: {rotateToLookAtPlayer}, delayBeforeLookingAtPlayer: {delayBeforeLookingAtPlayer}, lookAtPlayerDuration: {lookAtPlayerDuration}, waitTimeMin: {waitTimeMin}, waitTimeMax: {waitTimeMax}, randomMoveTimeMin: {randomMoveTimeMin}, randomMoveTimeMax: {randomMoveTimeMax}, lookTimeBeforeAgro: {lookTimeBeforeAgro}";
    }
}