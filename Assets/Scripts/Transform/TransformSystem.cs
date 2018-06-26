using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.InfiniteWorld
{
    public class TransformSystem : ComponentSystem
    {
        struct TransformPosGroup
        {
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public SubtractiveComponent<Rotation> rotations;
        }

        [Inject]
        TransformPosGroup transformsPosGroup;

        struct TransformPosRotGroup
        {
            public ComponentDataArray<Transform> transforms;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<Shift> shifts;
            [ReadOnly]
            public ComponentDataArray<Rotation> rotations;
        }

        [Inject]
        TransformPosRotGroup transformsPosRotGroup;

        /*
        [BurstCompile]
        struct UpdateTransformPosRot: IJobParallelFor
        {
            [ReadOnly]
            public ComponentDataArray<Position> positions;
            [ReadOnly]
            public ComponentDataArray<Rotation> rotations;
            public ComponentDataArray<Transform> transforms;

            public void Execute(int index)
            {
                Position pos = positions[index];
                float4x4 matrix = math.rottrans(rotations[index].rotation, positions[index].shift + new float3(pos.sector.x * Position.SECTOR_SIZE, 0, pos.sector.y * Position.SECTOR_SIZE));
                transforms[index] = new Transform(matrix);
            }
        }
        */

        protected override void OnUpdate()
        {
            // Entities with Position only
            {
                var transforms = transformsPosGroup.transforms;
                var sectors = transformsPosGroup.sectors;
                var shifts = transformsPosGroup.shifts;

                for (int index = 0; index < transforms.Length; index++)
                {
                    Shift shift = shifts[index];
                    Sector sector = sectors[index];
                    float4x4 matrix = math.translate(shift.value + new float3(sector.value.x * Sector.SECTOR_SIZE, 0, sector.value.y * Sector.SECTOR_SIZE));
                    transforms[index] = new Transform(matrix);
                }
            }

            // Entities with Position + Rotation
            {
                var transforms = transformsPosRotGroup.transforms;
                var sectors = transformsPosGroup.sectors;
                var shifts = transformsPosGroup.shifts;
                var rotations = transformsPosRotGroup.rotations;

                for (int index = 0; index < transforms.Length; index++)
                {
                    Shift shift = shifts[index];
                    Sector sector = sectors[index];
                    float4x4 matrix = math.rottrans(rotations[index].rotation, shift.value + new float3(sector.value.x * Sector.SECTOR_SIZE, 0, sector.value.y * Sector.SECTOR_SIZE));
                    transforms[index] = new Transform(matrix);
                }
            }

        }
    }
}
