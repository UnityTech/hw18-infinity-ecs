using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class TerrainChunkGenerator
    {
        struct GenerateJob : IJobParallelFor
        {
            [ReadOnly]
            public Sector sector;

            [WriteOnly]
            public NativeArray<float> heightmap;

            public void Execute(int i)
            {
                var perlin = Mathf.PerlinNoise(i % WorldChunkConstants.ChunkSize, i / WorldChunkConstants.ChunkSize);
                heightmap[i] = perlin;
            }
        }

        struct Request
        {
            public JobHandle handle;
            public GenerateJob generateJob;
        }

        public const int InitialCapacity = 256;

        Dictionary<Sector, Request> requests;
        Dictionary<Sector, ComputeBuffer> data;

        public void Init()
        {
            requests = new Dictionary<Sector, Request>();
            data = new Dictionary<Sector, ComputeBuffer>(InitialCapacity);
        }

        public ComputeBuffer GetHeightmapBuffer(Sector sector)
        {
            ComputeBuffer ret;
            if (!data.TryGetValue(sector, out ret))
            {
                MakeRequest(sector);
                return null;
            }

            return ret;
        }

        void MakeRequest(Sector sector)
        {
            Request req;
            if (!requests.TryGetValue(sector, out req))
            {
                // Initiate generate buffer job
                req.generateJob = new GenerateJob()
                {
                    sector = sector,
                    heightmap = new NativeArray<float>(WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize, Allocator.Persistent)
                };
                req.handle = req.generateJob.Schedule(WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize, 64);

                requests.Add(sector, req);
                return;
            }

            // We have job running, but it isn't complete
            if (!req.handle.IsCompleted)
                return;

            req.handle.Complete();

            // Register data buffer and cleanup
            var buffer = new ComputeBuffer(req.generateJob.heightmap.Length, sizeof(float));
            buffer.SetData(req.generateJob.heightmap);

            data.Add(sector, buffer);

            req.generateJob.heightmap.Dispose();
            requests.Remove(sector);
        }
    }
}
