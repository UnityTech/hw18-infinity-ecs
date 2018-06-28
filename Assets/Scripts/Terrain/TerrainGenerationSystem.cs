using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.InfiniteWorld
{
    public static class noisex
    {
        public static float fbm(
            float2 sector,
            float2 xy,
            int octaves, 
            float multiplier, 
            float sectorScale, 
            float persistence
        )
        {
            float value = 0.0f;
            for (int j = 0; j < octaves; ++j)
            {
                value += noise.snoise((xy + sector) * sectorScale) * multiplier;

                sectorScale *= 2.0f;
                multiplier *= persistence;
            }

            return value;
        }

        public static float turb(
            float2 sector,
            float2 xy,
            int octaves,
            float multiplier,
            float sectorScale,
            float persistence
        )
        {
            float value = 0.0f;
            for (int j = 0; j < octaves; ++j)
            {
                value += math.abs(noise.snoise((xy + sector) * sectorScale)) * multiplier;

                sectorScale *= 2.0f;
                multiplier *= persistence;
            }

            return value;
        }

        public static float ridge(
            float2 sector,
            float2 xy,
            int octaves,
            float multiplier,
            float sectorScale,
            float persistence,
            float offset
        )
        {
            float value = 0.0f;
            for (int j = 0; j < octaves; ++j)
            {
                var n = math.abs(noise.snoise((xy + sector) * sectorScale)) * multiplier;
                n = offset - n;
                n *= n;

                value += n;

                sectorScale *= 2.0f;
                multiplier *= persistence;
            }

            return value;
        }

        public static float clip01(float value, float threshold)
        {
            return math.clamp(value - threshold, 0.0f, 1.0f) / (1.0f - threshold);
        }
    }

    [AlwaysUpdateSystem]
    public unsafe class TerrainGenerationSystem : JobComponentSystem
    {
        static float CalculateHeight(Sector sector, int i)
        {
            var y = i / WorldChunkConstants.ChunkSize;
            var x = i % WorldChunkConstants.ChunkSize;

            const float invScale = 1.0f / (WorldChunkConstants.ChunkSize - 1);

            float2 uv = new float2(x, y) * invScale;
            float2 position = uv + sector.value;

            // Base height
            float baseHeight = noisex.fbm(sector.value, uv, 4, 0.8f, 0.3f, 0.4f) * 0.2f + 0.5f;

            // Mountains
            float mountainSectorScale = 0.3f;
            float mountainBase = noisex.ridge(sector.value, uv, 3, 0.7f, mountainSectorScale, 0.2f, 0.56f);
            float mountainBaseWeight = noise.snoise(position * 0.2f) * 0.5f + 0.5f;
            float mountainBaseWeighted = mountainBase * mountainBaseWeight;
            float mountainWeight = noisex.clip01(mountainBaseWeighted, 0.2f);
            mountainWeight = noisex.clip01(mountainWeight, -0.6f);
            mountainWeight *= mountainWeight * mountainWeight;
            mountainWeight *= 4.0f;
            mountainWeight = math.clamp(mountainWeight, 0.0f, 1.0f);

            float mountainDetails = noisex.ridge(sector.value, uv, 5, 0.6f, mountainSectorScale, 0.5f, 0.45f);

            float height = mountainDetails * mountainWeight * 3;

            return height;
        }

        struct GenerateHeightmapJob : IJobParallelFor
        {
            [ReadOnly] public Sector Sector;
            [WriteOnly] public NativeArray<float> Heightmap;

            public void Execute(int i)
            {
                Heightmap[i] = CalculateHeight(Sector, i);
            }
        }

        struct GenerateNormalmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> Heightmap;
            [WriteOnly] public NativeArray<byte4> Normalmap;

            public void Execute(int i)
            {

                // Calcul normal map
                var x = i / WorldChunkConstants.ChunkSize;
                var y = i % WorldChunkConstants.ChunkSize;

                var x_1 = math.clamp(x - 1, 0, WorldChunkConstants.ChunkSize - 1);
                var y_1 = math.clamp(y - 1, 0, WorldChunkConstants.ChunkSize - 1);
                var x1 = math.clamp(x + 1, 0, WorldChunkConstants.ChunkSize - 1);
                var y1 = math.clamp(y + 1, 0, WorldChunkConstants.ChunkSize - 1);

                var xLeft = x_1 + y * WorldChunkConstants.ChunkSize;
                var xRight = x1 + y * WorldChunkConstants.ChunkSize;
                var yUp = x + y_1 * WorldChunkConstants.ChunkSize;
                var yDown = x + y1 * WorldChunkConstants.ChunkSize;
                var dx = ((Heightmap[xLeft] - Heightmap[xRight]) + 1) * 0.5f;
                var dy = ((Heightmap[yUp] - Heightmap[yDown]) + 1) * 0.5f;

                var luma = new float4(dx, dy, 1, 1);

                // [Anton] Got rid of float4 textures
                Normalmap[i] = new byte4((byte)(luma.x * 0xFF), (byte)(luma.y * 0xFF), (byte)(luma.z * 0xFF), (byte)(luma.w * 0xFF));                
            }
        }

        struct GenerateSplatmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> Heightmap;
            [WriteOnly] public NativeArray<byte4> Splatmap;
            [WriteOnly] public float max;
            [WriteOnly] public float height;
            [WriteOnly] public float scope;

            public void Execute(int i)
            {

                // Calcul splat map
                var x = i / WorldChunkConstants.ChunkSize;
                var y = i % WorldChunkConstants.ChunkSize;
                //Normal map
                var x_1 = math.clamp(x - 1, 0, WorldChunkConstants.ChunkSize - 1);
                var y_1 = math.clamp(y - 1, 0, WorldChunkConstants.ChunkSize - 1);
                var x1 = math.clamp(x + 1, 0, WorldChunkConstants.ChunkSize - 1);
                var y1 = math.clamp(y + 1, 0, WorldChunkConstants.ChunkSize - 1);
                var xLeft = x_1 + y * WorldChunkConstants.ChunkSize;
                var xRight = x1 + y * WorldChunkConstants.ChunkSize;
                var yUp = x + y_1 * WorldChunkConstants.ChunkSize;
                var yDown = x + y1 * WorldChunkConstants.ChunkSize;
                var dxN = ((Heightmap[xLeft] - Heightmap[xRight]) + 1) * 0.5f;
                var dyN = ((Heightmap[yUp] - Heightmap[yDown]) + 1) * 0.5f;

                // Normalise x/y coordinates to range 0-1 
                //var xN = math.clamp(x, 0, WorldChunkConstants.ChunkSize - 1);
                //var yN = math.clamp(y, 0, WorldChunkConstants.ChunkSize - 1);

                //Find height
                height = Heightmap[i];

                //Find scope
                float3 normalVector = new float3(dxN, dyN, 1f);
                float3 yVector = new float3(0f, 1f, 0f);
                float scope = math.dot(normalVector, yVector);
               

                //THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE
                // Texture[0] GROUND (under 20%)
                var w0 = 1;// Mathf.Clamp01((terrainData.heightmapHeight - height));

                //// Texture[1] GRASS (decreases with scope)
                var w1 = 0.5f;// scope;// Mathf.Clamp01((terrainData.heightmapHeight - height));

                //// Texture[2] ROCKS (increases with scope)
                var w2 = 0f; // 1.0f - scope;// 1.0f - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / 5.0f));

                //// Texture[3] SNOW (above 75%)
                var w3 = 0f; //(height > 0.5f) ? 0.5f : 0f;// height * Mathf.Clamp01(normal.z);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                var w = new float4(w0, w1, w2, w3);
                //w = math.normalize(w);// [w1, w2, w3, w4]

                // [Anton] Got rid of float4 textures
                Splatmap[i] = new byte4((byte)(w.x * 0xFF), (byte)(w.y * 0xFF), (byte)(w.z * 0xFF), (byte)(w.w * 0xFF));
            }
        }

        struct TriggeredSectors
        {
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public ComponentDataArray<TerrainChunkGeneratorTrigger> Triggers;
            [ReadOnly] public ComponentDataArray<Sector> Sectors;
            //HeightMap
            [ReadOnly] public SubtractiveComponent<TerrainChunkHasHeightmap> NotHasHeightmap;
            [ReadOnly] public SubtractiveComponent<TerrainChunkIsHeightmapBakingComponent> NotIsBakingHeightmap;
            //NormalMap
            public SubtractiveComponent<TerrainChunkHasNormalmap> NotHasNormalmap;
            public SubtractiveComponent<TerrainChunkIsNormalmapBakingComponent> NotIsBakingNormalmap;
            //SplatMap
            public SubtractiveComponent<TerrainChunkHasSplatmap> NotHasSplatmap;
            public SubtractiveComponent<TerrainChunkIsSplatmapBakingComponent> NotIsBakingSplatmap;
        }

        struct DataToUploadOnGPU
        {
            public JobHandle Handle;
            public Sector Sector;
            public Entity Entity;
            public NativeArray<float> Heightmap;
            public NativeArray<byte4> Normalmap;
            public NativeArray<byte4> Splatmap;
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
                if (!data.Handle.IsCompleted)
                    continue;

                data.Handle.Complete();

                if (EntityManager.Exists(data.Entity))
                {
                    //HeightMap
                    var heightmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    );
                    heightmapTex.wrapMode = TextureWrapMode.Clamp;
                    heightmapTex.LoadRawTextureData(data.Heightmap);
                    heightmapTex.Apply();
                    m_TerrainChunkAssetDataSystem.AddSector(data.Sector, data.Heightmap, heightmapTex);

                    cmd.RemoveComponent<TerrainChunkIsHeightmapBakingComponent>(data.Entity);
                    cmd.AddComponent(data.Entity, new TerrainChunkHasHeightmap());

                    //NormalMap
                    var normalmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    );
                    normalmapTex.LoadRawTextureData(data.Normalmap);
                    normalmapTex.Apply();
                    m_TerrainChunkAssetDataSystem.SetNormalmapTex(data.Sector, normalmapTex);

                    cmd.RemoveComponent<TerrainChunkIsNormalmapBakingComponent>(data.Entity);
                    cmd.AddComponent(data.Entity, new TerrainChunkHasNormalmap());

                    //SplatMap
                    var splatmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    );
                    splatmapTex.LoadRawTextureData(data.Splatmap);
                    splatmapTex.Apply();
                    m_TerrainChunkAssetDataSystem.SetSplatmapTex(data.Sector, splatmapTex);

                    cmd.RemoveComponent<TerrainChunkIsSplatmapBakingComponent>(data.Entity);
                    cmd.AddComponent(data.Entity, new TerrainChunkHasSplatmap());

                    // Keep Heightmap, as it's passed to Asset Data system for later use
                    data.Normalmap.Dispose();
                    data.Splatmap.Dispose();
                }
                else
                {
                    // By the time we generated data, system doesn't need it anymore
                    data.Heightmap.Dispose();
                    data.Normalmap.Dispose();
                    data.Splatmap.Dispose();
                }

                m_DataToUploadOnGPU.RemoveAt(i);
            }

            // Update sectors
            if (m_TriggeredSectors.Sectors.Length > 0)
            {
                for (int i = 0, c = m_TriggeredSectors.Sectors.Length; i < c; ++i)
                {
                    var entity = m_TriggeredSectors.Entities[i];
                    var sector = m_TriggeredSectors.Sectors[i];
                    var heightmap = new NativeArray<float>(WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize, Allocator.Persistent); 
                    var normalmap = new NativeArray<byte4>(WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize, Allocator.Persistent);
                    var splatmap = new NativeArray<byte4>(WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize, Allocator.Persistent);

                    JobHandle thisChunkJob;
                    {
                        //HeightMap
                        var job = new GenerateHeightmapJob
                        {
                            Sector = sector,
                            Heightmap = heightmap
                        };

                        thisChunkJob = job.Schedule(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            64
                        );
                        //NormalMap
                        var job2 = new GenerateNormalmapJob
                        {
                            Heightmap = heightmap,
                            Normalmap = normalmap
                        };

                        thisChunkJob = job2.Schedule(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            64,
                            thisChunkJob
                        );
                        //SplatMap
                        var job3 = new GenerateSplatmapJob
                        {
                            Heightmap = heightmap,
                            Splatmap = splatmap
                        };

                        thisChunkJob = job3.Schedule(
                            WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                            64,
                            thisChunkJob
                        );
                    }

                    cmd.AddComponent(entity, new TerrainChunkIsHeightmapBakingComponent());
                    cmd.AddComponent(entity, new TerrainChunkIsNormalmapBakingComponent());
                    cmd.AddComponent(entity, new TerrainChunkIsSplatmapBakingComponent());

                    m_DataToUploadOnGPU.Add(new DataToUploadOnGPU
                    {
                        Handle = thisChunkJob,
                        Sector = sector,
                        Entity = entity,
                        Heightmap = heightmap,
                        Normalmap = normalmap,
                        Splatmap = splatmap
                    });
                }
            }

            return dependsOn;
        }
    }
}
