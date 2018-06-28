using Unity.Entities;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeWithScene()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            var cameraSystem = World.Active.GetOrCreateManager<CameraSystem>();

            var camera = entityManager.CreateEntity(cameraSystem.cameraArchetype);
            entityManager.AddComponent(camera, typeof(CameraMainTag));

            World.Active.GetOrCreateManager<DebugOutput>().Init();
        }
    }
}
