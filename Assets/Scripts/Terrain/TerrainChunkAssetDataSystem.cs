using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    // Use a ComponentSystem only to be able to inject it and to properly initialize && dispose it
    public class TerrainChunkAssetDataSystem : ComponentSystem
    {
        struct AssetData
        {
            public NativeArray<float> Heightmap;
            public Texture2D HeightmapTex;

            public NativeArray<float4> Normalmap;
            public Texture2D NormalmapTex;
        }

        Dictionary<int2, AssetData> m_CPUDataBySector;

        List<int> m_FreedIndices;
        int m_NextIndex;

        public NativeArray<float> GetChunkHeightmap(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).Heightmap;
        }

        public Texture2D GetChunkHeightmapTex(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).HeightmapTex;
        }

        public NativeArray<float4> GetChunkNormalmap(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).Normalmap;
        }

        public Texture2D GetChunkNormalmapTex(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).NormalmapTex;
        }

        public void DisposeChunkData(Sector sector)
        {
            if (m_CPUDataBySector.TryGetValue(sector.value, out AssetData asset))
                Dispose(asset);
            m_CPUDataBySector.Remove(sector.value);
        }

        protected override void OnCreateManager(int capacity)
        {
            m_CPUDataBySector = new Dictionary<int2, AssetData>();
            m_FreedIndices = new List<int>();
            m_NextIndex = 0;
        }

        protected override void OnDestroyManager()
        {
            foreach (var assets in m_CPUDataBySector)
                Dispose(assets.Value);
            m_CPUDataBySector.Clear();
            m_CPUDataBySector = null;
            m_FreedIndices = null;
        }

        protected override void OnUpdate()
        {
        }

        AssetData GetOrCreateChunkAssetData(Sector sector)
        {
            if (!m_CPUDataBySector.TryGetValue(sector.value, out AssetData asset))
            {
                asset = new AssetData
                {
                    Heightmap = new NativeArray<float>(
                        WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                        Allocator.Persistent
                    ),
                    HeightmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    ),

                    Normalmap = new NativeArray<float4>(
                        WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                        Allocator.Persistent
                    ),
                    NormalmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    )

                };
                asset.HeightmapTex.wrapMode = TextureWrapMode.Clamp;
                m_CPUDataBySector.Add(sector.value, asset);
            }

            return asset;
        }

        void Dispose(AssetData data)
        {
            data.Heightmap.Dispose();
            UnityEngine.Object.Destroy(data.HeightmapTex);
            data.Normalmap.Dispose();
            UnityEngine.Object.Destroy(data.NormalmapTex);
        }
    }
}
