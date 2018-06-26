using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.InfiniteWorld
{
    public static class RenderHelpers
    {
        // Instance renderer takes only batches of 1023
        public static Matrix4x4[] matricesArray = new Matrix4x4[1023];

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
    }
}