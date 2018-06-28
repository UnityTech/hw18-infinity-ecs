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
        public static readonly int _HeightMap0 = Shader.PropertyToID("_HeightMap0");
        public static readonly int _NormalMap0 = Shader.PropertyToID("_NormalMap0");
        public static readonly int _LayerMaskMap = Shader.PropertyToID("_LayerMaskMap");
        public static readonly int _BaseColor0 = Shader.PropertyToID("_BaseColor0");
        public static readonly int _Sector = Shader.PropertyToID("_Sector");
        public static readonly int _HeightAmplitude0 = Shader.PropertyToID("_HeightAmplitude0");

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
            [ReadOnly]
            public ComponentDataArray<TerrainChunkHasHeightmap> tags;
        }

        [Inject]
        TerrainDataGroup terrainDataGroup;
        [Inject]
        TerrainChunkAssetDataSystem chunkAssets;

        protected override void OnCreateManager(int capacity)
        {
            gridMesh = TerrainChunkUtils.GenerateGridMesh(new float2(WorldChunkConstants.ChunkSize, WorldChunkConstants.ChunkSize), new int2(WorldChunkConstants.ChunkSize - 2, WorldChunkConstants.ChunkSize - 2));
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
                var sector = terrainDataGroup.sectors[index];
                var heightmap = chunkAssets.GetHeightmapTex(sector);
                var normalmap = chunkAssets.GetNormalmapTex(sector);
                var splatmap = chunkAssets.GetSplatmapTex(sector);
                var baseColor = chunkAssets.GetBaseColorTex(sector);
                materialBlock.SetTexture(_HeightMap0, heightmap);
                materialBlock.SetTexture(_NormalMap0, normalmap); 
                materialBlock.SetTexture(_LayerMaskMap, splatmap);
                materialBlock.SetTexture(_BaseColor0, baseColor);
                materialBlock.SetVector(_Sector, new Vector4(sector.value.x, sector.value.y, 0, 0));
                materialBlock.SetFloat(_HeightAmplitude0, WorldChunkConstants.TerrainHeightScale);
                RenderHelpers.CopyMatrices(terrainDataGroup.transforms, index, 1, RenderHelpers.matricesArray);
                Graphics.DrawMeshInstanced(gridMesh, 0, material, RenderHelpers.matricesArray, 1, materialBlock, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
            }
        }
    }
}
