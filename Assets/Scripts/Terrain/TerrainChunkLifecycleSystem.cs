using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public class TerrainChunkLifecycleSystem : ComponentSystem
    {
        const int VISIBILITY = 4;
        const int GRID_WIDTH = VISIBILITY * 2 + 1;
        const int GRID_SIZE = GRID_WIDTH * GRID_WIDTH;
        
        struct ChunkGroup
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<LOD> lods;
            [ReadOnly]
            public SubtractiveComponent<Shift> shifts;
        }

        [Inject]
        ChunkGroup chunksGroup;
        [Inject]
        TerrainChunkAssetDataSystem chunkAssetData;

        EntityArchetype archetype;

        [Inject]
        CameraSystem camera;

        protected override void OnCreateManager(int capacity)
        {
            archetype = EntityManager.CreateArchetype(typeof(Sector), typeof(LOD), typeof(Transform), typeof(TerrainChunkGeneratorTrigger));
        }

        protected unsafe override void OnUpdate()
        {
            var cameraSector = EntityManager.GetComponentData<Sector>(camera.main).value;

            var sectors = chunksGroup.sectors;
            var entities = chunksGroup.entities;

            int2 distOffset = new int2(VISIBILITY, VISIBILITY);
            int2 baseSector = cameraSector - distOffset;

            var grid = stackalloc uint[GRID_SIZE];
            for (int i = 0; i < GRID_SIZE; ++i)
                grid[i] = 0;

            for (int i = 0; i < sectors.Length; ++i)
            {
                int2 sector = sectors[i].value;
                int2 dist = sector - baseSector;
                
                if (dist.x < GRID_WIDTH && dist.y < GRID_WIDTH && dist.x >= 0 && dist.y >= 0)
                    grid[dist.y * GRID_WIDTH + dist.x] = 1;
                else
                {
                    chunkAssetData.DisposeSector(sectors[i]);
                    PostUpdateCommands.DestroyEntity(entities[i]);
                }
            }

            for (int j = 0; j < GRID_WIDTH; ++j)
            {
                for (int i = 0; i < GRID_WIDTH; ++i)
                {
                    if (grid[j * GRID_WIDTH + i] == 0)
                    {
                        var entity = EntityManager.CreateEntity(archetype);
                        EntityManager.SetComponentData(entity, new Sector(baseSector, i, j));
                        EntityManager.SetComponentData(entity, new LOD(0));
                        EntityManager.SetComponentData(entity, new Transform(float4x4.identity));
                    }
                }
            }
        }
    }
}
