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
            public NativeArray<Color> Heightmap;
            public Texture2D HeightmapTex;
        }

        NativeHashMap<int2, int> m_CPUDataIndexBySector;
        NativeArray<AssetData> m_CPUDatas;

        List<int> m_FreedIndices;
        int m_NextIndex;

        public NativeArray<Color> GetChunkHeightmap(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).Heightmap;
        }

        public Texture2D GetChunkHeightmapTex(Sector sector)
        {
            return GetOrCreateChunkAssetData(sector).HeightmapTex;
        }

        protected override void OnCreateManager(int capacity)
        {
            m_CPUDataIndexBySector = new NativeHashMap<int2, int>(WorldChunkConstants.ChunkCapacity, Allocator.Persistent);
            m_CPUDatas = new NativeArray<AssetData>(WorldChunkConstants.ChunkCapacity, Allocator.Persistent);
            m_FreedIndices = new List<int>();
            m_NextIndex = 0;
        }

        protected override void OnDestroyManager()
        {
            m_CPUDataIndexBySector.Dispose();
            m_CPUDatas.Dispose();
            m_FreedIndices = null;
        }

        protected override void OnUpdate()
        {
        }

        AssetData GetOrCreateChunkAssetData(Sector sector)
        {
            if (!m_CPUDataIndexBySector.TryGetValue(sector.value, out int index))
            {
                var asset = new AssetData
                {
                    Heightmap = new NativeArray<Color>(
                        WorldChunkConstants.ChunkSize * WorldChunkConstants.ChunkSize,
                        Allocator.Persistent
                    ),
                    HeightmapTex = new Texture2D(
                        WorldChunkConstants.ChunkSize,
                        WorldChunkConstants.ChunkSize,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    )
                };
                index = -1;
                if (m_FreedIndices.Count > 0)
                {
                    index = m_FreedIndices[0];
                    m_FreedIndices.RemoveAt(0);
                }
                else
                {
                    index = m_NextIndex;
                    ++m_NextIndex;
                }
                m_CPUDatas[index] = asset;
            }

            return m_CPUDatas[index];
        }
    }
}
