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

        Dictionary<int2, AssetData> m_CPUDataBySector;

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
            m_CPUDataBySector = new Dictionary<int2, AssetData>();
            m_FreedIndices = new List<int>();
            m_NextIndex = 0;
        }

        protected override void OnDestroyManager()
        {
            foreach (var assets in m_CPUDataBySector)
            {
                assets.Value.Heightmap.Dispose();
                Object.Destroy(assets.Value.HeightmapTex);
            }
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
                m_CPUDataBySector.Add(sector.value, asset);
            }

            return asset;
        }
    }
}
