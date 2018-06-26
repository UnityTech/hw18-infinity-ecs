using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class Bootstrap
    {
        public static EntityArchetype terrainArchetype;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeWithScene()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            World.Active.GetOrCreateManager<TerrainChunkAssetDataSystem>(); // Instantiate only
            World.Active.GetOrCreateManager<TerrainChunkRenderSystem>();

            terrainArchetype = entityManager.CreateArchetype(typeof(Sector), typeof(LOD), typeof(Transform));

            World.Active.GetOrCreateManager<CameraControlSystem>().Init(Camera.main);
            World.Active.GetOrCreateManager<TerrainChunkLifecycleSystem>().Init(Camera.main);

            var entity = entityManager.CreateEntity(terrainArchetype);
            entityManager.SetComponentData(entity, new Sector() { value = new int2(0, 0) });

            var entity2 = entityManager.CreateEntity(terrainArchetype);
            entityManager.SetComponentData(entity2, new Sector() { value = new int2(1, 0) });
        }
    }
}
