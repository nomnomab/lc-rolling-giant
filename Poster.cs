using UnityEngine;

namespace RollingGiant; 

public class Poster: PhysicsProp {
    public void Init() {
        grabbable = true;
        itemProperties = Plugin.PosterItem;
        isInFactory = true;
        mainObjectRenderer = GetComponent<MeshRenderer>();
        grabbableToEnemies = true;
    }
}