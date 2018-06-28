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

                for(int temp = 0; temp < transforms.Length; ++temp)
                {
                    RenderHelpers.CopyMatrices(transforms, temp, 1, RenderHelpers.matricesArray);
                    if (renderer.materialCount > 0)
                        Graphics.DrawMeshInstanced(renderer.mesh, 0, renderer.material0, RenderHelpers.matricesArray, 1, null, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
                    if (renderer.materialCount > 1)
                        Graphics.DrawMeshInstanced(renderer.mesh, 1, renderer.material1, RenderHelpers.matricesArray, 1, null, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
                    if (renderer.materialCount > 2)
                        Graphics.DrawMeshInstanced(renderer.mesh, 2, renderer.material2, RenderHelpers.matricesArray, 1, null, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);

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