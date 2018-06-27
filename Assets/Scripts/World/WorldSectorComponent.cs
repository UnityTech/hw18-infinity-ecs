using System;
using Unity.Entities;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct WorldSector : IComponentData
    {
    }

    public class WorldChunkComponent : ComponentDataWrapper<WorldSector> { }
}