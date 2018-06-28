using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.InfiniteWorld
{
    public class WorldSectorVegetationSystem : ComponentSystem
    {
        // chunks that are visible, have heightmap and doesn't have VegetationComponent
        struct CreateEventsFilter
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<WorldSectorVegetationCreateEvent> events;
        }

        [Inject]
        CreateEventsFilter eventsFilter;

        [Inject]
        TerrainChunkAssetDataSystem dataSystem;

        RandomProvider randomGen = new RandomProvider(12345);
        EntityArchetype vegetationArchetype;
        Mesh testMesh;
        Material[] testMaterials;

        protected override void OnCreateManager(int capacity)
        {
            vegetationArchetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Shift), typeof(Transform), typeof(MeshRender), typeof(WorldSectorObject));

            var prefab = Resources.Load<GameObject>("Trees/Pines/Pine_005/Pine_005_01");
            var lod = prefab.transform.Find("pine_005_01_LOD0");
            testMesh = lod.GetComponent<MeshFilter>().sharedMesh;
            testMaterials = lod.GetComponent<MeshRenderer>().sharedMaterials;
        }

        protected override void OnUpdate()
        {
            for (int temp = 0; temp < eventsFilter.events.Length; ++temp)
            {
                var sector = eventsFilter.events[temp].sector;

                NativeArray<float> heightMap;
                if (dataSystem.GetHeightmap(new Sector(sector), out heightMap))
                {
                    // Just create 150 trees in random position inside a sector
                    for (int i = 0; i < 150; ++i)
                        CreateEntity(sector, heightMap);

                    PostUpdateCommands.DestroyEntity(eventsFilter.entities[temp]);
                }
            }
        }

        protected void CreateEntity(int2 sector, NativeArray<float> heightMap)
        {
            var rand = new int2((int)(UnityEngine.Random.value * WorldChunkConstants.ChunkSize), (int)(UnityEngine.Random.value * WorldChunkConstants.ChunkSize));
            Vector3 shift = new Vector3(rand.x, heightMap[(rand.y * WorldChunkConstants.ChunkSize + rand.x)] * WorldChunkConstants.TerrainHeightScale, rand.y);

            PostUpdateCommands.CreateEntity(vegetationArchetype);
            PostUpdateCommands.SetComponent(new Sector(sector));
            PostUpdateCommands.SetComponent(new Shift(shift));
            var meshRender = new MeshRender()
            {
                mesh = testMesh,
                materialCount = math.min(4, testMaterials.Length)
            };
            if (meshRender.materialCount > 0)
                meshRender.material0 = testMaterials[0];
            if (meshRender.materialCount > 1)
                meshRender.material1 = testMaterials[1];
            if (meshRender.materialCount > 2)
                meshRender.material2 = testMaterials[2];

            PostUpdateCommands.SetSharedComponent(meshRender);
        }
    }
}
