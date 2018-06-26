using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Unity.InfiniteWorld
{
    [UnityEngine.ExecuteInEditMode]
    [UpdateAfter(typeof(TransformSystem))]
    [UpdateAfter(typeof(TerrainGenerationSystem))]
    public class TerrainChunkRenderSystem : ComponentSystem
    {
        public static readonly int _Heightmap = Shader.PropertyToID("_Heightmap");

        // Universal material & mesh for all chunks
        Mesh gridMesh;
        Material material;
        MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();

        struct TerrainDataGroup
        {
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Transform> transforms;
        }

        [Inject]
        TerrainDataGroup terrainDataGroup;
        [Inject]
        TerrainChunkAssetDataSystem chunkAssets;

        protected override void OnCreateManager(int capacity)
        {
            gridMesh = TerrainChunkUtils.GenerateGridMesh(new float2(WorldChunkConstants.ChunkSize, WorldChunkConstants.ChunkSize), new int2(WorldChunkConstants.ChunkSize, WorldChunkConstants.ChunkSize));
            material = Resources.Load<Material>("Art/TerrainMaterial");
            Assert.AreNotEqual(null, material);
        }

        protected override void OnDestroyManager()
        {
            Object.Destroy(gridMesh);
        }

        protected override void OnUpdate()
        {
            for(int index = 0; index < terrainDataGroup.sectors.Length; index++)
            {
                var heightmap = chunkAssets.GetChunkHeightmapTex(terrainDataGroup.sectors[index]);
                materialBlock.SetTexture(_Heightmap, heightmap);
                RenderHelpers.CopyMatrices(terrainDataGroup.transforms, index, 1, RenderHelpers.matricesArray);
                Graphics.DrawMeshInstanced(gridMesh, 0, material, RenderHelpers.matricesArray, 1, materialBlock, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
            }
        }
    }
}
