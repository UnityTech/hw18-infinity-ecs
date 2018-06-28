using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public class WorldSectorLifecycleSystem : ComponentSystem
    {
        struct ObjectsFilter
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<WorldSectorObject> worldSectors;
        }

        [Inject]
        ObjectsFilter objectsFilter;

        [Inject]
        CameraSystem camera;

        EntityArchetype archetype;

        protected override void OnCreateManager(int capacity)
        {
            archetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Transform), typeof(WorldSectorObject));
        }

        protected override void OnUpdate()
        {
            var cameraSector = EntityManager.GetComponentData<Sector>(camera.main);

            // Remove anything outside ObjectsUnloadDistance radius
            CameraSystem.AddRemoveGrid(
                cameraSector.value,
                WorldChunkConstants.ObjectsUnloadDistance, 
                ref objectsFilter.sectors, 
                (int2 sector) => { }, 
                (int index, int2 sector) => 
                    {
                        PostUpdateCommands.DestroyEntity(objectsFilter.entities[index]);
                    }
            );

            // Add any missing sector inside ObjectsVisibleDistance radius
            CameraSystem.AddRemoveGrid(
                cameraSector.value,
                WorldChunkConstants.ObjectsVisibleDistance,
                ref objectsFilter.sectors,
                (int2 sector) =>
                    {
                        PostUpdateCommands.CreateEntity(archetype);
                        PostUpdateCommands.SetComponent(new Sector(sector));

                        PostUpdateCommands.CreateEntity();
                        PostUpdateCommands.AddComponent(new WorldSectorVegetationCreateEvent() { sector = sector });
                    },
                (int index, int2 sector) => { }
            );
        }
    }
}