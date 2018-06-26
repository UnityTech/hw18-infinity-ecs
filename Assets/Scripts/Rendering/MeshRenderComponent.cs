using Unity.Entities;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public struct MeshRender : IComponentData
    {
        public Mesh mesh;
        public Material material;
    }

    public class MeshRenderComponent : ComponentDataWrapper<MeshRender> { }
}