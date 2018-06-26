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
            [WriteOnly] public NativeArray<Color> Heightmap;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex, c = startIndex + count; i < c; ++i)
                {
                    var luma = Mathf.PerlinNoise(i % WorldChunkConstants.ChunkSize, i / WorldChunkConstants.ChunkSize);
                    Heightmap[i] = new Color(luma, luma, luma, 1);
                }
            }
        }

        struct TriggeredSectors
        {
            [ReadOnly]
            public ComponentDataArray<WorldChunkGeneratorTrigger> Triggers;
            [ReadOnly]
            public ComponentDataArray<Sector> Sectors;
        }

        struct DataToUploadOnGPU
        {
            public JobHandle Handle;
            public Sector Sector;
        }

        [Inject] TriggeredSectors m_TriggeredSectors;
        [Inject] TerrainChunkAssetDataSystem m_TerrainChunkAssetDataSystem;

        List<DataToUploadOnGPU> m_DataToUploadOnGPU = new List<DataToUploadOnGPU>();

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            // Upload to GPU datas that are ready
            for (int i = 0; i < m_DataToUploadOnGPU.Count; ++i)
            {
                var data = m_DataToUploadOnGPU[i];
                var heightmap = m_TerrainChunkAssetDataSystem.GetChunkHeightmap(data.Sector);
                var heightmapTex = m_TerrainChunkAssetDataSystem.GetChunkHeightmapTex(data.Sector);
                heightmapTex.SetPixels(heightmap.ToArray());
                heightmapTex.Apply();
            }
            m_DataToUploadOnGPU.Clear();

            // Update sectors
            if (m_TriggeredSectors.Sectors.Length > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(m_TriggeredSectors.Sectors.Length, Allocator.TempJob);
                for (int i = 0, c = m_TriggeredSectors.Sectors.Length; i < c; ++i)
                {
                    JobHandle thisChunkJob = dependsOn;

                    {
                        var job = new GenerateHeightmapJob
                        {
                            Sector = m_TriggeredSectors.Sectors[i],
                            Heightmap = m_TerrainChunkAssetDataSystem.GetChunkHeightmap(m_TriggeredSectors.Sectors[i])
                        };

                        thisChunkJob = job.ScheduleBatch(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize / (8 * 8),
                            dependsOn
                        );
                    }

                    m_DataToUploadOnGPU.Add(new DataToUploadOnGPU
                    {
                        Handle = thisChunkJob,
                        Sector = m_TriggeredSectors.Sectors[i]
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
