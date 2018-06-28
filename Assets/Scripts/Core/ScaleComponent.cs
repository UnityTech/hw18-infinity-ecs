using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct Scale : IComponentData
    {
        public float3 value;

        public Scale(float3 scale)
        {
            value = scale;
        }
    }

    public class ScaleComponent : ComponentDataWrapper<Scale> { }
}
