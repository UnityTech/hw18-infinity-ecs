using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeWithScene()
        {
            World.Active.GetOrCreateManager<CameraControlSystem>().Init(Camera.main);
            World.Active.GetOrCreateManager<TerrainChunkLifecycleSystem>().Init(Camera.main);
        }
    }
}
