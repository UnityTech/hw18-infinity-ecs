using Unity.Entities;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    public struct LOD : IComponentData
    {
        public int lod;

        public LOD(int _lod)
        {
            lod = _lod;
        }
    }

    public class LODComponent : ComponentDataWrapper<LOD> { }
}
