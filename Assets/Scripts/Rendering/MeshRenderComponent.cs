using Unity.Entities;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public struct MeshRender : ISharedComponentData
    {
        public Mesh mesh;
        public Material material0;
        public Material material1;
        public Material material2;
        public int materialCount;
    }

    public class MeshRenderComponent : SharedComponentDataWrapper<MeshRender> { }
}