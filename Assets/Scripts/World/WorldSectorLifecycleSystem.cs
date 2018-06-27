using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class WorldSectorLifecycleSystem : ComponentSystem
    {
        struct CreateFilter
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<WorldSector> worldSectors;
        }

        struct KillFilter
        {
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public ComponentDataArray<Sector> sectors;
            [ReadOnly]
            public ComponentDataArray<WorldSector> worldSectors;
            [ReadOnly]
            public ComponentDataArray<WorldSectorReady> readySectors;
        }

        [Inject]
        CreateFilter createFilter;
        [Inject]
        KillFilter killFilter;

        [Inject]
        WorldCamera camera;

        EntityArchetype archetype;

        protected override void OnCreateManager(int capacity)
        {
            archetype = EntityManager.CreateArchetype(typeof(Sector), typeof(Transform), typeof(WorldSector));
        }

        protected override void OnUpdate()
        {
            // Remove anything be
            camera.AddRemoveGrid(
                WorldChunkConstants.ObjectsUnloadDistance, 
                ref killFilter.sectors, 
                (int2 sector) => { }, 
                (int index, int2 sector) => 
                    {
                        PostUpdateCommands.DestroyEntity(createFilter.entities[index]);
                    }
            );

            camera.AddRemoveGrid(
                WorldChunkConstants.ObjectsVisibleDistance,
                ref createFilter.sectors,
                (int2 sector) =>
                    {
                        var entity = EntityManager.CreateEntity(archetype);
                        EntityManager.SetComponentData(entity, new Sector(sector));
                    },
                (int index, int2 sector) => { }
            );
        }
    }
}