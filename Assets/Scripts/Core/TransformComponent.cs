using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    [Serializable]
    public struct Transform : IComponentData
    {
        public float4x4 transform;

        public Transform(float4x4 _transform)
        {
            transform = _transform;
        }
    }

    public class TransformComponent : ComponentDataWrapper<Transform> { }
}
