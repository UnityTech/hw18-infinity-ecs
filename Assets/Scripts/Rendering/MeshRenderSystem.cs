using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.InfiniteWorld
{
    [UpdateAfter(typeof(TransformSystem))]
    public class MeshRenderSystem : ComponentSystem
    {
        ComponentGroup renderGroup;
        List<MeshRender> cacheduniqueRendererTypes = new List<MeshRender>(10);

        protected override void OnCreateManager(int capacity)
        {
            renderGroup = GetComponentGroup(
                new ComponentType(typeof(MeshRender), ComponentType.AccessMode.ReadOnly),
                new ComponentType(typeof(Transform), ComponentType.AccessMode.ReadOnly));
        }

        protected override void OnUpdate()
        {
            EntityManager.GetAllUniqueSharedComponentDatas(cacheduniqueRendererTypes);
            var forEachFilter = renderGroup.CreateForEachFilter(cacheduniqueRendererTypes);

            for (int i = 0; i != cacheduniqueRendererTypes.Count; i++)
            {
                var renderer = cacheduniqueRendererTypes[i];
                var transforms = renderGroup.GetComponentDataArray<Transform>(forEachFilter, i);

                if (renderer.mesh == null)
                    continue;

                for(int temp = 0; temp < transforms.Length; ++temp)
                {
                    RenderHelpers.CopyMatrices(transforms, temp, 1, RenderHelpers.matricesArray);

                    var materials = new Material[]{ renderer.material0, renderer.material1, renderer.material2 };
                    for (int subpart = 0; subpart < renderer.materialCount; ++subpart)
                        Graphics.DrawMeshInstanced(renderer.mesh, subpart, materials[subpart], RenderHelpers.matricesArray, 1, null, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
                }

                /*
                int beginIndex = 0;
                while (beginIndex < transforms.Length)
                {
                    //int length = math.min(RenderHelpers.matricesArray.Length, transforms.Length - beginIndex);
                    //RenderHelpers.CopyMatrices(transforms, beginIndex, length, RenderHelpers.matricesArray);
                    //Graphics.DrawMeshInstanced(renderer.mesh, 0, renderer.material, RenderHelpers.matricesArray, length, null, /*castShadows* /ShadowCastingMode.On, /*receiveShadows* /true);

                    beginIndex += length;
                }
                */
            }

            cacheduniqueRendererTypes.Clear();
            forEachFilter.Dispose();
        }
    }
}