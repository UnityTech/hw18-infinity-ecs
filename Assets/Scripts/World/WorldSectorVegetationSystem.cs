﻿using System;
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
        Material testMaterial;

        protected override void OnCreateManager(int capacity)
        {
            vegetationArchetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Shift), typeof(Transform), typeof(MeshRender), typeof(WorldSectorObject));

            var test = Resources.Load<GameObject>("Art/Tree 01");
            testMesh = test.GetComponent<MeshFilter>().sharedMesh;
            testMaterial = test.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial;
        }

        protected override void OnUpdate()
        {
            for (int temp = 0; temp < eventsFilter.events.Length; ++temp)
            {
                var sector = eventsFilter.events[temp].sector;
                if (dataSystem.IsHeightmapReady(sector))
                {
                    var heightMap = dataSystem.GetChunkHeightmap(sector);
                    // Just create 150 trees in random position inside a sector
                    for (int i = 0; i < 150; ++i)
                    {
                        CreateEntity(sector, heightMap);
                    }

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
            PostUpdateCommands.SetSharedComponent(new MeshRender() { mesh = testMesh, material = testMaterial });
        }
    }
}
