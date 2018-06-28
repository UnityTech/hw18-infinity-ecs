using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public class CameraControlSystem : ComponentSystem
    {
        [Inject]
        CameraSystem camera;

        [Inject]
        TerrainChunkAssetDataSystem heightmapSystem;

        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            var cameraShift = EntityManager.GetComponentData<Shift>(camera.main);
            var cameraRotate = EntityManager.GetComponentData<Rotation>(camera.main);

            // Update rotation
            Quaternion quat = new Quaternion(cameraRotate.rotation.value.x, cameraRotate.rotation.value.y, cameraRotate.rotation.value.z, cameraRotate.rotation.value.w);
            Vector3 rotation = quat.eulerAngles;
            rotation.x -= Input.GetAxis("Mouse Y");
            rotation.y += Input.GetAxis("Mouse X");
            EntityManager.SetComponentData<Rotation>(camera.main, new Rotation(Quaternion.Euler(rotation)));

            // Update position
            float forward = 0, strafe = 0, speed = 5;
            if (Input.GetKey(KeyCode.W))
                forward += 2;
            if (Input.GetKey(KeyCode.S))
                forward += -2;
            if (Input.GetKey(KeyCode.A))
                strafe += -2;
            if (Input.GetKey(KeyCode.D))
                strafe += 2;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed = 50;

            // Make direction vectors horizontal
            var dirForward = math.forward(quat);
            dirForward.y = 0;
            dirForward = math.normalize(dirForward);
            var dirRight = math.cross(math.forward(quat), math.up(quat));
            dirRight.y = 0;
            dirRight = math.normalize(dirRight);

            var deltaPos = dirForward * forward * dt * speed;
            deltaPos -= dirRight * strafe * dt * speed;
            cameraShift.value += deltaPos;

            // Add elevation from heightmap
            int2 cameraShiftInt = new int2(
                math.clamp((int)cameraShift.value.x, 0, WorldChunkConstants.ChunkSize - 1),
                math.clamp((int)cameraShift.value.z, 0, WorldChunkConstants.ChunkSize - 1));

            var cameraSector = EntityManager.GetComponentData<Sector>(camera.main);

            NativeArray<float> heightmap;
            if (heightmapSystem.GetHeightmap(cameraSector, out heightmap))
                cameraShift.value.y = heightmap[cameraShiftInt.y * WorldChunkConstants.ChunkSize + cameraShiftInt.x] * WorldChunkConstants.TerrainHeightScale + 3.0f;

            EntityManager.SetComponentData<Shift>(camera.main, cameraShift);
        }
    }
}
