using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct Rotation : IComponentData
    {
        public quaternion rotation;

        public Rotation(quaternion _rotation)
        {
            rotation = _rotation;
        }
    }

    public class RotationComponent : ComponentDataWrapper<Rotation> { }
}