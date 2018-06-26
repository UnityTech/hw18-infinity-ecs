using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class TerrainChunkUtils
    {
        public static Mesh GenerateGridMesh(float2 size, int2 splits)
        {
            Mesh mesh = new Mesh();

            int count = (splits.x + 2) * (splits.y + 2);
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            var indices = new int[(splits.x + 1) * (splits.y + 1) * 6];

            float stepX = 1.0f / splits.x;
            float stepY = 1.0f / splits.y;

            for (int tempY = 0; tempY < (splits.y + 2); ++tempY)
            {
                for (int tempX = 0; tempX < (splits.x + 2); ++tempX)
                {
                    int index = tempY * (splits.x + 2) + tempX;
                    positions[index] = new Vector3(tempX * stepX * size.x, 0, tempY * stepY * size.y);
                    normals[index] = new Vector3(0, 1, 0);
                    uvs[index] = new Vector2(tempX * stepX, tempY * stepY);
                }
            }

            int temp = 0;
            int lineStride = splits.x + 2;
            for (int tempY = 0; tempY < (splits.y + 1); ++tempY)
            {
                for (int tempX = 0; tempX < (splits.x + 1); ++tempX)
                {
                    int baseVert = tempY * lineStride + tempX;
                    indices[temp + 0] = baseVert + 0;
                    indices[temp + 1] = baseVert + lineStride;
                    indices[temp + 2] = baseVert + 1;

                    indices[temp + 3] = baseVert + lineStride;
                    indices[temp + 4] = baseVert + lineStride + 1;
                    indices[temp + 5] = baseVert + 1;

                    temp += 6;
                }
            }

            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;

            return mesh;
        }
    }
}