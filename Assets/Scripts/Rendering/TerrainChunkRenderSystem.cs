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
        // Universal material & mesh for all chunks
        Mesh gridMesh;
        Material material;
        MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();

        // Data manager
        TerrainChunkGenerator chunksSystem;

        // Instance renderer takes only batches of 1023
        Matrix4x4[] matricesArray = new Matrix4x4[1023];

        struct TerrainDataGroup
        {
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Transform> transforms;
        }

        [Inject]
        TerrainDataGroup terrainDataGroup;

        // This is copy&paste from MeshInstanceRendererSystem, necessary until Graphics.DrawMeshInstanced supports NativeArrays pulling the data in from a job.
        public unsafe static void CopyMatrices(ComponentDataArray<Transform> transforms, int beginIndex, int length, Matrix4x4[] outMatrices)
        {
            fixed (Matrix4x4* matricesPtr = outMatrices)
            {
                Assert.AreEqual(sizeof(Matrix4x4), sizeof(Transform));
                var matricesSlice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<Transform>(matricesPtr, sizeof(Matrix4x4), length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref matricesSlice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
                transforms.CopyTo(matricesSlice, beginIndex);
            }
        }

        public void Init(TerrainChunkGenerator _chunksSystem)
        {
            chunksSystem = _chunksSystem;

            gridMesh = TerrainChunkUtils.GenerateGridMesh(new float2(WorldChunkConstants.ChunkSize, WorldChunkConstants.ChunkSize), new int2(WorldChunkConstants.ChunkSize, WorldChunkConstants.ChunkSize));
            material = Resources.Load<Material>("Art/TerrainMaterial");
            Assert.AreNotEqual(null, material);
        }

        protected override void OnUpdate()
        {
            for(int index = 0; index < terrainDataGroup.sectors.Length; index++)
            {
                var buffer = chunksSystem.GetHeightmapBuffer(terrainDataGroup.sectors[index]);
                materialBlock.SetBuffer("buffer", buffer);
                CopyMatrices(terrainDataGroup.transforms, index, 1, matricesArray);
                Graphics.DrawMeshInstanced(gridMesh, 0, material, matricesArray, 1, materialBlock, /*castShadows*/ShadowCastingMode.On, /*receiveShadows*/true);
            }
        }
    }
}
