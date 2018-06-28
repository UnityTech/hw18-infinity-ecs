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
        struct SectorShiftFilter
        {
            public ComponentDataArray<Sector> sectors;
            public ComponentDataArray<Shift> shifts;
        }

        [Inject]
        SectorShiftFilter sectorShiftFilter;

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
        CameraSystem camera;

        [BurstCompile]
        struct ShiftSectorUpdateJob: IJobParallelFor
        {
            public ComponentDataArray<Shift> shifts;
            public ComponentDataArray<Sector> sectors;

            public void Execute(int index)
            {
                var shift = shifts[index].value;
                int sectorX = (shift.x > 0.0f) ? (int)(shift.x / WorldChunkConstants.ChunkSize) : -1;
                int sectorY = (shift.z > 0.0f) ? (int)(shift.z / WorldChunkConstants.ChunkSize) : -1;
                float shiftX = shift.x - sectorX * WorldChunkConstants.ChunkSize;
                float shiftZ = shift.z - sectorY * WorldChunkConstants.ChunkSize;
                sectors[index] = new Sector(sectors[index].value, sectorX, sectorY);
                shifts[index] = new Shift(new float3(shiftX, shift.y, shiftZ));
            }
        }

        [BurstCompile]
        struct TransformSectorJob : IJobParallelFor
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
            var cameraSector = EntityManager.GetComponentData<Sector>(camera.main);

            // Entities with Sector only
            JobHandle shiftUpdateJobHandle;
            {
                var shifts = sectorShiftFilter.shifts;
                var sectors = sectorShiftFilter.sectors;

                var updateShiftJob = new ShiftSectorUpdateJob
                {
                    sectors = sectors,
                    shifts = shifts
                };

                shiftUpdateJobHandle = updateShiftJob.Schedule(shifts.Length, 4, inputDeps);
            }

            // Entities with Sector only
            JobHandle sectorJobHandle;
            {
                var transforms = transformsSectorGroup.transforms;
                var sectors = transformsSectorGroup.sectors;

                var transformJob = new TransformSectorJob
                {
                    cameraSector = cameraSector.value,
                    sectors = sectors,
                    transforms = transforms
                };

                sectorJobHandle = transformJob.Schedule(transforms.Length, 4, shiftUpdateJobHandle);
            }

            // Entities with Sector and Shift only
            JobHandle sectorShiftJobHandle;
            {
                var transforms = transformsSectorShiftGroup.transforms;
                var sectors = transformsSectorShiftGroup.sectors;
                var shifts = transformsSectorShiftGroup.shifts;

                var transformJob = new TransformSectorShiftJob
                {
                    cameraSector = cameraSector.value,
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
                    cameraSector = cameraSector.value,
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
