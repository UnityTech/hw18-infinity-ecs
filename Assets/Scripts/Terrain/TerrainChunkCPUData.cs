using Unity.Collections;
using Unity.Entities;

namespace Unity.InfiniteWorld
{
    public unsafe struct TerrainChunkCPUData : IComponentData
    {
        public NativeArray<float> Heightmap;
    }
}
