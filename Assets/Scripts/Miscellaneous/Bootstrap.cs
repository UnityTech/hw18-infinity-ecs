using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class Bootstrap
    {
        public static EntityArchetype terrainArchetype;
        public static TerrainChunkGenerator terrainGenerator;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();

            terrainGenerator = new TerrainChunkGenerator();
            terrainGenerator.Init();

            terrainArchetype = entityManager.CreateArchetype(typeof(Sector), typeof(LOD), typeof(TerrainChunkCPUData), typeof(Transform));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeWithScene()
        {
            World.Active.GetOrCreateManager<CameraControlSystem>().Init(Camera.main);
            World.Active.GetOrCreateManager<TerrainChunkRenderSystem>().Init(terrainGenerator);

            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            var entity = entityManager.CreateEntity(terrainArchetype);
            entityManager.SetComponentData<Sector>(entity, new Sector() { value = new int2(0, 0) });

            var entity2 = entityManager.CreateEntity(terrainArchetype);
            entityManager.SetComponentData<Sector>(entity2, new Sector() { value = new int2(1, 0) });
        }
    }
}
