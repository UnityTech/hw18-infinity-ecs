using Unity.Entities;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public struct MeshRender : ISharedComponentData
    {
        public Mesh mesh;
        public Material material;
    }

    public class MeshRenderComponent : SharedComponentDataWrapper<MeshRender> { }
}