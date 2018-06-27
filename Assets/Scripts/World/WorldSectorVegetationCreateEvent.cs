using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    public struct WorldSectorVegetationCreateEvent : IComponentData
    {
        public int2 sector;
    }
}