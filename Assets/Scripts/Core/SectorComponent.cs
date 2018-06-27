using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct Sector : IComponentData
    {
        public const int SECTOR_SIZE = 256;

        public int2 value;

        public Sector(int2 xy)
        {
            value = xy;
        }

        public Sector(int2 baseSector, int xOffset, int yOffset)
        {
            value = baseSector + new int2(xOffset, yOffset);
        }
    }

    public class SectorComponent : ComponentDataWrapper<Sector> { }
}
