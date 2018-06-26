using Unity.Collections;
using Unity.Entities;

namespace Unity.InfiniteWorld
{
    public unsafe struct TerrainChunkAssetData : IComponentData
    {
        public NativeArray<float> Heightmap;
    }
}
