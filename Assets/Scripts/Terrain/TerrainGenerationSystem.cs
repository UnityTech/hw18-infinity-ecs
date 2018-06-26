using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public unsafe class TerrainGenerationSystem : JobComponentSystem
    {
        struct GenerateHeightmapJob : IJobParallelForBatch
        {
            [ReadOnly] public Sector Sector;
            [WriteOnly] public NativeArray<float> Heightmap;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex, c = startIndex + count; i < c; ++i)
                {
                    var luma = Mathf.PerlinNoise(i % WorldChunkConstants.ChunkSize, i / WorldChunkConstants.ChunkSize);
                    Heightmap[i] = luma;
                }
            }
        }

        struct TriggeredSectors
        {
            [ReadOnly]
            public EntityArray Entities;
            [ReadOnly]
            public ComponentDataArray<TerrainChunkGeneratorTrigger> Triggers;
            [ReadOnly]
            public ComponentDataArray<Sector> Sectors;
            public SubtractiveComponent<TerrainChunkHasHeightmap> NotHasHeightmap;
            public SubtractiveComponent<TerrainChunkIsHeightmapBakingComponent> NotIsBakingHeightmap;
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

                    m_DataToUploadOnGPU.RemoveAt(i);
                }
            }

            // Update sectors
            if (m_TriggeredSectors.Sectors.Length > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(m_TriggeredSectors.Sectors.Length, Allocator.TempJob);
                for (int i = 0, c = m_TriggeredSectors.Sectors.Length; i < c; ++i)
                {
                    var sector = m_TriggeredSectors.Sectors[i];
                    var heightmap = m_TerrainChunkAssetDataSystem.GetChunkHeightmap(sector);
                    JobHandle thisChunkJob = dependsOn;

                    {
                        var job = new GenerateHeightmapJob
                        {
                            Sector = sector,
                            Heightmap = heightmap
                        };

                        thisChunkJob = job.ScheduleBatch(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize / (8 * 8),
                            dependsOn
                        );
                    }

                    cmd.AddComponent(m_TriggeredSectors.Entities[i], new TerrainChunkIsHeightmapBakingComponent());

                    m_DataToUploadOnGPU.Add(new DataToUploadOnGPU
                    {
                        Handle = thisChunkJob,
                        Sector = sector,
                        Entity = m_TriggeredSectors.Entities[i]
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
