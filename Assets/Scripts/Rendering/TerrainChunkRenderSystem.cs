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
        public static readonly int _Normalmap = Shader.PropertyToID("_Normalmap");
        public static readonly int _Sector = Shader.PropertyToID("_Sector");
        public static readonly int _HeightmapScale = Shader.PropertyToID("_HeightmapScale");

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
                var heightmap = chunkAssets.GetChunkHeightmapTex(sector);
                var normalmap = chunkAssets.GetChunkNormalmapTex(sector);
                materialBlock.SetTexture(_Heightmap, heightmap);
                materialBlock.SetTexture(_Normalmap, normalmap);
                materialBlock.SetVector(_Sector, new Vector4(sector.value.x, sector.value.y, 0, 0));
                materialBlock.SetFloat(_HeightmapScale, WorldChunkConstants.TerrainHeightScale);
                RenderHelpers.CopyMatrices(terrainDataGroup.transforms, index, 1, RenderHelpers.matricesArray);
                Graphics.DrawMeshInstanced(gridMesh, 0, material, RenderHelpers.matricesArray, 1, materialBlock, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
            }
        }
    }
}
