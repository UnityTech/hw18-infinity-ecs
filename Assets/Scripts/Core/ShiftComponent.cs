using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct Shift : IComponentData
    {
        public float3 value;

        public Shift(float3 shift)
        {
            value = shift;
        }
    }

    public class ShiftComponent : ComponentDataWrapper<Shift> { }
}
