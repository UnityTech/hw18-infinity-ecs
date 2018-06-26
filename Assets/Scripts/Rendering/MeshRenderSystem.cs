using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.InfiniteWorld
{
    public class MeshRenderSystem : ComponentSystem
    {
        struct RenderGroup
        {
            [ReadOnly]
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<MeshRender> renderers;
        }

        [Inject]
        RenderGroup renderGroup;

        protected override void OnUpdate()
        {
            for (int index = 0; index < renderGroup.renderers.Length; index++)
            {
                RenderHelpers.CopyMatrices(renderGroup.transforms, index, 1, RenderHelpers.matricesArray);
                Graphics.DrawMeshInstanced(renderGroup.renderers[index].mesh, 0, renderGroup.renderers[index].material, RenderHelpers.matricesArray, 1, null, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
            }
        }
    }
}