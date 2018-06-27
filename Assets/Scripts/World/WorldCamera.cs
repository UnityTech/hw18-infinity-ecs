using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [UpdateBefore(typeof(TransformSystem))]
    public class WorldCamera : ComponentSystem
    {
        public int2 sector { get; private set; }

        protected override void OnCreateManager(int capacity)
        {
        }

        protected override void OnUpdate()
        {
            var pos = Camera.main.transform.position;
            sector = new int2((int)(pos.x / Sector.SECTOR_SIZE + 0.5f), (int)(pos.z / Sector.SECTOR_SIZE + 0.5f));
        }

        public void AddRemoveGrid(float radius, ref ComponentDataArray<Sector> sectors, Action<int2> onAdd, Action<int, int2> onRemove)
        {
            int halfSize = (int)(radius + 0.5f);
            int gridSize = halfSize * 2;
            int gridCells = gridSize * gridSize;
            int2 offset = new int2(halfSize, halfSize);

            var grid = new NativeArray<byte>(gridCells, Allocator.Temp);
            for (int i = 0; i < sectors.Length; ++i)
            {
                int2 sector = sectors[i].value;
                int2 dist = sector - this.sector + offset;

                if (dist.x < gridSize && dist.y < gridSize && dist.x >= 0 && dist.y >= 0)
                    grid[dist.y * gridSize + dist.x] = 1;
                else
                    onRemove(i, sector);
            }

            for (int j = 0; j < gridSize; ++j)
            {
                for (int i = 0; i < gridSize; ++i)
                {
                    if (grid[j * gridSize + i] == 0)
                        onAdd(this.sector + new int2(i, j) - offset);
                }
            }

            grid.Dispose();
        }
    }
}