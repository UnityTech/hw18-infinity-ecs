using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.InfiniteWorld
{
    public class TerrainChunkUtils
    {
        public static Mesh GenerateGridMesh(float2 size, int2 splits)
        {
            Mesh mesh = new Mesh();

            int xVerts = splits.x + 2;
            int yVerts = splits.y + 2;

            int xEdges = xVerts - 1;
            int yEdges = yVerts - 1;

            int indexCount = xEdges * yEdges * 6;

            int count = xVerts * yVerts;
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            var indices = new int[indexCount];

            if (indexCount >= 65536)
                mesh.indexFormat = IndexFormat.UInt32;

            float stepX = 1.0f / xEdges;
            float stepY = 1.0f / yEdges;

            int index = 0;
            float v = 0.0f;
            float z = 0.0f;
            for (int tempY = 0; tempY < yVerts; ++tempY)
            {
                float u = 0.0f;
                float x = 0.0f;
                for (int tempX = 0; tempX < xVerts; ++tempX)
                {
                    positions[index] = new Vector3(x, 0, z);
                    normals[index] = new Vector3(0, 1, 0);
                    uvs[index] = new Vector2(u, v);
                    ++index;

                    u += stepX;
                    x += stepX * size.y;
                }
                v += stepY;
                z += stepY * size.y;
            }

            int temp = 0;
            int baseVert = 0;
            int lineStride = xVerts;
            for (int tempY = 0; tempY < yEdges; ++tempY)
            {
                for (int tempX = 0; tempX < xEdges; ++tempX)
                {
                    indices[temp + 0] = baseVert + 0;
                    indices[temp + 1] = baseVert + lineStride;
                    indices[temp + 2] = baseVert + 1;

                    indices[temp + 3] = baseVert + lineStride;
                    indices[temp + 4] = baseVert + lineStride + 1;
                    indices[temp + 5] = baseVert + 1;

                    temp += 6;
                    ++baseVert;
                }
                ++baseVert;
            }

            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;

            var bounds = mesh.bounds;
            var extents = bounds.extents;

            extents.y = 1024.0f;

            bounds.extents = extents;
            mesh.bounds = bounds;

            return mesh;
        }
    }
}
