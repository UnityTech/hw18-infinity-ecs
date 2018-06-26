using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public unsafe class TerrainGenerationSystem : JobComponentSystem
    {
        struct GenerateHeightmapJob : IJobParallelFor
        {
            [ReadOnly] public Sector Sector;
            [WriteOnly] public NativeArray<float> Heightmap;

            public void Execute(int i)
            {
                var x = i / WorldChunkConstants.ChunkSize;
                var y = i % WorldChunkConstants.ChunkSize; 
                var luma = noise.snoise(
                    new float2(
                        x / (float)(WorldChunkConstants.ChunkSize - 1),
                        y / (float)(WorldChunkConstants.ChunkSize - 1)
                    )
                    + Sector.value
                );
                Heightmap[i] = luma;
            }
        }

        struct GenerateNormalmapJob : IJobParallelForBatch
        {
            [ReadOnly] public Sector Sector;
            [WriteOnly] public NativeArray<float4> Normalmap;

            public void Execute(int startIndex, int count)
            {

                // Calcul normal map
            }
        }

        struct TriggeredSectors
        {
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public ComponentDataArray<TerrainChunkGeneratorTrigger> Triggers;
            [ReadOnly] public ComponentDataArray<Sector> Sectors;
            [ReadOnly] public SubtractiveComponent<TerrainChunkHasHeightmap> NotHasHeightmap;
            [ReadOnly] public SubtractiveComponent<TerrainChunkIsHeightmapBakingComponent> NotIsBakingHeightmap;
            public SubtractiveComponent<TerrainChunkHasNormalmap> NotHasNormalmap;
            public SubtractiveComponent<TerrainChunkIsNormalmapBakingComponent> NotIsBakingNormalmap;
        }

        struct DataToUploadOnGPU
        {
            public JobHandle Handle;
            public Sector Sector;
            public Entity Entity;
        }

        class EntityBarrier : BarrierSystem
        { }

        [Inject] TriggeredSectors m_TriggeredSectors;
        [Inject] TerrainChunkAssetDataSystem m_TerrainChunkAssetDataSystem;
        [Inject] EntityBarrier m_EntityBarrier;

        List<DataToUploadOnGPU> m_DataToUploadOnGPU = new List<DataToUploadOnGPU>();

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            var cmd = m_EntityBarrier.CreateCommandBuffer();
            // Upload to GPU datas that are ready
            for (int i = m_DataToUploadOnGPU.Count - 1; i >= 0; --i)
            {
                var data = m_DataToUploadOnGPU[i];
                if (data.Handle.IsCompleted)
                {
                    data.Handle.Complete();

                    var heightmap = m_TerrainChunkAssetDataSystem.GetChunkHeightmap(data.Sector);
                    var heightmapTex = m_TerrainChunkAssetDataSystem.GetChunkHeightmapTex(data.Sector);
                    heightmapTex.LoadRawTextureData(heightmap);
                    heightmapTex.Apply();
                    cmd.RemoveComponent<TerrainChunkIsHeightmapBakingComponent>(data.Entity);
                    cmd.AddComponent(data.Entity, new TerrainChunkHasHeightmap());

                    var normalmap = m_TerrainChunkAssetDataSystem.GetChunkNormalmap(data.Sector);
                    var normalmapTex = m_TerrainChunkAssetDataSystem.GetChunkNormalmapTex(data.Sector);
                    normalmapTex.LoadRawTextureData(normalmap);
                    normalmapTex.Apply();
                    cmd.RemoveComponent<TerrainChunkIsNormalmapBakingComponent>(data.Entity);
                    cmd.AddComponent(data.Entity, new TerrainChunkHasNormalmap());

                    m_DataToUploadOnGPU.RemoveAt(i);
                }
            }

            // Update sectors
            if (m_TriggeredSectors.Sectors.Length > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(m_TriggeredSectors.Sectors.Length, Allocator.TempJob);
                for (int i = 0, c = m_TriggeredSectors.Sectors.Length; i < c; ++i)
                {
                    var entity = m_TriggeredSectors.Entities[i];
                    var sector = m_TriggeredSectors.Sectors[i];
                    var heightmap = m_TerrainChunkAssetDataSystem.GetChunkHeightmap(sector);
                    var normalmap = m_TerrainChunkAssetDataSystem.GetChunkNormalmap(sector);
                    JobHandle thisChunkJob = dependsOn;

                    {
                        var job = new GenerateHeightmapJob
                        {
                            Sector = sector,
                            Heightmap = heightmap
                        };

                        thisChunkJob = job.Schedule(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            64,
                            dependsOn
                        );

                        var job2 = new GenerateNormalmapJob
                        {
                            Sector = sector,
                            Normalmap = normalmap
                        };

                        thisChunkJob = job2.ScheduleBatch(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            1,
                            thisChunkJob
                        );
                    }

                    cmd.AddComponent(entity, new TerrainChunkIsHeightmapBakingComponent());
                    cmd.AddComponent(m_TriggeredSectors.Entities[i], new TerrainChunkIsNormalmapBakingComponent());

                    m_DataToUploadOnGPU.Add(new DataToUploadOnGPU
                    {
                        Handle = thisChunkJob,
                        Sector = sector,
                        Entity = entity
                    });

                    jobHandles[i] = thisChunkJob;
                }
                dependsOn = JobHandle.CombineDependencies(jobHandles);
                jobHandles.Dispose();
            }

            return dependsOn;
        }
    }
}
