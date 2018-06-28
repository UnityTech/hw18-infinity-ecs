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
            vegetationArchetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Shift), typeof(Rotation), typeof(Scale), typeof(Transform), typeof(MeshRender), typeof(WorldSectorObject));

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
                randomGen.seed = (uint)((sector.x + 1023) * 1048575 + sector.y);

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
            float posX = randomGen.Uniform(0, WorldChunkConstants.ChunkSize - 1);
            float posZ = randomGen.Uniform(0, WorldChunkConstants.ChunkSize - 1);
            int index = ((int)(posZ) * WorldChunkConstants.ChunkSize + (int)(posX));

            float3 shift = new float3(posX, heightMap[index] * WorldChunkConstants.TerrainHeightScale, posZ);

            float rx = randomGen.Uniform(Mathf.PI * 0.1f);
            float ry = randomGen.Uniform(Mathf.PI);
            float rz = randomGen.Uniform(Mathf.PI * 0.1f);

            float3 rotation = new float3(rx, ry, rz);

            float3 s, c;
            math.sincos(0.5f * rotation, out s, out c);
            var q = new quaternion(new float4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * new float4(c.xyz, s.x) * new float4(-1.0f, 1.0f, -1.0f, 1.0f));

            float sx = randomGen.Uniform(0.4f) + 0.8f;
            float sy = randomGen.Uniform(0.4f) + 0.8f;
            float sz = randomGen.Uniform(0.4f) + 0.8f;
            float3 scale = new float3(sx, sy, sz);

            PostUpdateCommands.CreateEntity(vegetationArchetype);
            PostUpdateCommands.SetComponent(new Sector(sector));
            PostUpdateCommands.SetComponent(new Shift(shift));
            PostUpdateCommands.SetComponent(new Rotation(q));
            PostUpdateCommands.SetComponent(new Scale(scale));
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
