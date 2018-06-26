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
        struct GenerateChunkDataJob : IJobParallelForBatch
        {
            [ReadOnly] public Sector Sector;
            [WriteOnly] public NativeArray<float> Heightmap;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex, c = startIndex + count; i < c; ++i)
                    Heightmap[i] = Mathf.PerlinNoise(i % WorldChunkConstants.ChunkSize, i / WorldChunkConstants.ChunkSize);
            }
        }

        struct TriggeredSectors
        {
            [ReadOnly]
            public ComponentDataArray<WorldChunkGeneratorTrigger> Triggers;
            [ReadOnly]
            public ComponentDataArray<Sector> Sectors;
        }

        [Inject] TriggeredSectors m_TriggeredSectors;

        struct CPUData
        {
            public NativeArray<float> Heightmap;
            public Texture2D HeightmapTex;
        }

        NativeHashMap<int2, int> m_CPUDataIndexBySector;
        NativeArray<CPUData> m_CPUDatas;

        bool MustUpdateSector(
            Sector sector,
            WorldChunkGeneratorTrigger trigger
        )
        {
            return true;
        }

        struct UpdateRequest
        {
            public Sector Sector;
        }

        protected override void OnCreateManager(int capacity)
        {
            m_CPUDataIndexBySector = new NativeHashMap<int2, int>(WorldChunkConstants.ChunkCapacity, Allocator.Persistent);
            m_CPUDatas = new NativeArray<CPUData>(WorldChunkConstants.ChunkCapacity, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            m_CPUDataIndexBySector.Dispose();
            m_CPUDatas.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            if (m_TriggeredSectors.Sectors.Length > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(m_TriggeredSectors.Sectors.Length, Allocator.TempJob);
                for (int i = 0, c = m_TriggeredSectors.Sectors.Length; i < c; ++i)
                {
                    m_CPUDataIndexBySector.TryGetValue(m_TriggeredSectors.Sectors[i].value, out int cpuDataIndex);
                    var job = new GenerateChunkDataJob
                    {
                        Sector = m_TriggeredSectors.Sectors[i],
                        Heightmap = m_CPUDatas[cpuDataIndex].Heightmap
                    };

                    jobHandles[i] = job.ScheduleBatch(
                        WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize / (8 * 8),
                        dependsOn
                    );
                }
                dependsOn = JobHandle.CombineDependencies(jobHandles);
                jobHandles.Dispose();
            }

            return dependsOn;
        }
    }
}
