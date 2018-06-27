using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class TransformSystem : JobComponentSystem
    {
        struct TransformSectorFilter
        {
            [WriteOnly]
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public SubtractiveComponent<Shift> shift;
            [ReadOnly]
            public SubtractiveComponent<Rotation> rotations;
        }

        [Inject]
        TransformSectorFilter transformsSectorGroup;

        struct TransformSectorShiftFilter
        {
            [WriteOnly]
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public SubtractiveComponent<Rotation> rotations;
        }

        [Inject]
        TransformSectorShiftFilter transformsSectorShiftGroup;

        struct TransformSectorShiftRotationFilter
        {
            [WriteOnly]
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public ComponentDataArray<Rotation> rotations;
        }

        [Inject]
        TransformSectorShiftRotationFilter transformSectorShiftRotationGroup;

        [Inject]
        WorldCamera camera;

        [BurstCompile]
        struct TransformSectorJob: IJobParallelFor
        {
            [ReadOnly]
            public int2 cameraSector;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;

            public ComponentDataArray<Transform> transforms;

            public void Execute(int index)
            {
                int2 sector = sectors[index].value - cameraSector;
                float4x4 matrix = math.translate(new float3(sector.x * Sector.SECTOR_SIZE, 0, sector.y * Sector.SECTOR_SIZE));
                transforms[index] = new Transform(matrix);
            }
        }

        [BurstCompile]
        struct TransformSectorShiftJob : IJobParallelFor
        {
            [ReadOnly]
            public int2 cameraSector;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;

            public ComponentDataArray<Transform> transforms;

            public void Execute(int index)
            {
                float3 shift = shifts[index].value;
                int2 sector = sectors[index].value - cameraSector;
                float4x4 matrix = math.translate(shift + new float3(sector.x * Sector.SECTOR_SIZE, 0, sector.y * Sector.SECTOR_SIZE));
                transforms[index] = new Transform(matrix);
            }
        }

        [BurstCompile]
        struct TransformSectorShiftRotationJob : IJobParallelFor
        {
            [ReadOnly]
            public int2 cameraSector;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public ComponentDataArray<Rotation> rotations;

            public ComponentDataArray<Transform> transforms;

            public void Execute(int index)
            {
                float3 shift = shifts[index].value;
                int2 sector = sectors[index].value - cameraSector;
                float4x4 matrix = math.rottrans(rotations[index].rotation, shift + new float3(sector.x * Sector.SECTOR_SIZE, 0, sector.y * Sector.SECTOR_SIZE));
                transforms[index] = new Transform(matrix);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Entities with Sector only
            JobHandle sectorJobHandle;
            {
                var transforms = transformsSectorGroup.transforms;
                var sectors = transformsSectorGroup.sectors;

                var transformJob = new TransformSectorJob
                {
                    cameraSector = camera.sector,
                    sectors = sectors,
                    transforms = transforms
                };

                sectorJobHandle = transformJob.Schedule(transforms.Length, 4, inputDeps);
            }

            // Entities with Sector and Shift only
            JobHandle sectorShiftJobHandle;
            {
                var transforms = transformsSectorShiftGroup.transforms;
                var sectors = transformsSectorShiftGroup.sectors;
                var shifts = transformsSectorShiftGroup.shifts;

                var transformJob = new TransformSectorShiftJob
                {
                    cameraSector = camera.sector,
                    sectors = sectors,
                    shifts = shifts,
                    transforms = transforms
                };

                sectorShiftJobHandle = transformJob.Schedule(transforms.Length, 4, sectorJobHandle);
            }

            // Entities with Position + Rotation
            JobHandle sectorShiftRotationJobHandle;
            {
                var transforms = transformSectorShiftRotationGroup.transforms;
                var sectors = transformSectorShiftRotationGroup.sectors;
                var shifts = transformSectorShiftRotationGroup.shifts;
                var rotations = transformSectorShiftRotationGroup.rotations;

                var transformJob = new TransformSectorShiftRotationJob
                {
                    cameraSector = camera.sector,
                    sectors = sectors,
                    shifts = shifts,
                    rotations = rotations,
                    transforms = transforms
                };

                sectorShiftRotationJobHandle = transformJob.Schedule(transforms.Length, 4, sectorShiftJobHandle);
            }

            return sectorShiftRotationJobHandle;
        }
    }
}
