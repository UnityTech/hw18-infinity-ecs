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
        WorldCamera camera;

        EntityArchetype archetype;

        protected override void OnCreateManager(int capacity)
        {
            archetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Transform), typeof(WorldSectorObject));
        }

        protected override void OnUpdate()
        {
            // Remove anything outside ObjectsUnloadDistance radius
            camera.AddRemoveGrid(
                WorldChunkConstants.ObjectsUnloadDistance, 
                ref objectsFilter.sectors, 
                (int2 sector) => { }, 
                (int index, int2 sector) => 
                    {
                        Debug.Log("KILL: " + sector.x + ":" + sector.y);
                        PostUpdateCommands.DestroyEntity(objectsFilter.entities[index]);
                    }
            );

            // Add any missing sector inside ObjectsVisibleDistance radius
            camera.AddRemoveGrid(
                WorldChunkConstants.ObjectsVisibleDistance,
                ref objectsFilter.sectors,
                (int2 sector) =>
                    {
                        Debug.Log("CREATE: " + sector.x + ":" + sector.y);

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