using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.InfiniteWorld
{
    [UpdateBefore(typeof(TransformSystem))]
    public class CameraSystem : ComponentSystem
    {
        struct CamerasFilter
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<CameraMainTag> cameras;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public ComponentDataArray<Rotation> rotations;
        }

        [Inject]
        CamerasFilter camerasFilter;

        public Entity main { get; private set; }
        public EntityArchetype cameraArchetype { get; private set; }

        protected override void OnCreateManager(int capacity)
        {
            cameraArchetype = EntityManager.CreateArchetype(typeof(Shift), typeof(Sector), typeof(Rotation), typeof(Transform), typeof(Camera));
        }

        protected override void OnUpdate()
        {
            Assert.AreEqual(1, camerasFilter.cameras.Length);

            // Update main camera
            main = camerasFilter.entities[0];

            // Update Unity camera position/rotation, so that all relative transform are valid
            var transform = UnityEngine.Camera.main.transform;
            transform.localPosition = camerasFilter.shifts[0].value;
            transform.localRotation = camerasFilter.rotations[0].rotation;
        }

        public static void AddRemoveGrid(int2 baseSector, float radius, ref ComponentDataArray<Sector> sectors, Action<int2> onAdd, Action<int, int2> onRemove)
        {
            int halfSize = (int)(radius + 0.5f);
            int gridSize = halfSize * 2;
            int gridCells = gridSize * gridSize;
            int2 offset = new int2(halfSize, halfSize);

            var grid = new NativeArray<byte>(gridCells, Allocator.Temp);
            for (int i = 0; i < sectors.Length; ++i)
            {
                int2 sector = sectors[i].value;
                int2 dist = sector - baseSector + offset;

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
                        onAdd(baseSector + new int2(i, j) - offset);
                }
            }

            grid.Dispose();
        }
    }
}