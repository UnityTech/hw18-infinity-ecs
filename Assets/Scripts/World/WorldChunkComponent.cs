using System;
using Unity.Entities;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct WorldChunk : IComponentData
    {
    }

    public class WorldChunkComponent : ComponentDataWrapper<WorldChunk> { }
}